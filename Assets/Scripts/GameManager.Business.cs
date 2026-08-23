using System.Collections.Generic;
using UnityEngine;

// GameManager의 사업 시스템 담당 partial — 매출 재계산, 사업 착수/진행/성패 판정, 뜬금 이벤트를 다룬다
public partial class GameManager
{
    // ── 실적 / 사업 ───────────────────────────────────

    // 매턴 모든 회사의 매출·영업이익을 재계산
    void RecalculateFinancials()
    {
        foreach (Stock stock in LiveStocks)
        {
            // 시대는 매출만 밀거나 누른다. 운영비는 시대와 무관하게 그대로 나간다
            // 그래서 운영비가 무거운 회사일수록 같은 시대 배율에도 수입이 크게 흔들린다
            int revenue = Mathf.RoundToInt(stock.runtimeRevenue * GetEraRevenueModifier(stock));
            stock.income = revenue - stock.operatingCost;
        }
    }

    // 진행 중이던 사업의 성공·실패만 판정한다 — 분기 마감일에만 호출된다
    // 사업 결과가 분기 실적 발표와 같은 날 나와야 "이번 분기 잘 나올까?" 하는 긴장감이 생긴다
    // 새 사업 착수(StartNewBusinesses)는 여기서 하지 않는다 — 배당·부도 판정이 끝난 뒤 최종 금고를 보고 정한다
    void ResolveBusinesses()
    {
        for (int i = activeBusinesses.Count - 1; i >= 0; i--)
        {
            ActiveBusiness biz = activeBusinesses[i];
            biz.remainingQuarters--;

            Stock stock = System.Array.Find(tabmanager.allstock, s => s.StockName == biz.stockName);

            // 투자비는 착수할 때 금고에서 이미 빠져나갔으므로 진행 중에는 아무것도 하지 않는다
            if (biz.remainingQuarters > 0) continue;

            // 회사를 못 찾으면(이름이 바뀌었거나 목록에서 빠졌거나) 판정 자체를 건너뛴다
            // 예전에는 이 경우가 실패 처리로 흘러들어가서, 있지도 않은 회사의 실패 뉴스가 떴다
            if (stock == null)
            {
                activeBusinesses.RemoveAt(i);
                continue;
            }

            int revenueBefore = stock.runtimeRevenue;

            // 사업 완료 판정 — 성공은 크게, 실패는 작게 (한 번 실패로 회사가 죽지 않도록)
            bool success = Random.Range(0, 100) < biz.successRate;
            if (success)
            {
                // 사업이 올리는 것은 순수입이 아니라 평소 매출이다 — 성공하면 장사 규모 자체가 커진다
                stock.runtimeRevenue += biz.revenueBoost;
                stock.runtimeAsset += biz.assetBoost;

                // 문구만 띄우면 "그래서 얼마가 달라졌는데?"를 알 수 없다 — 매출이 얼마에서 얼마로 갔는지 숫자로 보여준다
                AddNotice($"[사업 성공] {stock.StockName} · {biz.projectName}\n" +
                          $"    {biz.successNews}\n" +
                          $"    매출 {UIFormat.Gold(revenueBefore)} → {UIFormat.Gold(stock.runtimeRevenue)} (+{UIFormat.Gold(biz.revenueBoost)})" +
                          (biz.assetBoost > 0 ? $" · 금고 +{UIFormat.Gold(biz.assetBoost)}" : ""));
                quarterChangeReasons[stock.StockName] = $"{biz.projectName} 성공";
            }
            else
            {
                // 실패: 투자금은 이미 날아갔고, 평소 매출도 소폭 내려간다
                stock.runtimeRevenue -= biz.revenueDrop;

                AddNotice($"[사업 실패] {stock.StockName} · {biz.projectName}\n" +
                          $"    {biz.failNews}\n" +
                          $"    매출 {UIFormat.Gold(revenueBefore)} → {UIFormat.Gold(stock.runtimeRevenue)} (-{UIFormat.Gold(biz.revenueDrop)})" +
                          $" · 투자금 {UIFormat.Gold(biz.investmentCost)} 회수 실패");
                quarterChangeReasons[stock.StockName] = $"{biz.projectName} 실패";
            }

            activeBusinesses.RemoveAt(i);
        }
    }

    // 새 사업 착수 — 분기 마감일에, 배당·부도 판정이 끝난 뒤 호출된다
    // 회사마다 독립적으로 딱 한 번만 판정한다. 사업 목록을 먼저 돌며 하나씩 판정하면
    // 사업을 많이 가진 회사만 착수 확률이 배로 뛰어서, 사업 종류를 늘릴수록 불공평해진다
    void StartNewBusinesses()
    {
        foreach (Stock stock in LiveStocks)
        {
            // 뜬금 이벤트(초대형 사업·깜짝 대형 투자)가 터지면 이번 분기 사업 슬롯은 그걸로 끝 —
            // 평소 사업 착수 판정과 같은 슬롯(activeBusinesses에 회사당 하나)을 두고 경쟁하기 때문이다
            if (TryTriggerSurpriseEvent(stock)) continue;
            TryStartBusiness(stock);
        }
    }

    // 뜬금 이벤트 — 성격별로 정해진 뻔한 패턴 위에 가끔 얹히는 예상 밖의 한 방
    // 배당 성애자의 특별 배당 폭탄은 사업 슬롯을 안 쓰므로 PayDividends에서 따로 판정한다
    bool TryTriggerSurpriseEvent(Stock stock)
    {
        if (activeBusinesses.Exists(b => b.stockName == stock.StockName)) return false; // 이미 사업 진행 중이면 자리가 없다
        if (stock.runtimeAsset < 0) return false; // 빚진 회사는 뜬금 이벤트도 못 벌인다
        if (Random.value >= GameBalance.SURPRISE_EVENT_CHANCE) return false;

        if (stock.personality == Stock.Personality.BusinessManiac)
            return TryStartMegaBusiness(stock, GameBalance.MEGA_BUSINESS_SCALE, "초대형 사업");

        if (stock.personality == Stock.Personality.VaultKeeper)
            return TryStartMegaBusiness(stock, GameBalance.VAULT_SURPRISE_SCALE, "깜짝 대형 투자");

        return false; // 배당 성애자는 여기서 처리하지 않는다
    }

    // 사업 목록에서 하나를 골라 규모를 키운 채로 착수시킨다 — 초대형 사업과 깜짝 대형 투자가 공유하는 로직
    // 성패 판정·결과 발표는 평소 사업과 똑같은 ActiveBusiness 파이프라인을 그대로 탄다
    bool TryStartMegaBusiness(Stock stock, float scale, string eventLabel)
    {
        if (stock.businesses == null || stock.businesses.Length == 0) return false;

        // 뜬금 이벤트는 안전 마진(GameBalance.BUSINESS_SAFETY_MARGIN_QUARTERS)을 따지지 않는다 — 평소와 다른 파격적인 한 방이기 때문
        List<BusinessProject> candidates = new();
        foreach (BusinessProject project in stock.businesses)
        {
            if (project == null) continue;
            int scaledCost = Mathf.RoundToInt(project.investmentCost * scale);
            if (stock.runtimeAsset < scaledCost) continue;
            candidates.Add(project);
        }
        if (candidates.Count == 0) return false;

        StartBusiness(stock, candidates[Random.Range(0, candidates.Count)], scale, eventLabel);
        return true;
    }

    // 회사 하나가 새 사업을 시작할지 판정한다
    void TryStartBusiness(Stock stock)
    {
        if (stock.businesses == null || stock.businesses.Length == 0) return;

        // 이미 사업 진행 중인 회사는 제외
        if (activeBusinesses.Exists(b => b.stockName == stock.StockName)) return;

        // 금고가 마이너스면 새 도박을 벌이지 않는다 — 빚진 회사가 사업을 벌이면 부도 카운트가 무의미해진다
        if (stock.runtimeAsset < 0) return;

        // 성격별로 착수 확률이 다르다 — 사업 미치광이는 자주, 배당 성애자는 신중하게, 금고 지기는 제일 소극적으로
        float chance = stock.personality switch
        {
            Stock.Personality.BusinessManiac => GameBalance.BUSINESS_START_CHANCE_MANIAC,
            Stock.Personality.VaultKeeper => GameBalance.BUSINESS_START_CHANCE_VAULT,
            _ => GameBalance.BUSINESS_START_CHANCE_DIVIDEND,
        };
        if (Random.value >= chance) return;

        // 착수 후에도 운영비 몇 분기치는 금고에 남는 사업만 후보로 삼는다 — 안전 마진 없이 다 지르면 전부 부도난다
        int safetyMargin = stock.operatingCost * GameBalance.BUSINESS_SAFETY_MARGIN_QUARTERS;
        List<BusinessProject> affordable = new();
        foreach (BusinessProject project in stock.businesses)
        {
            if (project == null) continue;
            if (stock.runtimeAsset - project.investmentCost < safetyMargin) continue;
            affordable.Add(project);
        }
        if (affordable.Count == 0)
        {
            Debug.Log($"[사업 보류] {stock.StockName} 안전 마진을 남기고 감당할 사업이 없음 (금고 {stock.runtimeAsset})");
            return;
        }

        StartBusiness(stock, affordable[Random.Range(0, affordable.Count)]);
    }

    // 사업 하나를 실제로 착수시킨다 — 금고에서 투자비를 빼고 진행 목록에 등록
    // scale·eventLabel은 뜬금 이벤트(초대형 사업·깜짝 대형 투자) 전용 — 평소 사업은 기본값(1배, 라벨 없음)으로 호출된다
    void StartBusiness(Stock stock, BusinessProject picked, float scale = 1f, string eventLabel = null)
    {
        int investmentCost = Mathf.RoundToInt(picked.investmentCost * scale);
        int revenueBoost = Mathf.RoundToInt(picked.revenueBoost * scale);
        int revenueDrop = Mathf.RoundToInt(picked.revenueDrop * scale);
        int assetBoost = Mathf.RoundToInt(picked.assetBoost * scale);

        stock.runtimeAsset -= investmentCost;

        int quarters = Mathf.Max(1, picked.durationQuarters);
        string projectName = eventLabel != null ? $"{eventLabel}: {picked.projectName}" : picked.projectName;

        activeBusinesses.Add(new ActiveBusiness
        {
            projectName = projectName,
            stockName = stock.StockName,
            remainingQuarters = quarters,
            totalQuarters = quarters,
            startedQuarter = CurrentQuarter,
            investmentCost = investmentCost,
            successRate = picked.successRate,
            revenueBoost = revenueBoost,
            revenueDrop = revenueDrop,
            assetBoost = assetBoost,
            successNews = picked.successNews,
            failNews = picked.failNews,
        });

        // 착수 문구 한 줄만 띄우면 어느 회사가 무슨 사업을 언제까지 하는지 알 수 없다.
        // 여기서 회사·사업명·기간·성공확률·성패 시 효과를 한꺼번에 보여줘야
        // 몇 분기 뒤 실적이 튀었을 때 "아, 그때 그 사업 때문이구나"로 이어진다
        string headline = eventLabel != null ? $"[뜬금 이벤트 · {eventLabel}]" : "[신사업]";
        AddNotice($"{headline} {stock.StockName} · {projectName}\n" +
                  $"    {picked.startNews}\n" +
                  $"    투자 {UIFormat.Gold(investmentCost)} · {quarters}분기 뒤({CurrentQuarter + quarters}분기) 결과 발표 · 성공 확률 {picked.successRate}%\n" +
                  $"    성공하면 매출 +{UIFormat.Gold(revenueBoost)} / 실패하면 매출 -{UIFormat.Gold(revenueDrop)}");
    }

    // 이번 분기에 막 시작한 사업 말고, 예전부터 굴러가고 있는 사업을 분기 리포트에 다시 알려준다
    // 결과가 나오는 분기에만 사업 이야기가 보이면 그 사이 분기에는 존재 자체를 잊게 된다
    void ReportOngoingBusinesses()
    {
        foreach (ActiveBusiness biz in activeBusinesses)
        {
            if (biz.startedQuarter == CurrentQuarter) continue; // 방금 착수한 건 위에서 이미 알렸다

            int resultQuarter = CurrentQuarter + biz.remainingQuarters;
            AddNotice($"[진행 중] {biz.stockName} · {biz.projectName}\n" +
                      $"    {biz.totalQuarters}분기 중 {biz.remainingQuarters}분기 남음 → {resultQuarter}분기에 결과 발표 (성공 확률 {biz.successRate}%)");
        }
    }
}
