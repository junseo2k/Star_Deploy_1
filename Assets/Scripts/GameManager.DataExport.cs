using System.IO;
using System.Text;
using UnityEngine;

// GameManager의 학습 데이터 수집 담당 partial — NPC 예측 모델 학습용 CSV 기록과 자동 시뮬레이션을 다룬다
public partial class GameManager
{
    // 하루가 끝날 때 모든 주식의 가격·변동률·뉴스 영향 여부를 CSV에 누적 기록 (ML 학습 데이터용)
    void SaveStockDataToCSV()
    {
        string path = Path.Combine(Application.persistentDataPath, "stock_history.csv");

        if (!File.Exists(path))
        {
            File.WriteAllText(path,
                "day,stockName,currentPrice,previousPrice,priceChangePct,isAffectedByNews,eraEffect\n");
        }

        StringBuilder sb = new();
        foreach (Stock stock in LiveStocks)
        {
            int eraEffect = IsBoostedByEra(stock) ? 1 : (IsWeakenedByEra(stock) ? -1 : 0);

            sb.AppendLine(
                $"{currentDay}," +
                $"{stock.StockName}," +
                $"{stock.StockCurrentPrice}," +
                $"{stock.StockPreviousPrice}," +
                $"{stock.ChangePercent:F2}," +
                $"{(IsAffectedByNews(stock) ? 1 : 0)}," +
                $"{eraEffect}");
        }

        File.AppendAllText(path, sb.ToString());
    }

    // 데이터 생성용 임시 함수 — 나중에 삭제
    public void AutoSimulate(int days)
    {
        for (int i = 0; i < days; i++)
            OnNextDayButton();

        Debug.Log($"{days}일 시뮬레이션 완료! CSV 확인하세요.");
    }
}
