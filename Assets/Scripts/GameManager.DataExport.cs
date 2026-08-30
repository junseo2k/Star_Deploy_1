using System.IO;
using System.Text;
using UnityEngine;

// GetDailyFeatures()가 반환하는 하루치 상태값 묶음 — CSV 저장(학습용)과 NPCInvestor의 실시간 추론(운영용)이
// 이 구조체 하나를 공유해서 "학습 때 본 데이터"와 "실전에서 보는 데이터"가 어긋나지 않게 한다
public struct StockDailyFeatures
{
    public float priceChangePct;        // 전일 대비 가격 변동률(%)
    public int newsCategory;            // 0: 뉴스 없음, 1: 소형 뉴스, 2: 대형 뉴스
    public bool isForeshadowPending;    // 전조 뉴스가 떠 있는지 (시장 전체 기준)
    public int daysSinceLastSmallNews;  // 마지막 소형 뉴스 이후 지난 날 수
    public int daysSinceLastBigNews;    // 마지막 대형 뉴스 이후 지난 날 수
    public float eraRatePct;            // 시대 보정률 — 수혜면 +, 타격이면 -, 무관이면 0
    public int volatilityType;          // Stock.VolatilityType을 int로 (0 Stable / 1 Normal / 2 Volatile)
    public int quarterDayIndex;         // 이번 분기 중 며칠째인지 (0 ~ QUARTER_LENGTH-1)
    public bool isQuarterEndToday;      // 오늘이 분기 마감일인지
    public bool hasActiveBusiness;      // 현재 진행 중인 사업이 있는지
    public int personality;             // Stock.Personality를 int로
}

// GameManager의 학습 데이터 수집 담당 partial — NPC 예측 모델 학습용 CSV 기록과 자동 시뮬레이션을 다룬다
public partial class GameManager
{
    // 하루가 끝날 때 모든 주식의 상태값을 CSV에 누적 기록 (ML 학습 데이터용)
    // 일일변동/시대변동/소형뉴스/대형뉴스/사업시스템 — 게임의 5가지 가격 변동 원인을 전부 컬럼으로 남긴다
    void SaveStockDataToCSV()
    {
        string path = Path.Combine(Application.persistentDataPath, "stock_history.csv");

        if (!File.Exists(path))
        {
            File.WriteAllText(path,
                "day,stockName,currentPrice,previousPrice,priceChangePct,isAffectedByNews,eraEffect," +
                "newsCategory,isForeshadowPending,daysSinceLastSmallNews,daysSinceLastBigNews," +
                "eraRatePct,volatilityType,quarterDayIndex,isQuarterEndToday,hasActiveBusiness,personality\n");
        }

        // 전조 뉴스 여부는 종목과 무관하게 시장 전체 기준이라 한 번만 계산해서 재사용
        bool foreshadowPending = IsForeshadowPending();

        StringBuilder sb = new();
        foreach (Stock stock in LiveStocks)
        {
            StockDailyFeatures f = GetDailyFeatures(stock, foreshadowPending);
            int eraEffect = IsBoostedByEra(stock) ? 1 : (IsWeakenedByEra(stock) ? -1 : 0);

            sb.AppendLine(
                $"{currentDay}," +
                $"{stock.StockName}," +
                $"{stock.StockCurrentPrice}," +
                $"{stock.StockPreviousPrice}," +
                $"{f.priceChangePct:F2}," +
                $"{(IsAffectedByNews(stock) ? 1 : 0)}," +
                $"{eraEffect}," +
                $"{f.newsCategory}," +
                $"{(f.isForeshadowPending ? 1 : 0)}," +
                $"{f.daysSinceLastSmallNews}," +
                $"{f.daysSinceLastBigNews}," +
                $"{f.eraRatePct:F2}," +
                $"{f.volatilityType}," +
                $"{f.quarterDayIndex}," +
                $"{(f.isQuarterEndToday ? 1 : 0)}," +
                $"{(f.hasActiveBusiness ? 1 : 0)}," +
                $"{f.personality}");
        }

        File.AppendAllText(path, sb.ToString());
    }

    // 특정 종목의 오늘 상태값을 하나로 묶어서 반환 — CSV 저장과 NPCInvestor.OnDayPassed가 똑같이 호출한다
    // (두 곳이 각자 계산하면 미묘하게 어긋날 수 있어서, 반드시 이 함수 하나만 거치게 한다)
    public StockDailyFeatures GetDailyFeatures(Stock stock, bool foreshadowPending)
    {
        float eraRatePct = IsBoostedByEra(stock) ? currentEra.boostedRate
            : IsWeakenedByEra(stock) ? -currentEra.weakenedRate
            : 0f;

        return new StockDailyFeatures
        {
            priceChangePct = stock.ChangePercent,
            newsCategory = GetNewsCategory(stock),
            isForeshadowPending = foreshadowPending,
            daysSinceLastSmallNews = smallNewsTimer,
            daysSinceLastBigNews = currentDay - lastBigNewsDay,
            eraRatePct = eraRatePct,
            volatilityType = (int)stock.volatilityType,
            quarterDayIndex = currentDay % GameBalance.QUARTER_LENGTH,
            isQuarterEndToday = IsQuarterEnd,
            hasActiveBusiness = activeBusinesses.Exists(b => b.stockName == stock.StockName),
            personality = (int)stock.personality,
        };
    }

    // 이 종목에 지금 걸려 있는 뉴스가 없는지/소형인지/대형인지 — IsAffectedByNews(News.cs)를 종류별로 나눈 버전
    int GetNewsCategory(Stock stock)
    {
        bool hasBigNews = activeNewsList.Exists(active =>
            active.caseData.newsType == Case.NewsType.BigTriggerNews
            && active.caseData.industryRates != null
            && System.Array.Exists(active.caseData.industryRates, ir => ir.industryTag == stock.IndustryTag));
        if (hasBigNews) return 2;

        bool hasSmallNews = activeNewsList.Exists(active =>
        {
            Case data = active.caseData;
            if (data.affectedCase != null && System.Array.Exists(data.affectedCase, s => s == stock))
                return true;
            if (data.chainEffects != null && System.Array.Exists(data.chainEffects, e => e.stock == stock))
                return true;
            return false;
        });
        return hasSmallNews ? 1 : 0;
    }

    // 전조 뉴스(대형 뉴스 발생 예고)가 지금 시장에 떠 있는지 — 특정 종목이 아니라 시장 전체 기준
    bool IsForeshadowPending()
    {
        return activeNewsList.Exists(active => active.caseData.newsType == Case.NewsType.ForeshadowNews);
    }

    // 데이터 생성용 임시 함수 — 나중에 삭제
    public void AutoSimulate(int days)
    {
        for (int i = 0; i < days; i++)
            OnNextDayButton();

        Debug.Log($"{days}일 시뮬레이션 완료! CSV 확인하세요.");
    }
}