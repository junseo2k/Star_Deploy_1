using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// 게임 전체 흐름을 관리하는 싱글톤 클래스 — 골드·날짜·사건 처리·저장/불러오기를 담당
public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // 어디서든 GameManager에 접근하기 위한 싱글톤 인스턴스

    [Header("기본 상태")]
    public int currentgold;  // 현재 보유 골드
    public int currentDay;   // 현재 게임 날짜
    public int loanAmount;   // 현재 대출금
    public int lowGoldStreak; // 골드가 기준치 이하인 연속 턴 수 (기준 도달 시 게임 오버)

    [Header("참조")]
    public TabManager tabmanager; // 모든 주식 목록(allstock)에 접근하기 위한 참조

    [Header("사건 데이터")]
    public Case[] AllCases;                                          // 게임에 등록된 모든 사건(Case ScriptableObject) 배열
    public List<Case> recentIssues = new List<Case>();               // 최근 발생한 사건 이력
    public List<ActiveCase> activeNewsList = new List<ActiveCase>(); // 현재 진행 중인 활성 뉴스 목록
    public List<Case> SmallNewsCases = new List<Case>();             // SmallIndustryNews·SmallCompanyNews 타입 케이스 목록
    public List<Case> BigTriggerCases = new List<Case>();            // BigTriggerNews 타입 케이스 목록

    [Header("대형 뉴스 스케줄")]
    public int lastBigNewsDay = -1;          // 마지막 대형 뉴스가 실제로 발생한 날짜
    public int scheduledBigNewsDay = -1;     // 예약된 대형 뉴스 발생 날짜
    public int scheduledBigNewsVibeDay = -1; // 전조 뉴스 발생 날짜 (대형 뉴스 며칠 전)
    public int scheduledBigNewsCaseIndex = -1; // 예약된 대형 뉴스의 케이스 인덱스
    public int scheduledForeshadowIndex = -1;  // 예약된 전조 뉴스의 인덱스
    public string scheduledNationPair = "";    // 대형 뉴스에 사용할 국가 쌍 (전조 뉴스와 공유)

    [Header("소형 뉴스")]
    public NewsTemplate[] newsTemplates;                                // 소형 뉴스 생성용 템플릿 목록
    public int smallNewsTimer;                                          // 소형 뉴스 생성 주기 타이머
    public List<PendingNews> pendingNewsList = new List<PendingNews>(); // 불확정 뉴스 대기열

    [Header("시대 시스템")]
    public Era[] allEras;              // 전체 시대 목록 (Inspector에 등록)
    public Era currentEra;             // 현재 진행 중인 시대 (없으면 null)
    public int eraEndDay = -1;         // 현재 시대 종료 날짜
    public int nextEraStartDay = 20;   // 다음 시대 시작 가능 날짜 (최초 20턴)
    public int eraHintShownDay = -1;   // 힌트 뉴스를 보여준 날짜 (중복 출력 방지)

    [Header("사업 시스템")]
    public BusinessProject[] allBusinessProjects;                             // 전체 사업 템플릿 목록 (인스펙터 등록)
    public List<ActiveBusiness> activeBusinesses = new List<ActiveBusiness>(); // 현재 진행 중인 사업

    // ── 게임 밸런스 상수 ──────────────────────────────
    private const int START_GOLD = 5000;              // 새 게임 시작 골드
    private const int LOW_GOLD_THRESHOLD = 1000;      // 이 골드 이하가 연속되면 게임 오버 카운트 증가
    private const int LOW_GOLD_GAMEOVER_STREAK = 5;   // 게임 오버까지의 연속 턴 수
    private const float LOAN_INTEREST_RATE = 0.01f;   // 턴당 대출 이자율 (1%)
    private const int LOAN_MAX_RATIO_DIVISOR = 2;     // 대출 한도 = 보유 골드 / 이 값 (50%)

    private const int SMALL_NEWS_MIN_INTERVAL = 3;    // 소형 뉴스 최소 발생 주기 (턴)
    private const int SMALL_NEWS_MAX_INTERVAL = 6;    // 소형 뉴스 최대 발생 주기 (상한 제외)
    private const int UNCERTAIN_NEWS_DELAY = 2;       // 불확정 뉴스 효과가 나타나기까지 걸리는 턴 수
    private const int BIG_NEWS_MIN_INTERVAL = 30;     // 대형 뉴스 최소 예약 간격 (턴)
    private const int BIG_NEWS_MAX_INTERVAL = 51;     // 대형 뉴스 최대 예약 간격 (상한 제외)
    private const int FORESHADOW_DEFAULT_LEAD = 2;    // 전조 뉴스 지속일이 없을 때 사용할 기본 선행 턴 수

    private const int ERA_FIRST_START_DAY = 20;       // 시대 시스템 최초 발생 가능 날짜
    private const int ERA_DURATION = 15;              // 시대 지속 턴 수
    private const int ERA_COOLDOWN = 5;               // 시대 종료 후 휴지기 턴 수
    private const int ERA_HINT_DAYS_BEFORE = 2;       // 힌트 뉴스를 며칠 전에 보여줄지
    private const float ERA_REVENUE_BOOST = 1.2f;     // 유망 산업 매출 배율
    private const float ERA_REVENUE_PENALTY = 0.8f;   // 비선호 산업 매출 배율

    private const int BUSINESS_MIN_INTERVAL = 8;      // 신사업 착수 최소 주기 (턴)
    private const int BUSINESS_MAX_INTERVAL = 15;     // 신사업 착수 최대 주기 (상한 제외)

    private const float DAILY_BIAS = 0.15f;           // 매일 가격에 더해지는 상승 편향 (%)
    private const float RANGE_STABLE = 0.5f;          // 안정형 일일 변동폭 (±%)
    private const float RANGE_NORMAL = 1f;          // 보통형 일일 변동폭 (±%)
    private const float RANGE_VOLATILE = 1.5f;          // 고변동형 일일 변동폭 (±%)
    private const float RANGE_DEFAULT = 2f;           // 변동 유형이 없을 때의 기본값

    private const int HISTORY_LENGTH = 10;            // 그래프·NPC 예측에 쓰는 최근 기록 보관 일수
    private const float FAKE_VOLUME_SCALE = 50f;      // 가짜 거래량 계수 (변동폭 절댓값 × 이 값)
    private const int MIN_STOCK_PRICE = 1;            // 주가 하한 (0 이하로 내려가지 않도록)

    private Era pendingEra;                                 // 힌트 뉴스로 예고된, 아직 시작 안 한 시대
    private int businessTimer;                              // 사업 착수 주기 타이머
    private List<string> dailyNotices = new List<string>(); // 오늘 발생한 알림 메시지 모음 (줄바꿈으로 이어서 표시)

    // 진행 중인 사업 상태를 추적하는 클래스
    [System.Serializable]
    public class ActiveBusiness
    {
        public string projectName;  // 사업 이름 (저장/식별용)
        public string stockName;    // 대상 회사 이름 (저장용)
        public int remainingTurns;  // 남은 턴 수
        public int investmentCost;  // 매턴 차감할 투자 비용
        public int successRate;     // 성공 확률
        public int revenueBoost;    // 성공 시 baseRevenue 증가량
        public string successNews;  // 성공 뉴스 문구
        public string failNews;     // 실패 뉴스 문구
    }

    // JSON 저장을 위한 주식 데이터 구조체
    [System.Serializable]
    public class StockSaveData
    {
        public string stockname;       // 주식 이름 (식별 키)
        public int currentPrice;       // 저장 시점의 주식 가격
        public int ownamount;          // 보유 수량
        public float avgPurchasePrice; // 평균 매수 단가
        public bool isFavorite;        // 즐겨찾기 여부
        public int revenue;            // 매출액
        public int operatingProfit;    // 영업이익
        public int baseRevenue;        // 기본 매출
        public int baseCost;           // 기본 비용
    }

    // JSON 저장을 위한 게임 전체 데이터 구조체
    [System.Serializable]
    public class GameSaveData
    {
        public int currentgold;
        public int currentday;
        public List<StockSaveData> OwnStocks;
        public int currentPrice;
        public int lastBigNewsDay;
        public int scheduledBigNewsDay;
        public int scheduledBigNewsVibeDay;
        public int scheduledBigNewsCaseIndex;
        public int scheduledForeshadowIndex;
        public string scheduledNationPair;
        public int smallNewsTimer;
        public int loanAmount;
        public int eraEndDay;
        public int nextEraStartDay;
        public int eraHintShownDay;
        public string currentEraName; // ScriptableObject는 직렬화 안 되니 이름으로 저장
        public string pendingEraName;
        public List<ActiveBusiness> activeBusinesses;
        public int businessTimer;
    }

    // ── 초기화 ────────────────────────────────────────

    // Start보다 먼저 실행 — 싱글톤 인스턴스 등록
    void Awake()
    {
        Instance = this; // 외부에서 GameManager.Instance로 접근 가능하도록 자신을 등록
    }

    // 저장 데이터 로드 → UI 갱신 → AllCases를 타입별로 분류 → 대형 뉴스 스케줄 설정
    void Start()
    {
        LoadGame();
        ClassifyCases();

        UIManager.instance.UpdateDayText();
        UIManager.instance.UpdateGoldText();
        UIManager.instance.UpdateEraText();

        if (scheduledBigNewsDay <= currentDay) // 저장 데이터에 유효한 스케줄이 없으면 새로 예약
            ScheduleNextBigNews();
    }

    // AllCases를 뉴스 타입별 목록으로 분류
    // 인스펙터에 값이 남아 있을 수 있으므로 먼저 비우고 채운다
    void ClassifyCases()
    {
        SmallNewsCases.Clear();
        BigTriggerCases.Clear();

        foreach (Case c in AllCases)
        {
            switch (c.newsType)
            {
                case Case.NewsType.SmallIndustryNews:
                case Case.NewsType.SmallCompanyNews: // 소형 뉴스는 하나의 목록으로 합침
                    SmallNewsCases.Add(c);
                    break;
                case Case.NewsType.BigTriggerNews:
                    BigTriggerCases.Add(c);
                    break;
            }
        }
    }

    // ── 저장 / 불러오기 ───────────────────────────────

    // 선택된 슬롯 번호 기반으로 저장 경로 반환
    string GetSavePath()
    {
        int slot = PlayerPrefs.GetInt("SelectedSlot", 1);
        return Path.Combine(Application.persistentDataPath, $"save{slot}.json");
    }

    // 현재 게임 상태를 JSON 파일로 저장
    public void SaveGame()
    {
        GameSaveData data = new GameSaveData
        {
            currentgold = currentgold,
            currentday = currentDay,
            smallNewsTimer = smallNewsTimer,
            loanAmount = loanAmount,
            eraEndDay = eraEndDay,
            nextEraStartDay = nextEraStartDay,
            eraHintShownDay = eraHintShownDay,
            currentEraName = currentEra != null ? currentEra.eraName : "",
            pendingEraName = pendingEra != null ? pendingEra.eraName : "",
            activeBusinesses = activeBusinesses,
            businessTimer = businessTimer,
            lastBigNewsDay = lastBigNewsDay,
            scheduledBigNewsDay = scheduledBigNewsDay,
            scheduledBigNewsVibeDay = scheduledBigNewsVibeDay,
            scheduledBigNewsCaseIndex = scheduledBigNewsCaseIndex,
            scheduledForeshadowIndex = scheduledForeshadowIndex,
            scheduledNationPair = scheduledNationPair,
            OwnStocks = new List<StockSaveData>(),
        };

        // 보유 여부와 무관하게 모든 주식의 현재 상태를 저장 (가격·실적이 매턴 바뀌므로)
        foreach (Stock stock in tabmanager.allstock)
        {
            data.OwnStocks.Add(new StockSaveData
            {
                stockname = stock.StockName,
                currentPrice = stock.StockCurrentPrice,
                isFavorite = stock.isFavorite,
                revenue = stock.revenue,
                operatingProfit = stock.OperatingProfit,
                baseRevenue = stock.runtimeBaseRevenue, // 사업 보너스가 누적된 런타임 값을 저장
                baseCost = stock.baseCost,
                ownamount = Portfolio.GetAmount(stock),
                avgPurchasePrice = Portfolio.GetAvgPurchasePrice(stock),
            });
        }

        File.WriteAllText(GetSavePath(), JsonUtility.ToJson(data));
    }

    // JSON 파일에서 게임 데이터를 불러와 복원
    void LoadGame()
    {
        // Portfolio는 static이라 씬을 다시 로드해도 값이 남으므로, 불러오기 전에 항상 비운다
        Portfolio.Clear();

        // 먼저 모든 주식을 기본값으로 세팅 — 저장 이후에 새로 추가된 주식도 0G로 시작하지 않도록
        ResetStocksToBase();

        string path = GetSavePath();
        if (!File.Exists(path))
        {
            currentgold = START_GOLD;
            currentDay = 1;
            return;
        }

        GameSaveData data = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(path));
        currentgold = data.currentgold;
        currentDay = data.currentday;
        lastBigNewsDay = data.lastBigNewsDay;
        scheduledBigNewsDay = data.scheduledBigNewsDay;
        scheduledBigNewsVibeDay = data.scheduledBigNewsVibeDay;
        scheduledBigNewsCaseIndex = data.scheduledBigNewsCaseIndex;
        scheduledForeshadowIndex = data.scheduledForeshadowIndex;
        scheduledNationPair = data.scheduledNationPair ?? "";
        smallNewsTimer = data.smallNewsTimer;
        loanAmount = data.loanAmount;
        eraEndDay = data.eraEndDay;
        nextEraStartDay = data.nextEraStartDay > 0 ? data.nextEraStartDay : ERA_FIRST_START_DAY;
        eraHintShownDay = data.eraHintShownDay;
        currentEra = System.Array.Find(allEras, e => e.eraName == data.currentEraName);
        pendingEra = System.Array.Find(allEras, e => e.eraName == data.pendingEraName);
        activeBusinesses = data.activeBusinesses ?? new List<ActiveBusiness>();
        businessTimer = data.businessTimer;

        if (data.OwnStocks == null) return;

        // 이름으로 주식을 찾기 위한 색인 — 저장 목록과 이중 루프를 도는 것을 피함
        Dictionary<string, Stock> stocksByName = new Dictionary<string, Stock>();
        foreach (Stock stock in tabmanager.allstock)
            stocksByName[stock.StockName] = stock;

        foreach (StockSaveData saved in data.OwnStocks)
        {
            if (!stocksByName.TryGetValue(saved.stockname, out Stock stock)) continue;

            stock.StockCurrentPrice = saved.currentPrice;
            stock.StockPreviousPrice = saved.currentPrice;
            stock.isFavorite = saved.isFavorite;
            stock.revenue = saved.revenue;
            stock.OperatingProfit = saved.operatingProfit;
            stock.runtimeBaseRevenue = saved.baseRevenue;
            // baseCost는 인스펙터 고정값이라 런타임에 바뀌지 않는다
            // 세이브에서 되돌리면 에셋에 옛 값이 덮여 밸런스 조정이 무효화되므로 복원하지 않는다

            Portfolio.SetHolding(stock, saved.ownamount, saved.avgPurchasePrice);
        }
    }

    // 모든 주식의 런타임 값을 인스펙터 기준값으로 되돌린다
    // Stock은 ScriptableObject라 에디터에서 값이 남으므로 불러오기 전에 반드시 초기화해야 한다
    void ResetStocksToBase()
    {
        foreach (Stock stock in tabmanager.allstock)
        {
            stock.StockCurrentPrice = stock.StockBasePrice;
            stock.StockPreviousPrice = stock.StockBasePrice;
            // 사업 보너스가 쌓인 런타임 기준값을 에셋 원본으로 되돌린다 (도메인 리로드 여부와 무관하게 항상 실행)
            stock.runtimeBaseRevenue = stock.baseRevenue;
            stock.revenue = stock.runtimeBaseRevenue;
            stock.OperatingProfit = stock.runtimeBaseRevenue - stock.baseCost;
            // 즐겨찾기는 세이브 대상이지만 에셋에도 남으므로 먼저 끈다
            // 세이브가 있으면 바로 아래 LoadGame이 저장된 값으로 다시 켜준다
            stock.isFavorite = false;

            stock.changePctHistory.Clear();
            stock.priceHistory.Clear();
            stock.volumeHistory.Clear();
        }
    }

    // ── 골드 / 대출 ───────────────────────────────────

    // 주식 구매/판매 시 골드 증감 처리 (구매 시 음수, 판매 시 양수 전달)
    public void UpdateGold(int amount)
    {
        currentgold += amount;
    }

    // 현재 대출 한도 — 보유 골드의 50%
    public int MaxLoan => currentgold / LOAN_MAX_RATIO_DIVISOR;

    // 이번 턴에 부과될 대출 이자
    public int TurnInterest => Mathf.CeilToInt(loanAmount * LOAN_INTEREST_RATE);

    // 대출 실행 — 자산의 50%까지만 가능
    public bool TakeLoan(int amount)
    {
        if (amount > MaxLoan)
        {
            Debug.Log("대출 한도 초과");
            return false;
        }
        loanAmount += amount;
        currentgold += amount;
        UIManager.instance.UpdateGoldText();
        SaveGame();
        return true;
    }

    // 상환 — 보유 골드보다 많이 상환 불가
    public bool RepayLoan(int amount)
    {
        if (amount > currentgold)
        {
            Debug.Log("골드 부족");
            return false;
        }
        if (amount > loanAmount) amount = loanAmount; // 대출금 초과 상환 방지
        loanAmount -= amount;
        currentgold -= amount;
        UIManager.instance.UpdateGoldText();
        SaveGame();
        return true;
    }

    // ── 하루 진행 ─────────────────────────────────────

    // 다음 날 버튼 클릭 시 호출 — 하루 동안 일어나는 모든 처리를 순서대로 실행
    // 각 단계의 순서에 의미가 있으므로(예: 시대 효과 → 실적 재계산 → 가격 변동) 호출 순서를 바꾸지 말 것
    public void OnNextDayButton()
    {
        dailyNotices.Clear();

        AdvanceActiveNews();
        AdvancePendingNews();

        currentDay += 1;

        ApplyLoanInterest();
        TryGenerateSmallNews();
        TriggerScheduledNews();
        UpdateEra();
        ApplyEraPriceEffects();
        RecalculateFinancials();
        ProcessBusinesses();
        ApplyDailyDrift();
        ApplyActiveNewsEffects();

        activeNewsList.RemoveAll(active => active.Rday <= 0); // 지속일이 끝난 뉴스 제거

        RunNpcInvestors();
        UIManager.instance.UpdateDayText();

        PayDividends();
        RecordStockHistories();
        CheckGameOverConditions();

        SaveStockDataToCSV();
        UIManager.instance.UpdateTotalAssetText();
        UIManager.instance.favoritePanel.RefreshHome();
    }

    // 알림 메시지를 오늘 목록에 추가하고, 지금까지 쌓인 메시지를 줄바꿈으로 이어서 NoticeText에 표시
    private void AddNotice(string message)
    {
        dailyNotices.Add(message);
        UIManager.instance.NoticeText.text = string.Join("\n", dailyNotices);
        Debug.Log($"[알림 {dailyNotices.Count}개] {string.Join(" | ", dailyNotices)}");
    }

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

    // 대출 이자 차감 — 턴당 대출금의 1%
    void ApplyLoanInterest()
    {
        if (loanAmount <= 0) return;

        int interest = TurnInterest;
        currentgold -= interest;
        UIManager.instance.UpdateGoldText();
        Debug.Log($"이자 {interest}G 차감. 남은 대출금: {loanAmount}G");
    }

    // 소형 뉴스 생성 — 3~5턴 주기로 템플릿에서 뉴스를 하나 만들어 낸다
    void TryGenerateSmallNews()
    {
        smallNewsTimer++;
        if (newsTemplates.Length == 0) return;
        if (smallNewsTimer < Random.Range(SMALL_NEWS_MIN_INTERVAL, SMALL_NEWS_MAX_INTERVAL)) return;

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
                daysLeft = UNCERTAIN_NEWS_DELAY,
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

    // 시대 힌트·시작·종료 처리
    void UpdateEra()
    {
        // 힌트 뉴스 — 시대 시작 며칠 전에 미리 보여줌
        if (currentEra == null
            && nextEraStartDay >= ERA_FIRST_START_DAY
            && currentDay == nextEraStartDay - ERA_HINT_DAYS_BEFORE
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
            eraEndDay = currentDay + ERA_DURATION;
            AddNotice($"[새로운 시대] {currentEra.eraName} 시작! {currentEra.eraDescription}");
            UIManager.instance.UpdateEraText();
        }

        // 시대 종료
        if (currentEra != null && currentDay >= eraEndDay)
        {
            AddNotice($"{currentEra.eraName}이(가) 막을 내렸습니다.");
            currentEra = null;
            nextEraStartDay = currentDay + ERA_COOLDOWN;
            UIManager.instance.UpdateEraText();
        }
    }

    // 시대에 따른 산업별 가격 보정
    void ApplyEraPriceEffects()
    {
        if (currentEra == null) return;

        foreach (Stock stock in tabmanager.allstock)
        {
            if (IsBoostedByEra(stock))
                stock.StockCurrentPrice = Mathf.RoundToInt(stock.StockCurrentPrice * (1 + currentEra.boostedRate / 100f));
            else if (IsWeakenedByEra(stock))
                stock.StockCurrentPrice = Mathf.RoundToInt(stock.StockCurrentPrice * (1 - currentEra.weakenedRate / 100f));

            ClampPrice(stock);
        }
    }

    // 매일 발생하는 무작위 가격 변동 — 변동 유형별 폭에 상승 편향을 더해 적용
    // 어제 가격을 StockPreviousPrice에 보관해 변동률 계산 기준으로 삼는다
    void ApplyDailyDrift()
    {
        foreach (Stock stock in tabmanager.allstock)
        {
            stock.StockChangePrice = 0; // 상세 패널에 전날 기록이 남지 않도록 초기화
            stock.StockPreviousPrice = stock.StockCurrentPrice;

            float range = GetVolatilityRange(stock);
            float randomChange = Random.Range(-range, range) + DAILY_BIAS;
            stock.StockCurrentPrice = Mathf.RoundToInt(stock.StockCurrentPrice * (1 + randomChange / 100f));

            ClampPrice(stock);
        }
    }

    // 주식의 변동 유형에 해당하는 일일 변동폭(±%) 반환
    static float GetVolatilityRange(Stock stock)
    {
        switch (stock.volatilityType)
        {
            case Stock.VolatilityType.Stable: return RANGE_STABLE;
            case Stock.VolatilityType.Normal: return RANGE_NORMAL;
            case Stock.VolatilityType.Volatile: return RANGE_VOLATILE;
            default: return RANGE_DEFAULT;
        }
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
        foreach (Stock stock in tabmanager.allstock)
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
            if (effect.stock == null) continue;

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

    // 주가가 하한 아래로 내려가지 않도록 보정
    static void ClampPrice(Stock stock)
    {
        if (stock.StockCurrentPrice < MIN_STOCK_PRICE)
            stock.StockCurrentPrice = MIN_STOCK_PRICE;
    }

    // 씬의 모든 NPC에게 오늘 주식 데이터를 전달한 뒤 매매 결정 실행
    void RunNpcInvestors()
    {
        NPCInvestor[] npcs = FindObjectsByType<NPCInvestor>(FindObjectsSortMode.None);
        if (npcs.Length == 0) return;

        // 뉴스 영향 여부는 NPC마다 같으므로 한 번만 계산해서 재사용
        Dictionary<Stock, float> newsFlags = new Dictionary<Stock, float>();
        foreach (Stock stock in tabmanager.allstock)
        {
            bool isAffected = activeNewsList.Exists(active =>
                active.caseData.affectedCase != null &&
                System.Array.Exists(active.caseData.affectedCase, s => s == stock));
            newsFlags[stock] = isAffected ? 1f : 0f;
        }

        foreach (NPCInvestor npc in npcs)
        {
            foreach (Stock stock in tabmanager.allstock)
                npc.OnDayPassed(stock, stock.StockChangePrice, newsFlags[stock]);

            npc.DecideAfterAllPredictions();
        }
    }

    // 배당주 배당금 지급 — 영업이익이 0 이하면 삭감
    void PayDividends()
    {
        foreach (Stock stock in tabmanager.allstock)
        {
            if (!stock.hasDividend) continue;

            // 실적 악화 시 배당 삭감
            if (stock.OperatingProfit <= 0)
            {
                AddNotice($"[배당 삭감] {stock.StockName} 실적 악화로 이번 턴 배당이 없습니다.");
                continue;
            }

            int owned = Portfolio.GetAmount(stock);
            if (owned <= 0) continue;

            // 배당금 = 현재가 × 보유수량 × 배당률
            int dividend = Mathf.RoundToInt(stock.StockCurrentPrice * owned * stock.dividendRate);
            if (dividend <= 0) continue;

            UpdateGold(dividend);
            AddNotice($"[배당] {stock.StockName} {owned}주 → +{dividend} G");
        }
    }

    // 그래프·NPC 예측에 쓰이는 최근 기록 갱신 (최대 HISTORY_LENGTH일치 유지)
    void RecordStockHistories()
    {
        foreach (Stock stock in tabmanager.allstock)
        {
            AppendCapped(stock.changePctHistory, stock.ChangePercent);
            AppendCapped(stock.priceHistory, stock.StockCurrentPrice);

            // 가짜 거래량 = 변동폭 절댓값 × 임의 계수 (필요하면 계수만 조절하면 됨)
            AppendCapped(stock.volumeHistory, Mathf.Abs(stock.ChangePercent) * FAKE_VOLUME_SCALE);
        }
    }

    // 리스트에 값을 추가하되 길이를 HISTORY_LENGTH 이하로 유지
    static void AppendCapped<T>(List<T> history, T value)
    {
        history.Add(value);
        if (history.Count > HISTORY_LENGTH)
            history.RemoveAt(0);
    }

    // 게임 오버 조건 검사 — 골드 고갈 연속 지속, 또는 대출금을 감당하지 못하는 경우
    void CheckGameOverConditions()
    {
        if (currentgold <= LOW_GOLD_THRESHOLD)
        {
            lowGoldStreak++;
            if (lowGoldStreak >= LOW_GOLD_GAMEOVER_STREAK)
            {
                LoadGameOverScene();
                return;
            }
        }
        else
        {
            lowGoldStreak = 0;
        }

        // 골드가 대출금보다 적으면 상환 불가 → 게임 오버
        if (loanAmount > 0 && currentgold < loanAmount)
            LoadGameOverScene();
    }

    void LoadGameOverScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameOver");
    }

    // ── 뉴스 스케줄 / 등록 ────────────────────────────

    // 다음 대형 뉴스 발생 날짜·케이스·국가 쌍·전조 뉴스를 무작위로 예약
    void ScheduleNextBigNews()
    {
        if (BigTriggerCases.Count == 0) return;

        scheduledBigNewsCaseIndex = Random.Range(0, BigTriggerCases.Count);
        scheduledBigNewsDay = currentDay + Random.Range(BIG_NEWS_MIN_INTERVAL, BIG_NEWS_MAX_INTERVAL);
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
            : FORESHADOW_DEFAULT_LEAD;
        scheduledBigNewsVibeDay = scheduledBigNewsDay - (lead + 1);
    }

    // 케이스를 활성 뉴스로 등록 — 국가 쌍을 플레이스홀더에 치환하고 관련 국가 목록을 초기화
    public void AddActiveNews(Case casedata, string forcedNationPair = null, bool isRumorConfirmed = false)
    {
        Debug.Log($"뉴스 추가: {casedata.CaseName}");

        ActiveCase newActive = new ActiveCase
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
        recentIssues.Add(casedata);
    }

    // ── 실적 / 사업 ───────────────────────────────────

    // 매턴 모든 회사의 매출·영업이익을 재계산
    void RecalculateFinancials()
    {
        foreach (Stock stock in tabmanager.allstock)
        {
            float revenueModifier = 1.0f;
            if (IsBoostedByEra(stock)) revenueModifier = ERA_REVENUE_BOOST;
            else if (IsWeakenedByEra(stock)) revenueModifier = ERA_REVENUE_PENALTY;

            stock.revenue = Mathf.RoundToInt(stock.runtimeBaseRevenue * revenueModifier);
            stock.OperatingProfit = stock.revenue - stock.baseCost;
        }
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

    // 사업 시스템 — 매턴 호출
    void ProcessBusinesses()
    {
        // ── 진행 중인 사업 처리 ──
        for (int i = activeBusinesses.Count - 1; i >= 0; i--)
        {
            ActiveBusiness biz = activeBusinesses[i];
            biz.remainingTurns--;

            Stock stock = System.Array.Find(tabmanager.allstock, s => s.StockName == biz.stockName);

            // 진행 중: 매턴 투자비만큼 영업이익 감소 (비용 증가 효과)
            if (stock != null)
                stock.OperatingProfit -= biz.investmentCost;

            if (biz.remainingTurns > 0) continue;

            // 사업 완료 판정
            bool success = Random.Range(0, 100) < biz.successRate;
            if (success && stock != null)
            {
                stock.runtimeBaseRevenue += biz.revenueBoost; // 성공: 매출 기준값 영구 상승 (세이브 단위로 유지)
                AddNotice($"[사업 성공] {biz.successNews}");
            }
            else
            {
                AddNotice($"[사업 실패] {biz.failNews}"); // 실패: 투자금은 이미 날린 상태
            }

            activeBusinesses.RemoveAt(i);
        }

        // ── 새 사업 착수 (일정 주기마다) ──
        businessTimer++;
        if (businessTimer >= Random.Range(BUSINESS_MIN_INTERVAL, BUSINESS_MAX_INTERVAL))
        {
            businessTimer = 0;
            TryStartNewBusiness();
        }
    }

    // 사업 풀에서 하나 골라서 착수
    void TryStartNewBusiness()
    {
        if (allBusinessProjects.Length == 0) return;

        BusinessProject picked = allBusinessProjects[Random.Range(0, allBusinessProjects.Length)];
        if (picked.targetStock == null) return;

        // 이미 사업 진행 중인 회사는 제외
        if (activeBusinesses.Exists(b => b.stockName == picked.targetStock.StockName)) return;

        activeBusinesses.Add(new ActiveBusiness
        {
            projectName = picked.projectName,
            stockName = picked.targetStock.StockName,
            remainingTurns = picked.duration,
            investmentCost = picked.investmentCost,
            successRate = picked.successRate,
            revenueBoost = picked.revenueBoost,
            successNews = picked.successNews,
            failNews = picked.failNews,
        });
        AddNotice($"[신사업] {picked.startNews}");
    }

    // ── 학습 데이터 수집 ──────────────────────────────

    // 하루가 끝날 때 모든 주식의 가격·변동률·뉴스 영향 여부를 CSV에 누적 기록 (ML 학습 데이터용)
    void SaveStockDataToCSV()
    {
        string path = Path.Combine(Application.persistentDataPath, "stock_history.csv");

        if (!File.Exists(path))
        {
            File.WriteAllText(path,
                "day,stockName,currentPrice,previousPrice,priceChangePct,isAffectedByNews,eraEffect\n");
        }

        StringBuilder sb = new StringBuilder();
        foreach (Stock stock in tabmanager.allstock)
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

    // 데이터 생성용 임시 함수 — 나중에 삭제
    public void AutoSimulate(int days)
    {
        for (int i = 0; i < days; i++)
            OnNextDayButton();

        Debug.Log($"{days}일 시뮬레이션 완료! CSV 확인하세요.");
    }
}
