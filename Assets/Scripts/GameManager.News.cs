using UnityEngine;

// GameManager의 뉴스 시스템 담당 partial — 소형/대형 뉴스 발생·예약·효과 적용을 다룬다
public partial class GameManager
{
    // 활성 뉴스의 남은 일수·경과 일수를 하루씩 진행
    void AdvanceActiveNews()
    {
        foreach (ActiveCase active in activeNewsList)
        {
            active.Rday--;
            active.currentDay++;
        }
    }

    // 불확정 뉴스 대기 처리 — 지정된 턴이 지나면 실제 활성 뉴스로 전환해 가격에 영향을 준다
    void AdvancePendingNews()
    {
        for (int i = pendingNewsList.Count - 1; i >= 0; i--)
        {
            pendingNewsList[i].daysLeft--;
            if (pendingNewsList[i].daysLeft > 0) continue;

            // 소문이 사실로 확정 — AddActiveNews가 "[속보]" 접두어와 공지를 함께 처리한다
            AddActiveNews(pendingNewsList[i].caseData, null, true);
            pendingNewsList.RemoveAt(i);
        }
    }

    // 소형 뉴스 생성 — 3~5턴 주기로 템플릿에서 뉴스를 하나 만들어 낸다
    void TryGenerateSmallNews()
    {
        smallNewsTimer++;
        if (newsTemplates.Length == 0) return;
        if (smallNewsTimer < Random.Range(GameBalance.SMALL_NEWS_MIN_INTERVAL, GameBalance.SMALL_NEWS_MAX_INTERVAL)) return;

        smallNewsTimer = 0;
        NewsTemplate template = newsTemplates[Random.Range(0, newsTemplates.Length)];
        TemplateNewsGenerator.GeneratedNews? result = TemplateNewsGenerator.Generate(template);
        if (!result.HasValue) return;

        if (result.Value.isUncertain)
        {
            // 불확정 뉴스 — 지금 당장 가격에 영향 주지 않고 대기열에 등록
            pendingNewsList.Add(new PendingNews
            {
                caseData = result.Value.caseData,
                daysLeft = GameBalance.UNCERTAIN_NEWS_DELAY,
            });
            AddNotice($"[소문] {result.Value.caseData.CaseDescription}");
        }
        else
        {
            AddActiveNews(result.Value.caseData); // 확정 뉴스 — 바로 활성화
        }
    }

    // 예약된 전조 뉴스와 대형 뉴스를 날짜에 맞춰 발생시킨다
    void TriggerScheduledNews()
    {
        bool hasScheduledCase = scheduledBigNewsCaseIndex >= 0
            && scheduledBigNewsCaseIndex < BigTriggerCases.Count;

        // 전조 뉴스 — 대형 뉴스 발생 2~3턴 전
        if (currentDay == scheduledBigNewsVibeDay && hasScheduledCase)
        {
            Case[] foreshadows = BigTriggerCases[scheduledBigNewsCaseIndex].foreshadowCases;
            if (foreshadows != null && foreshadows.Length > 0
                && scheduledForeshadowIndex >= 0 && scheduledForeshadowIndex < foreshadows.Length)
            {
                Case foreshadow = foreshadows[scheduledForeshadowIndex];
                if (foreshadow != null && !activeNewsList.Exists(a => a.caseData == foreshadow))
                    AddActiveNews(foreshadow, scheduledNationPair);
            }
        }

        // 예약된 대형 뉴스 발생
        if (scheduledBigNewsDay <= 0 || currentDay < scheduledBigNewsDay) return;

        if (hasScheduledCase)
        {
            Case picked = BigTriggerCases[scheduledBigNewsCaseIndex];
            if (!activeNewsList.Exists(active => active.caseData == picked))
            {
                AddActiveNews(picked, scheduledNationPair);
                lastBigNewsDay = currentDay;
            }
        }
        ScheduleNextBigNews();
    }

    // 활성 뉴스의 가격 효과를 모두 적용하고, 오늘 새로 발생한 뉴스는 공지에 올린다
    void ApplyActiveNewsEffects()
    {
        StockControl[] allStockControls = StockControl.FindAll();

        foreach (ActiveCase active in activeNewsList)
        {
            // 경과 일수에 따른 감쇠율 — 배열 범위를 넘어가면 효과 없음
            float rate = active.currentDay < active.caseData.lowPercent.Length
                ? active.caseData.lowPercent[active.currentDay]
                : 0f;

            bool isBigNews = active.caseData.newsType == Case.NewsType.BigTriggerNews
                && active.caseData.industryRates != null
                && active.caseData.industryRates.Length > 0;

            if (isBigNews)
                ApplyBigNewsEffect(active, rate);
            else
                ApplySmallNewsEffect(active, rate, allStockControls);

            if (active.caseData.newsType == Case.NewsType.BigTriggerNews)
            {
                foreach (StockControl sc in allStockControls)
                    sc.UpdatePriceText();
            }

            if (active.currentDay == 0)
                AddNotice(active.displayDescription);
        }
    }

    // 대형 뉴스 효과 — 산업군별로 당사국/비당사국 변동률을 나눠 적용
    void ApplyBigNewsEffect(ActiveCase active, float rate)
    {
        foreach (Stock stock in LiveStocks)
        {
            // 이 주식의 산업군에 맞는 변동률 찾기
            Case.IndustryRate matchedRate = default;
            bool found = false;
            foreach (Case.IndustryRate ir in active.caseData.industryRates)
            {
                if (ir.industryTag != stock.IndustryTag) continue;
                matchedRate = ir;
                found = true;
                break;
            }
            if (!found) continue;

            bool isInvolved = active.involvedNations.Contains(stock.nation); // 당사국 여부
            int minRate = isInvolved ? matchedRate.changePriceMin : matchedRate.changePriceMinNon;
            int maxRate = isInvolved ? matchedRate.changePriceMax : matchedRate.changePriceMaxNon;

            stock.StockCurrentPrice = Mathf.RoundToInt(
                stock.StockCurrentPrice * (1 + Random.Range(minRate, maxRate + 1) / 100f * rate));
            ClampPrice(stock);

            Debug.Log($"[{active.caseData.CaseName}] {stock.StockName} | " +
                $"산업군: {stock.IndustryTag} | 당사국: {isInvolved} | " +
                $"변동 전: {stock.StockPreviousPrice} | 변동 후: {stock.StockCurrentPrice}");
        }
    }

    // 소형 뉴스 효과 — 지정된 주식에 주 효과를, 2일차에 연쇄 효과를 적용
    void ApplySmallNewsEffect(ActiveCase active, float rate, StockControl[] allStockControls)
    {
        Case data = active.caseData;

        if (data.affectedCase != null)
        {
            for (int i = 0; i < data.affectedCase.Length; i++)
            {
                Stock affected = data.affectedCase[i];
                if (affected == null)
                {
                    Debug.LogError($"[{data.CaseName}]의 affectedCase[{i}]가 null입니다!");
                    continue;
                }
                if (affected.isDelisted) continue; // 상폐된 회사는 뉴스 효과를 받지 않는다

                affected.StockCurrentPrice = Mathf.RoundToInt(
                    affected.StockCurrentPrice *
                    (1 + Random.Range(data.ChangePriceMin[i], data.ChangePriceMax[i] + 1) / 100f * rate));
                ClampPrice(affected);

                Debug.Log($"[{data.CaseName}] {affected.StockName} | " +
                    $"변동 전: {affected.StockPreviousPrice} | 감쇠율: {rate} | 변동 후: {affected.StockCurrentPrice}");

                RefreshPriceTexts(allStockControls, affected);
            }
        }

        // 연쇄 효과 — 뉴스 2일차에만 실행
        if (active.currentDay != 1 || data.chainEffects == null) return;

        foreach (Case.ChainEffect effect in data.chainEffects)
        {
            if (effect.stock == null || effect.stock.isDelisted) continue;

            effect.stock.StockCurrentPrice = Mathf.RoundToInt(
                effect.stock.StockCurrentPrice *
                (1 + Random.Range((float)effect.priceMin, effect.priceMax) / 100f * rate));
            ClampPrice(effect.stock);

            RefreshPriceTexts(allStockControls, effect.stock);
        }
    }

    // 해당 주식을 표시 중인 모든 StockControl의 가격 텍스트 갱신
    static void RefreshPriceTexts(StockControl[] allStockControls, Stock stock)
    {
        foreach (StockControl sc in allStockControls)
        {
            if (sc.stockinfo == stock)
                sc.UpdatePriceText();
        }
    }

    // ── 뉴스 스케줄 / 등록 ────────────────────────────

    // 다음 대형 뉴스 발생 날짜·케이스·국가 쌍·전조 뉴스를 무작위로 예약
    void ScheduleNextBigNews()
    {
        if (BigTriggerCases.Count == 0) return;

        scheduledBigNewsCaseIndex = Random.Range(0, BigTriggerCases.Count);
        scheduledBigNewsDay = currentDay + Random.Range(GameBalance.BIG_NEWS_MIN_INTERVAL, GameBalance.BIG_NEWS_MAX_INTERVAL);
        Case bigCase = BigTriggerCases[scheduledBigNewsCaseIndex];

        // 대형 뉴스의 국가 쌍을 미리 결정 — 전조 뉴스와 공유
        scheduledNationPair = (bigCase.nationPairs != null && bigCase.nationPairs.Length > 0)
            ? bigCase.nationPairs[Random.Range(0, bigCase.nationPairs.Length)]
            : "";

        Case[] foreshadows = bigCase.foreshadowCases;
        Case foreshadow = null;
        if (foreshadows != null && foreshadows.Length > 0)
        {
            scheduledForeshadowIndex = Random.Range(0, foreshadows.Length);
            foreshadow = foreshadows[scheduledForeshadowIndex];
        }
        else
        {
            scheduledForeshadowIndex = -1;
        }

        // 전조 뉴스가 대형 뉴스 직전에 끝나도록 지속일만큼 앞당겨 배치
        int lead = (foreshadow != null && foreshadow.lastingDay > 0)
            ? foreshadow.lastingDay
            : GameBalance.FORESHADOW_DEFAULT_LEAD;
        scheduledBigNewsVibeDay = scheduledBigNewsDay - (lead + 1);
    }

    // 케이스를 활성 뉴스로 등록 — 국가 쌍을 플레이스홀더에 치환하고 관련 국가 목록을 초기화
    public void AddActiveNews(Case casedata, string forcedNationPair = null, bool isRumorConfirmed = false)
    {
        Debug.Log($"뉴스 추가: {casedata.CaseName}");

        ActiveCase newActive = new()
        {
            caseData = casedata,
            Rday = casedata.lastingDay,
            currentDay = 0,
            displayDescription = casedata.CaseDescription,
        };

        if (casedata.nationPairs != null && casedata.nationPairs.Length > 0)
        {
            string picked = !string.IsNullOrEmpty(forcedNationPair)
                ? forcedNationPair
                : casedata.nationPairs[Random.Range(0, casedata.nationPairs.Length)];

            if (picked.Contains(",")) // 접경국 쌍 ("마노사,알브레이")
            {
                string[] nations = picked.Split(',');
                newActive.displayDescription = casedata.CaseDescription
                    .Replace("[A국]", nations[0])
                    .Replace("[B국]", nations[1]);
                newActive.involvedNations.Add(nations[0]);
                newActive.involvedNations.Add(nations[1]);
            }
            else // 단일 국가 ("마노사")
            {
                newActive.displayDescription = casedata.CaseDescription.Replace("[국가]", picked);
                newActive.involvedNations.Add(picked);
            }
        }

        // 소문이 사실로 확정됐음을 표시 — 매일 효과 루프에서 이 문구가 그대로 한 번만 뜸
        if (isRumorConfirmed)
            newActive.displayDescription = "[속보] " + newActive.displayDescription;

        activeNewsList.Add(newActive);
    }

    // 이 주식이 현재 활성 뉴스의 영향을 받고 있는지 판정 (직접 영향 / 연쇄 효과 / 대형 뉴스 산업군)
    bool IsAffectedByNews(Stock stock)
    {
        return activeNewsList.Exists(active =>
        {
            Case data = active.caseData;

            // ① 소형 뉴스 직접 영향 (지속 기간 포함)
            if (data.affectedCase != null && System.Array.Exists(data.affectedCase, s => s == stock))
                return true;

            // ② 연쇄 효과
            if (data.chainEffects != null && System.Array.Exists(data.chainEffects, e => e.stock == stock))
                return true;

            // ③ 대형 뉴스 산업군 영향
            if (data.newsType == Case.NewsType.BigTriggerNews
                && data.industryRates != null
                && System.Array.Exists(data.industryRates, ir => ir.industryTag == stock.IndustryTag))
                return true;

            return false;
        });
    }
}
