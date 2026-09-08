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

    // 목표 주가 ÷ 현재 주가 — 1보다 크면 저평가(오를 여지), 작으면 고평가
    //
    // 분기말 주가 변동은 ApplyFinancialGravity의 (목표가 - 현재가) × QUARTER_SHOCK 가 사실상 전부다.
    // 그런데 모델에는 매출·수입·목표가가 하나도 안 들어가서, 지금 주가가 싼지 비싼지 알 방법이 없었다.
    // 사업 성패 자체는 난수라 못 맞히지만, "지금까지 쌓인 실적 대비 주가가 어디쯤인가"는 알 수 있고
    // 그게 분기말 변동의 방향을 정하는 값이다. 플레이어도 상세 패널에서 보는 정보라 불공평하지도 않다
    public float fairPriceRatio;
}

// GameManager의 학습 데이터 수집 담당 partial — NPC 예측 모델 학습용 CSV 기록과 자동 시뮬레이션을 다룬다
public partial class GameManager
{
    // 이번 플레이를 식별하는 값 — 게임을 켤 때(정확히는 이 GameManager가 만들어질 때) 한 번만 정해진다
    //
    // CSV는 File.AppendAllText로 계속 쌓이기만 하므로, 여러 판을 돌리면 같은 day 값이 여러 번 들어간다.
    // 이 표시가 없으면 Model.py가 날짜순으로 정렬해 슬라이딩 윈도우를 만들 때 서로 다른 판의 행이
    // 한 윈도우에 섞여 들어간다 (실제로 그래서 학습이 망가진 적이 있다).
    // runId로 판을 갈라두면 여러 판을 안전하게 누적할 수 있다
    private string runId;

    [Header("학습 데이터 수집")]
    [Tooltip("CollectTrainingData()가 돌릴 판 수")]
    public int dataRunCount = 5;
    [Tooltip("판당 진행할 날짜 수 — 실제 게임 한 판 길이(20분기=400턴)와 비슷하게 잡는 것이 좋다")]
    public int dataRunDays = 500;

    // runId를 아직 안 정했으면 지금 정한다 — 첫 CSV 기록 시점에 딱 한 번 실행된다
    string RunId
    {
        get
        {
            if (string.IsNullOrEmpty(runId))
                runId = System.DateTime.Now.ToString("yyMMdd_HHmmss");
            return runId;
        }
    }

    // 하루가 끝날 때 모든 주식의 상태값을 CSV에 누적 기록 (ML 학습 데이터용)
    // 일일변동/시대변동/소형뉴스/대형뉴스/사업시스템 — 게임의 5가지 가격 변동 원인을 전부 컬럼으로 남긴다
    void SaveStockDataToCSV()
    {
        string path = Path.Combine(Application.persistentDataPath, "stock_history.csv");

        if (!File.Exists(path))
        {
            File.WriteAllText(path,
                "runId,day,stockName,currentPrice,previousPrice,priceChangePct,isAffectedByNews,eraEffect," +
                "newsCategory,isForeshadowPending,daysSinceLastSmallNews,daysSinceLastBigNews," +
                "eraRatePct,volatilityType,quarterDayIndex,isQuarterEndToday,hasActiveBusiness,personality," +
                "fairPriceRatio\n");
        }

        // 전조 뉴스 여부는 종목과 무관하게 시장 전체 기준이라 한 번만 계산해서 재사용
        bool foreshadowPending = IsForeshadowPending();

        StringBuilder sb = new();
        foreach (Stock stock in LiveStocks)
        {
            StockDailyFeatures f = GetDailyFeatures(stock, foreshadowPending);
            int eraEffect = IsBoostedByEra(stock) ? 1 : (IsWeakenedByEra(stock) ? -1 : 0);

            sb.AppendLine(
                $"{RunId}," +
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
                $"{f.personality}," +
                $"{f.fairPriceRatio:F4}");
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
            // 0으로 나누는 것을 막는다 — 주가는 MIN_STOCK_PRICE 이상이라 실제로는 걸릴 일이 없지만 안전하게
            fairPriceRatio = stock.StockCurrentPrice > 0
                ? (float)GetFairPrice(stock) / stock.StockCurrentPrice
                : 1f,
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

    // ── 학습 데이터 수집 ──────────────────────────────

    // 여러 판을 연달아 돌려서 학습 데이터를 모은다 — 버튼 하나에 연결해서 쓰면 된다
    //
    // 한 판을 길게(2500일) 돌리는 것보다 짧은 판(500일)을 여러 번 돌리는 쪽이 낫다:
    //   ① 플레이어가 실제로 겪는 초반 구간 데이터가 많아진다 (한 판을 길게 돌리면 대부분이 후반 시장이 된다)
    //   ② 시대·뉴스·사업이 매번 다르게 굴러가서 다양한 상황이 모인다
    //   ③ 주가가 매일 오르는 구조라, 한 판이 길수록 후반 주가가 학습 정규화 범위를 벗어난다
    //
    // 판을 바꿀 때마다 게임 상태를 처음으로 되돌리고 runId도 새로 발급한다.
    // 이걸 안 하면 CSV에 여러 판이 한 판처럼 이어져 기록되어, 학습할 때 서로 다른 판의 날짜가
    // 한 윈도우에 섞여 들어간다 (실제로 그렇게 학습했다가 방향 정확도가 0.53까지 떨어진 적이 있다)
    public void CollectTrainingData()
    {
        for (int run = 1; run <= dataRunCount; run++)
        {
            ResetForNewRun();
            for (int day = 0; day < dataRunDays; day++)
                OnNextDayButton();

            Debug.Log($"[학습 데이터] {run}/{dataRunCount}판 완료 ({dataRunDays}일)");
        }

        Debug.Log($"[학습 데이터] 전체 완료 — {dataRunCount}판 × {dataRunDays}일\n" +
                  $"    {Path.Combine(Application.persistentDataPath, "stock_history.csv")}\n" +
                  $"    이 파일을 Model/ 폴더로 복사한 뒤 python Model.py 를 실행하세요.");
    }

    // 게임을 새 판 시작 상태로 되돌린다 — CollectTrainingData가 판을 바꿀 때 호출한다
    // 세이브 파일은 건드리지 않는다 (메모리 상태만 초기화한다)
    void ResetForNewRun()
    {
        runId = null; // 다음 CSV 기록 때 새 runId가 발급된다

        currentDay = 1;
        currentgold = GameBalance.START_GOLD;
        loanAmount = 0;
        lowGoldStreak = 0;

        // 뉴스
        activeNewsList.Clear();
        pendingNewsList.Clear();
        smallNewsTimer = 0;
        lastBigNewsDay = -1;
        scheduledBigNewsDay = -1;
        scheduledBigNewsVibeDay = -1;
        scheduledBigNewsCaseIndex = -1;
        scheduledForeshadowIndex = -1;
        scheduledNationPair = "";

        // 시대
        currentEra = null;
        pendingEra = null;
        eraEndDay = -1;
        nextEraStartDay = GameBalance.ERA_FIRST_START_DAY;
        eraHintShownDay = -1;

        // 사업 · 분기 기록
        activeBusinesses.Clear();
        quarterChangeReasons.Clear();
        lastQuarterEraModifier.Clear();
        lastQuarterRevenue.Clear();

        // 보유 주식과 종목 런타임 값(매출·금고·상폐 여부·히스토리)을 인스펙터 원본으로 되돌린다
        Portfolio.Clear();
        ResetStocksToBase();

        ScheduleNextBigNews(); // 새 판의 대형 뉴스 일정을 다시 잡는다
    }
}