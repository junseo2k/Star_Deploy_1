using UnityEngine;

// GameManager의 시대 시스템 담당 partial — 시대 힌트·시작·종료와 산업별 매출/가격 보정을 다룬다
public partial class GameManager
{
    // 시대 힌트·시작·종료 처리
    void UpdateEra()
    {
        // 힌트 뉴스 — 시대 시작 며칠 전에 미리 보여줌
        if (currentEra == null
            && nextEraStartDay >= GameBalance.ERA_FIRST_START_DAY
            && currentDay == nextEraStartDay - GameBalance.ERA_HINT_DAYS_BEFORE
            && eraHintShownDay != currentDay)
        {
            eraHintShownDay = currentDay;
            // 다음 시대를 미리 결정해서 힌트로 보여줌 (랜덤은 여기서 한 번만 뽑고 고정)
            pendingEra = allEras[Random.Range(0, allEras.Length)];
            if (pendingEra.hintNews != null)
                AddActiveNews(pendingEra.hintNews);
        }

        // 시대 시작
        if (currentEra == null && currentDay >= nextEraStartDay)
        {
            currentEra = pendingEra != null ? pendingEra : allEras[Random.Range(0, allEras.Length)];
            pendingEra = null;
            eraEndDay = currentDay + GameBalance.ERA_DURATION;
            AddNotice($"[새로운 시대] {currentEra.eraName} 시작! {currentEra.eraDescription}");
            UIManager.instance.UpdateEraText();
        }

        // 시대 종료
        if (currentEra != null && currentDay >= eraEndDay)
        {
            AddNotice($"{currentEra.eraName}이(가) 막을 내렸습니다.");
            currentEra = null;
            nextEraStartDay = currentDay + GameBalance.ERA_COOLDOWN;
            UIManager.instance.UpdateEraText();
        }
    }

    // 시대에 따른 산업별 가격 보정
    void ApplyEraPriceEffects()
    {
        if (currentEra == null) return;

        foreach (Stock stock in LiveStocks)
        {
            if (IsBoostedByEra(stock))
                stock.StockCurrentPrice = Mathf.RoundToInt(stock.StockCurrentPrice * (1 + currentEra.boostedRate / 100f));
            else if (IsWeakenedByEra(stock))
                stock.StockCurrentPrice = Mathf.RoundToInt(stock.StockCurrentPrice * (1 - currentEra.weakenedRate / 100f));

            ClampPrice(stock);
        }
    }

    // 지금 이 회사의 매출에 걸려 있는 시대 배율 — 수혜 1.2 / 무관 1.0 / 타격 0.8
    public float GetEraRevenueModifier(Stock stock)
    {
        if (IsBoostedByEra(stock)) return GameBalance.ERA_REVENUE_BOOST;
        if (IsWeakenedByEra(stock)) return GameBalance.ERA_REVENUE_PENALTY;
        return 1.0f;
    }

    // 지금 이 회사의 "새 사업 착수 확률"에 걸려 있는 시대 배율 — 수혜 1.5 / 무관 1.0 / 타격 0.6
    // 제약의 시대면 제약 회사들이 평소보다 자주 신사업을 벌이도록 TryStartBusiness에서 곱해준다
    public float GetEraBusinessChanceModifier(Stock stock)
    {
        if (IsBoostedByEra(stock)) return GameBalance.ERA_BUSINESS_CHANCE_BOOST;
        if (IsWeakenedByEra(stock)) return GameBalance.ERA_BUSINESS_CHANCE_PENALTY;
        return 1.0f;
    }

    // 현재 시대가 이 주식의 산업군을 밀어주는지 여부
    public bool IsBoostedByEra(Stock stock)
    {
        return currentEra != null
            && System.Array.Exists(currentEra.boostedIndustries, t => t == stock.IndustryTag);
    }

    // 현재 시대가 이 주식의 산업군을 누르는지 여부
    public bool IsWeakenedByEra(Stock stock)
    {
        return currentEra != null
            && System.Array.Exists(currentEra.weakenedIndustries, t => t == stock.IndustryTag);
    }
}
