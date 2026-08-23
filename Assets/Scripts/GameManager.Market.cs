using System.Collections.Generic;
using UnityEngine;

// GameManager의 시장 시뮬레이션 담당 partial — 대출 이자, 매일 가격 변동, NPC 매매, 히스토리 기록을 다룬다
public partial class GameManager
{
    // 대출 이자 차감 — 턴당 대출금의 1%
    void ApplyLoanInterest()
    {
        if (loanAmount <= 0) return;

        int interest = TurnInterest;
        currentgold -= interest;
        UIManager.instance.UpdateGoldText();
        Debug.Log($"이자 {interest}G 차감. 남은 대출금: {loanAmount}G");
    }

    // 매일 발생하는 무작위 가격 변동 — 변동 유형별 폭에 상승 편향을 더해 적용
    // 어제 가격을 StockPreviousPrice에 보관해 변동률 계산 기준으로 삼는다
    void ApplyDailyDrift()
    {
        foreach (Stock stock in LiveStocks)
        {
            stock.StockChangePrice = 0; // 상세 패널에 전날 기록이 남지 않도록 초기화
            stock.StockPreviousPrice = stock.StockCurrentPrice;

            float range = GetVolatilityRange(stock);
            float randomChange = Random.Range(-range, range) + GameBalance.DAILY_BIAS;
            stock.StockCurrentPrice = Mathf.RoundToInt(stock.StockCurrentPrice * (1 + randomChange / 100f));

            ClampPrice(stock);
        }
    }

    // 주식의 변동 유형에 해당하는 일일 변동폭(±%) 반환
    static float GetVolatilityRange(Stock stock)
    {
        return stock.volatilityType switch
        {
            Stock.VolatilityType.Stable => GameBalance.RANGE_STABLE,
            Stock.VolatilityType.Normal => GameBalance.RANGE_NORMAL,
            Stock.VolatilityType.Volatile => GameBalance.RANGE_VOLATILE,
            _ => GameBalance.RANGE_DEFAULT,
        };
    }

    // 주가가 하한 아래로 내려가지 않도록 보정
    static void ClampPrice(Stock stock)
    {
        if (stock.StockCurrentPrice < GameBalance.MIN_STOCK_PRICE)
            stock.StockCurrentPrice = GameBalance.MIN_STOCK_PRICE;
    }

    // 씬의 모든 NPC에게 오늘 주식 데이터를 전달한 뒤 매매 결정 실행
    void RunNpcInvestors()
    {
        NPCInvestor[] npcs = FindObjectsByType<NPCInvestor>(FindObjectsSortMode.None);
        if (npcs.Length == 0) return;

        // 뉴스 영향 여부는 NPC마다 같으므로 한 번만 계산해서 재사용
        Dictionary<Stock, float> newsFlags = new();
        foreach (Stock stock in LiveStocks)
        {
            bool isAffected = activeNewsList.Exists(active =>
                active.caseData.affectedCase != null &&
                System.Array.Exists(active.caseData.affectedCase, s => s == stock));
            newsFlags[stock] = isAffected ? 1f : 0f;
        }

        foreach (NPCInvestor npc in npcs)
        {
            foreach (Stock stock in LiveStocks)
                npc.OnDayPassed(stock, stock.StockChangePrice, newsFlags[stock]);

            npc.DecideAfterAllPredictions();
        }
    }

    // 그래프·NPC 예측에 쓰이는 최근 기록 갱신 (최대 GameBalance.HISTORY_LENGTH일치 유지)
    void RecordStockHistories()
    {
        foreach (Stock stock in LiveStocks)
        {
            AppendCapped(stock.changePctHistory, stock.ChangePercent);
            AppendCapped(stock.priceHistory, stock.StockCurrentPrice);

            // 가짜 거래량 = 변동폭 절댓값 × 임의 계수 (필요하면 계수만 조절하면 됨)
            AppendCapped(stock.volumeHistory, Mathf.Abs(stock.ChangePercent) * GameBalance.FAKE_VOLUME_SCALE);
        }
    }

    // 리스트에 값을 추가하되 길이를 GameBalance.HISTORY_LENGTH 이하로 유지
    static void AppendCapped<T>(List<T> history, T value)
    {
        history.Add(value);
        if (history.Count > GameBalance.HISTORY_LENGTH)
            history.RemoveAt(0);
    }
}
