using System.Collections.Generic;
using UnityEngine;

// GameManager의 재무 담당 partial — 배당, 금고 적립, 부도/상장폐지, 실적 기반 주가·공시를 다룬다
public partial class GameManager
{
    // 상장 중인 회사만 순회한다 — 상폐 종목을 빠뜨리고 건드리는 실수를 막기 위한 단일 통로
    // tabmanager.allstock 배열에서 실제로 제거하지는 않는다 — 인스펙터에 등록된 원본 배열이라 되돌리기가 까다롭다
    IEnumerable<Stock> LiveStocks
    {
        get
        {
            foreach (Stock stock in tabmanager.allstock)
                if (!stock.isDelisted) yield return stock;
        }
    }

    // 배당 지급 — 배당 성애자(Personality.DividendLover)만 정기 배당을 준다. 영업이익이 0 이하면 삭감
    void PayDividends()
    {
        foreach (Stock stock in LiveStocks)
        {
            if (stock.personality != Stock.Personality.DividendLover) continue;

            // 실적 악화 시 배당 삭감
            if (stock.income <= 0)
            {
                AddNotice($"[배당 삭감] {stock.StockName} 실적 악화로 이번 분기 배당이 없습니다.");
                continue;
            }

            // 뜬금 이벤트 — 특별 배당 폭탄. 평소보다 몇 배 몰아주고 주가도 그 자리에서 들썩인다
            // 성격상 배당 성애자는 사업 슬롯을 거의 안 쓰므로, 사업 쪽 뜬금 이벤트와 겹칠 걱정 없이 여기서 독립적으로 판정한다
            bool isBomb = stock.runtimeAsset > 0 && Random.value < GameBalance.SURPRISE_EVENT_CHANCE;

            // 배당 재원은 회사 금고다. 이번 분기 수입 중 배당성향만큼을 주주 전체에게 나눠준다
            // dividendRate를 안 채운 종목은 기본값을 쓴다 — 종목 40개를 일일이 채우지 않아도 배당이 살아있게 하기 위함
            float rate = stock.dividendRate > 0f ? stock.dividendRate : GameBalance.DIVIDEND_RATE_DEFAULT;
            if (isBomb) rate *= GameBalance.DIVIDEND_BOMB_MULTIPLIER;

            int totalDividend = MoneyMath.Int(stock.income * rate);
            if (totalDividend <= 0) continue;

            // 금고가 모자라면 있는 만큼만 준다 — 금고가 마른 회사는 배당을 못 주게 되는 것이 기획 의도
            if (totalDividend > stock.runtimeAsset) totalDividend = stock.runtimeAsset;
            if (totalDividend <= 0)
            {
                AddNotice($"[배당 중단] {stock.StockName} 금고가 바닥나 배당을 지급하지 못했습니다.");
                continue;
            }

            stock.runtimeAsset -= totalDividend;

            // 주당 배당 = 배당총액 ÷ 발행 주식 수 (주가와 무관하게 회사가 번 돈에서 나온다)
            float perShare = stock.TotalShare > 0 ? (float)totalDividend / stock.TotalShare : 0f;

            if (isBomb)
            {
                // 실적 발표 쇼크(ApplyFinancialGravity)와 별개로, 배당 폭탄 자체가 즉시 호재로 작동한다
                int bump = Mathf.Max(1, MoneyMath.Int(stock.StockCurrentPrice * GameBalance.DIVIDEND_BOMB_PRICE_BUMP));
                stock.StockCurrentPrice = MoneyMath.Int((long)stock.StockCurrentPrice + bump);
                ClampPrice(stock);
                AddNotice($"[뜬금 이벤트 · 특별 배당] {stock.StockName}이(가) 평소의 {GameBalance.DIVIDEND_BOMB_MULTIPLIER:0}배에 달하는 배당을 터뜨렸습니다! " +
                          $"(주당 {perShare:F1} G · 주가 +{GameBalance.DIVIDEND_BOMB_PRICE_BUMP * 100f:F0}%)");
            }

            int owned = Portfolio.GetAmount(stock);
            if (owned <= 0) continue;

            int received = MoneyMath.Int(perShare * owned);
            if (received <= 0) continue;

            UpdateGold(received);
            AddNotice($"[배당] {stock.StockName} {owned}주 → +{received} G (주당 {perShare:F1} G)");
        }
    }

    // ── 금고 · 부도 · 상장폐지 ─────────────────────────

    // 금고 상태를 보고 경고 → 부도 위기 → 상장폐지를 판정한다
    // 배당까지 다 빠져나간 최종 금고를 봐야 "금고 바닥 → 위험 시작"이 제대로 작동한다
    void CheckSolvency()
    {
        foreach (Stock stock in LiveStocks)
        {
            if (stock.runtimeAsset >= 0)
            {
                stock.negativeQuarters = 0; // 플러스로 돌아오면 초기화 — "위기에서 극적으로 살아나는" 이야기가 나온다
                continue;
            }

            stock.negativeQuarters++;

            if (stock.negativeQuarters >= GameBalance.SOLVENCY_DELIST_QUARTERS)
            {
                DelistStock(stock);
            }
            else if (stock.negativeQuarters >= GameBalance.SOLVENCY_CRISIS_QUARTERS)
            {
                AddNotice($"[부도 위기] {stock.StockName} 금고가 {stock.negativeQuarters}분기 연속 마이너스입니다.");
            }
            else
            {
                AddNotice($"[경고] {stock.StockName} 금고가 마이너스입니다. ({stock.negativeQuarters}분기째)");
            }
        }
    }

    // 상장폐지 처리 — 순서가 중요하다: 보유 주식을 먼저 비운 뒤, 진행 중이던 사업·뉴스를 걷어내고,
    // 화면에 남은 흔적(상세 패널·주식 목록 슬롯)을 지운다. 하나라도 빠뜨리면 없는 회사에
    // 뉴스가 붙거나 배당이 나가는 유령 상태가 된다
    void DelistStock(Stock stock)
    {
        stock.isDelisted = true;
        foreach (var npc in FindObjectsByType<NPCInvestor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            npc.OnStockDelisted(stock);

        Portfolio.SetHolding(stock, 0, 0); // 보유 주식 전액 손실 — 골드 지급 없음 (기획대로)

        activeBusinesses.RemoveAll(b => b.stockName == stock.StockName);

        // 이 회사 하나만을 대상으로 하는 뉴스만 걷어낸다. 여러 회사가 걸린 뉴스(대형 뉴스 등)는
        // 다른 회사를 위해 남겨둔다
        activeNewsList.RemoveAll(active => IsCompanyOnlyNewsFor(active.caseData, stock));
        pendingNewsList.RemoveAll(pending => IsCompanyOnlyNewsFor(pending.caseData, stock));

        stock.isFavorite = false;
        stock.runtimeIssues.Clear();

        if (UIManager.instance.detailpanel != null)
            UIManager.instance.detailpanel.CloseIfShowing(stock);

        // 이미 생성된 주식 목록 슬롯이 있으면 숨긴다 (탭을 아직 안 열었다면 TabManager.CreateStock이
        // 애초에 생성하지 않는다)
        foreach (StockControl sc in StockControl.FindAll())
            if (sc.stockinfo == stock)
                sc.gameObject.SetActive(false);

        UIManager.instance.favoritePanel.RefreshHome();

        AddNotice($"[상장폐지] {stock.StockName}이(가) 상장폐지되었습니다.");
    }

    // 이 뉴스가 오직 이 회사 하나만을 대상으로 하는지 판정
    static bool IsCompanyOnlyNewsFor(Case data, Stock stock)
    {
        return data != null
            && data.affectedCase != null
            && data.affectedCase.Length > 0
            && System.Array.TrueForAll(data.affectedCase, s => s == null || s == stock);
    }

    // ── 회사 자산 / 재무 기반 주가 ────────────────────

    // 매턴 영업이익의 일부를 회사 자산으로 적립한다
    // 돈을 잘 버는 회사일수록 재산이 빨리 쌓이고, 적자면 반대로 자산이 줄어든다
    // (영업이익이 음수면 곱셈 결과도 음수라 따로 분기할 필요가 없다)
    void AccumulateAssets()
    {
        foreach (Stock stock in LiveStocks)
        {
            stock.runtimeAsset = MoneyMath.Int((long)stock.runtimeAsset + stock.income); // 그 분기에 번 돈이 통째로 금고에 들어간다
        }
    }

    // 실적 발표에 맞춰 주가를 목표가 쪽으로 끌어당긴다 — 분기 마감일에만 호출된다
    // 평소에는 뉴스·시대·매매만 주가를 움직이고, 실적은 발표일에 한 번에 반영되어 이 날이 이벤트가 된다
    void ApplyFinancialGravity()
    {
        foreach (Stock stock in LiveStocks)
        {
            int gap = GetFairPrice(stock) - stock.StockCurrentPrice;
            if (gap == 0) continue;

            // 주가가 정수라, 차이가 작으면 반올림 결과가 0이 되어 영영 멈춘다
            // 특히 저가 종목은 첫 판정부터 0이라 실적이 주가에 전혀 반영되지 않는다
            // 차이가 남아 있으면 최소 1G는 움직이게 해서 목표까지 반드시 도달시킨다
            int step = MoneyMath.Int(gap * GameBalance.QUARTER_SHOCK);
            if (step == 0) step = gap > 0 ? 1 : -1;

            stock.StockCurrentPrice += step;
            ClampPrice(stock);
        }
    }

    // 실적 공시 — 지난 분기 대비 수입이 얼마나 변했는지 알린다
    // 사업 성공·실패 뉴스와 별개로, 시대 보정까지 반영된 최종 성적표를 보여주는 역할
    void AnnounceQuarterResults()
    {
        foreach (Stock stock in LiveStocks)
        {
            int previous = stock.lastQuarterIncome;
            stock.lastQuarterIncome = stock.income;

            // 이유 판정에 쓸 "지난 분기 배율 → 이번 분기 배율"을 확정한다
            // continue로 빠지는 회사도 배율은 갱신해둬야, 다음 분기 비교가 어긋나지 않는다
            float previousModifier = lastQuarterEraModifier.TryGetValue(stock.StockName, out float saved) ? saved : 1.0f;
            float currentModifier = GetEraRevenueModifier(stock);
            lastQuarterEraModifier[stock.StockName] = currentModifier;

            // 매출도 같은 규칙으로 기억해둔다 — 기록이 없으면 이번 매출을 그대로 써서 "변화 없음"으로 본다
            int previousRevenue = lastQuarterRevenue.TryGetValue(stock.StockName, out int savedRevenue)
                ? savedRevenue
                : stock.runtimeRevenue;
            lastQuarterRevenue[stock.StockName] = stock.runtimeRevenue;

            if (previous <= 0) continue; // 첫 분기는 비교 대상이 없다

            float changePercent = ((float)stock.income - previous) * 100f / previous;
            if (Mathf.Abs(changePercent) < GameBalance.QUARTER_REPORT_THRESHOLD) continue; // 변화가 미미하면 공시하지 않는다

            string arrow = changePercent > 0 ? "▲" : "▼";
            AddNotice($"[실적] {stock.StockName} 수입 {arrow}{Mathf.Abs(changePercent):F0}% ({UIFormat.Gold(stock.income)}) — {GetIncomeChangeReason(stock, previousRevenue, previousModifier, currentModifier)}");
        }
    }

    // 이번 분기 수입이 왜 변했는지 한 줄로 설명한다
    // 1순위는 사업 성패(평소 매출 자체가 영구히 바뀐 것),
    // 2순위는 시대 배율의 변화(매출은 그대로인데 배율만 1.2 ↔ 1.0 ↔ 0.8로 옮겨간 것)
    string GetIncomeChangeReason(Stock stock, int previousRevenue, float previousModifier, float currentModifier)
    {
        // 수입 = round(매출 × 시대배율) − 운영비 다. 지난 분기와 빼면 운영비는 상쇄되고 "매출 × 배율"만 남는다.
        // 덕분에 변화를 두 몫으로 정확히 쪼갤 수 있다 (둘을 더하면 실제 변화와 일치한다)
        //   시대 몫 = 지난 매출 × (이번 배율 − 지난 배율)   ← 매출은 지난 분기 그대로 두고 배율만 바꿔봤을 때
        //   사업 몫 = (이번 매출 − 지난 매출) × 이번 배율   ← 배율은 이번 분기로 두고 매출만 바꿔봤을 때
        float eraDelta = previousRevenue * (currentModifier - previousModifier);
        float businessDelta = ((float)stock.runtimeRevenue - previousRevenue) * currentModifier;

        // 사업 쪽 문구는 ResolveBusinesses가 남겨둔 "{사업명} 성공/실패"를 그대로 쓴다
        bool hasBusiness = quarterChangeReasons.TryGetValue(stock.StockName, out string businessReason)
                           && Mathf.Abs(businessDelta) >= 1f;
        bool hasEra = Mathf.Abs(eraDelta) >= 1f;

        if (!hasEra && !hasBusiness) return DescribeSteadyEra(currentModifier);
        if (!hasEra) return businessReason;
        if (!hasBusiness) return DescribeEraShift(previousModifier, currentModifier);

        // 둘 다 움직인 분기 — 기여분이 큰 쪽을 주 원인으로 앞에 세운다.
        // 예전에는 사업 결과가 있으면 무조건 사업을 이유로 적어서, 시대 수혜로 수입이 3배가 된 분기에도
        // "사업 실패"만 떠서 화면 숫자와 정반대로 읽혔다
        bool eraIsBigger = Mathf.Abs(eraDelta) >= Mathf.Abs(businessDelta);
        string main = eraIsBigger ? ShortEraPhrase(previousModifier, currentModifier) : businessReason;
        string sub = eraIsBigger ? ShortBusinessPhrase(businessReason) : ShortEraPhrase(previousModifier, currentModifier);

        // 방향이 같으면 서로 밀어준 것이고, 다르면 한쪽이 다른 쪽을 깎아먹은 것이다
        bool sameDirection = (eraDelta > 0f) == (businessDelta > 0f);
        return sameDirection ? $"{main} + {sub}" : $"{main} ({WithRoParticle(sub)} 일부 상쇄)";
    }

    // 시대 배율이 움직인 방향을 문장으로 — 시대가 유일한 원인일 때 쓴다
    string DescribeEraShift(float previousModifier, float currentModifier)
    {
        string eraName = currentEra != null ? currentEra.eraName : "시대";

        if (currentModifier > previousModifier)
            return currentModifier > 1.0f ? $"{eraName} 수혜를 받기 시작" : "시대의 타격에서 벗어남";

        return currentModifier < 1.0f ? $"{eraName} 타격을 받기 시작" : "시대의 수혜가 끝남";
    }

    // 사업과 나란히 놓을 때 쓰는 짧은 형태 — 한 줄에 둘이 들어가므로 문장이 아니라 명사로 끊는다
    // 수혜·타격이 "끝나는" 쪽은 현재 시대가 원인이 아니므로(이미 다른 시대이거나 시대 없음) 이름을 붙이지 않는다
    string ShortEraPhrase(float previousModifier, float currentModifier)
    {
        string eraName = currentEra != null ? currentEra.eraName : "시대";

        if (currentModifier > previousModifier)
            return currentModifier > 1.0f ? $"{eraName} 수혜" : "시대 타격 해소";

        return currentModifier < 1.0f ? $"{eraName} 타격" : "시대 수혜 종료";
    }

    // 배율이 그대로인 분기 — 변화를 만든 것이 아니라 계속 걸려 있는 상태를 알린다
    string DescribeSteadyEra(float currentModifier)
    {
        string eraName = currentEra != null ? currentEra.eraName : "시대";

        if (currentModifier > 1.0f) return $"{eraName} 수혜가 이어지는 중";
        if (currentModifier < 1.0f) return $"{eraName} 타격이 이어지는 중";
        return "장사 규모 변화";
    }

    // "{사업명} 성공/실패" → "사업 성공 / 사업 실패"
    // 괄호 안 보조 자리에까지 사업명을 넣으면 줄이 너무 길어진다. 사업명은 같은 팝업의 [사업 실패] 줄에 이미 있다
    static string ShortBusinessPhrase(string businessReason)
    {
        return businessReason != null && businessReason.EndsWith("실패") ? "사업 실패" : "사업 성공";
    }

    // 한국어 조사 "로 / 으로"를 앞 글자의 받침에 맞춰 고른다 (실패 → 실패로, 성공 → 성공으로)
    // 받침이 없거나 받침이 ㄹ이면 "로", 그 밖에는 "으로"
    static string WithRoParticle(string word)
    {
        if (string.IsNullOrEmpty(word)) return word;

        char last = word[^1];
        if (last < '가' || last > '힣') return word + "로"; // 한글이 아니면 그냥 붙인다

        int finalConsonant = (last - '가') % 28; // 0이면 받침 없음, 8이면 받침 ㄹ
        return word + (finalConsonant == 0 || finalConsonant == 8 ? "로" : "으로");
    }

    // 목표 주가 — 회사 실적이 게임 시작 시점보다 얼마나 나아졌는지로 계산한다
    // 절대 공식(영업이익 × PER)을 쓰지 않는 이유: 종목별로 주가와 이익의 비율이 8~22배까지
    // 제각각이라, 절대 공식을 쓰면 게임을 시작하자마자 전 종목 주가가 뒤집힌다
    // 실적이 그대로면 목표가 = 기준가가 되어 기존 밸런스를 건드리지 않는다
    public int GetFairPrice(Stock stock)
    {
        // "시작보다 얼마나 늘었나"를 매출 규모로 나눈다 — 나누는 쪽이 항상 양수라 부호가 뒤집히지 않는다
        // 예전처럼 수입으로 나누면 평소 적자인 회사에서 두 가지가 깨진다
        //   ① 기준 수입이 0 이하면 계산 자체가 불가능해 사업 성공이 주가에 전혀 반영되지 않는다
        //   ② 음수끼리 나누면 부호가 뒤집혀, 적자가 두 배로 커질 때 목표 주가가 두 배로 오른다
        // 뺄셈으로 바꾸면 적자가 줄어드는 것도 "개선"으로 잡혀서, 부도 위기에서 살아나는 회복이 주가에 나타난다
        float improvement = ((float)stock.income - stock.BaselineIncome)
                            / Mathf.Max(stock.BaselineRevenue, 1);

        float multiplier = 1f + improvement * GameBalance.PROFIT_SENSITIVITY;

        // 폭락·폭등이 끝없이 이어지지 않도록 기준가의 30%~300% 사이로 제한
        // (제한은 "실적 대비 평가"에만 걸린다. 아래 시장 성장분은 이 제한 밖에서 따로 곱해진다)
        multiplier = Mathf.Clamp(multiplier, GameBalance.FAIR_PRICE_MIN_RATIO, GameBalance.FAIR_PRICE_MAX_RATIO);

        // 시장 전체 성장 — 주가가 매일 DAILY_BIAS만큼 오르는 만큼 목표 주가도 같이 올려준다
        //
        // 이게 없으면 주가만 매일 오르고 목표가는 게임 시작 시점에 멈춰 있어서, 분기가 지날수록
        // 괴리가 쌓이고 분기말마다 주가가 끌어내려지는 톱니 패턴이 생긴다 (실측: 분기말의 72%가 하락,
        // 주가가 목표가보다 평균 7.6% 위에서 평형). 그러면 "분기말 직전에 팔고 지나서 사기"가
        // 항상 이기는 요령이 되어버린다
        //
        // 현실에서도 금리가 내려가면 개별 회사 실적과 무관하게 모든 자산의 적정가치가 같이 오른다.
        // 개별 종목의 주가가 아니라 시장 전체에 걸리는 공통 계수라서 순환 참조가 아니다
        float marketGrowth = Mathf.Pow(1f + GameBalance.DAILY_BIAS / 100f, currentDay);

        return MoneyMath.Int(stock.StockBasePrice * multiplier * marketGrowth);
    }
}
