using System.Collections.Generic;
using UnityEngine;

// 플레이어의 보유 주식 상태를 한곳에서 관리하는 런타임 저장소
// 같은 주식이 종족 탭과 산업 탭에 동시에 표시되면 StockControl 인스턴스가 2개 생기므로,
// 보유 수량을 UI 컴포넌트가 아니라 이곳에 두어 수량이 갈라지거나 총 자산에 중복 합산되는 것을 막는다
public static class Portfolio
{
    // 주식 하나에 대한 보유 상태
    public class Holding
    {
        public int amount;              // 보유 수량
        public float avgPurchasePrice;  // 평균 매수 단가
    }

    private static readonly Dictionary<Stock, Holding> holdings = new();

    // 보유 중인(수량 1 이상) 주식 목록 — 총 자산·포트폴리오 화면에서 순회용
    public static IEnumerable<KeyValuePair<Stock, Holding>> All => holdings;

    // 보유 수량 조회 (미보유 시 0)
    public static int GetAmount(Stock stock)
    {
        if (stock == null) return 0;
        return holdings.TryGetValue(stock, out Holding h) ? h.amount : 0;
    }

    // 평균 매수 단가 조회 (미보유 시 0)
    public static float GetAvgPurchasePrice(Stock stock)
    {
        if (stock == null) return 0f;
        return holdings.TryGetValue(stock, out Holding h) ? h.avgPurchasePrice : 0f;
    }

    // 보유 수량 설정 — 0 이하가 되면 목록에서 제거
    public static void SetAmount(Stock stock, int amount)
    {
        if (stock == null) return;

        if (amount <= 0)
        {
            holdings.Remove(stock);
            return;
        }
        GetOrCreate(stock).amount = amount;
    }

    // 평균 매수 단가 설정
    public static void SetAvgPurchasePrice(Stock stock, float avgPurchasePrice)
    {
        if (stock == null) return;
        GetOrCreate(stock).avgPurchasePrice = avgPurchasePrice;
    }

    // 수량·단가를 한 번에 설정 (불러오기에서 사용)
    public static void SetHolding(Stock stock, int amount, float avgPurchasePrice)
    {
        if (stock == null || amount <= 0)
        {
            if (stock != null) holdings.Remove(stock);
            return;
        }
        Holding h = GetOrCreate(stock);
        h.amount = amount;
        h.avgPurchasePrice = avgPurchasePrice;
    }

    // 전체 초기화 — 새 슬롯을 불러오기 전에 이전 슬롯 데이터가 남지 않도록 호출
    // (static이라 씬을 다시 로드해도 값이 유지되기 때문에 반드시 필요)
    public static void Clear()
    {
        holdings.Clear();
    }

    // 보유 주식의 현재 평가금액 합계
    public static long TotalValue()
    {
        long total = 0;
        foreach (KeyValuePair<Stock, Holding> pair in holdings)
            total = MoneyMath.Add(total, (long)pair.Key.StockCurrentPrice * pair.Value.amount);
        return total;
    }

    // 평균 매수가 기준 총 매수 원금
    public static float TotalPurchaseCost()
    {
        float total = 0f;
        foreach (KeyValuePair<Stock, Holding> pair in holdings)
            total += pair.Value.avgPurchasePrice * pair.Value.amount;
        return total;
    }

    private static Holding GetOrCreate(Stock stock)
    {
        if (!holdings.TryGetValue(stock, out Holding h))
        {
            h = new();
            holdings[stock] = h;
        }
        return h;
    }
}
