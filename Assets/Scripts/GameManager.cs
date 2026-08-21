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
    // 사업 템플릿은 이제 Stock.businesses 배열에 회사별로 등록한다 — 여기서 전체 목록을 따로 관리하지 않는다
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

    // 분기 마감마다 회사 하나하나가 새 사업을 벌일 확률 — 성향에 따라 다르게 준다
    // 재투자형은 번 돈을 다음 사업에 넣는 게 정체성이라 더 자주 벌이고, 배당형은 안정을 우선해 덜 벌인다
    // 40개 평균이 옛 0.25와 비슷하게 유지되도록 두 값의 중간이 0.25 부근이 되게 잡았다
    private const float BUSINESS_START_CHANCE_REINVEST = 0.35f;
    private const float BUSINESS_START_CHANCE_DIVIDEND = 0.18f;

    // 사업 착수 후에도 금고에 운영비 이만큼의 분기치는 남겨야 한다 — 안전 마진 없이 다 지르면 전부 부도난다
    private const int BUSINESS_SAFETY_MARGIN_QUARTERS = 2;

    private const int QUARTER_LENGTH = 10;            // 분기 길이 (턴) — 200턴이면 20분기

    // ── 회사 금고 / 재무 기반 주가 ──
    // 분기 마감일에 그 분기 수입이 금고로 들어간다. 배당과 사업 투자비는 여기서 빠져나간다
    private const float QUARTER_SHOCK = 0.30f;        // 실적 발표일에 목표 주가 쪽으로 한 번에 좁히는 비율 (30%)
    private const float QUARTER_REPORT_THRESHOLD = 3f; // 이 수치(%) 미만의 수입 변화는 공시하지 않는다 (알림 도배 방지)
    // 주가를 움직이는 엔진은 수입 하나다. 금고는 회사의 체력이지 가치 평가 요소가 아니므로 주가에 넣지 않는다
    // 금고를 주가에 반영하면 배당을 줄수록·사업을 시작할수록 주가가 떨어지는 역설이 생긴다
    // 실적이 좋아진 것을 목표 주가에 얼마나 세게 반영할지 정하는 손잡이
    // "매출의 몇 %만큼 수입이 늘었나"에 이 값을 곱해 주가 배수를 만든다. 클수록 실적에 예민해진다
    // 40개 매출값을 채운 지금(Step 6), 안정형(운영비 30%)과 자원형(60%) 사이에서 체감이 맞게 2.0으로 올린다
    private const float PROFIT_SENSITIVITY = 2.0f;
    private const float FAIR_PRICE_MIN_RATIO = 0.3f;  // 목표 주가 하한 (기준가의 30% — 적자가 심해도 0으로 떨어지지 않게)
    private const float FAIR_PRICE_MAX_RATIO = 3.0f;  // 목표 주가 상한 (기준가의 300% — 사업 연속 성공으로 무한히 오르지 않게)

    // 금고가 마이너스라고 바로 부도나지 않는다 — 버티는 기간을 준다
    private const int SOLVENCY_CRISIS_QUARTERS = 3;   // 이 분기 이상 연속 마이너스면 "부도 위기" 경고
    private const int SOLVENCY_DELIST_QUARTERS = 5;   // 이 분기 이상 연속 마이너스면 상장폐지

    private const float DAILY_BIAS = 0.15f;           // 매일 가격에 더해지는 상승 편향 (%)
    private const float RANGE_STABLE = 0.5f;          // 안정형 일일 변동폭 (±%)
    private const float RANGE_NORMAL = 1f;          // 보통형 일일 변동폭 (±%)
    private const float RANGE_VOLATILE = 1.5f;          // 고변동형 일일 변동폭 (±%)
    private const float RANGE_DEFAULT = 2f;           // 변동 유형이 없을 때의 기본값

    // 그래프는 이만큼 쌓여야 선이 그려진다 — 그래프 탭 잠금 기준일로도 쓰이므로 public
    public const int HISTORY_LENGTH = 10;             // 그래프·NPC 예측에 쓰는 최근 기록 보관 일수
    private const float FAKE_VOLUME_SCALE = 50f;      // 가짜 거래량 계수 (변동폭 절댓값 × 이 값)
    private const int MIN_STOCK_PRICE = 1;            // 주가 하한 (0 이하로 내려가지 않도록)

    private Era pendingEra;                                 // 힌트 뉴스로 예고된, 아직 시작 안 한 시대
    private List<string> dailyNotices = new List<string>(); // 오늘 발생한 알림 메시지 모음 (줄바꿈으로 이어서 표시)

    // 분기 결산 중에는 알림이 메인 알림창 대신 실적 발표 팝업으로 모인다
    // 사업 결과·배당·공시가 한꺼번에 쏟아져서 알림창이 감당하지 못하기 때문
    private bool isSettlingQuarter;
    private List<string> quarterReportLines = new List<string>();

    // 이번 분기에 각 회사의 실적이 왜 변했는지 한 줄 이유를 기억해둔다 (회사 이름 → 이유)
    // 실적 공시에 "▲12%"만 뜨면 원인을 알 수 없어서, 사업 성패 같은 원인을 여기 담아뒀다가 같이 보여준다
    private Dictionary<string, string> quarterChangeReasons = new Dictionary<string, string>();

    // 지난 분기에 각 회사에 걸려 있던 시대 배율 (회사 이름 → 1.2 / 1.0 / 0.8)
    // "지금 시대가 미는가"만 봐서는 시대가 끝나서 수입이 원래대로 떨어진 분기를 설명할 수 없다.
    // 시대 종료야말로 수입이 가장 크게 꺾이는 순간이라, 지난 배율과 비교해야 원인을 제대로 짚는다
    private Dictionary<string, float> lastQuarterEraModifier = new Dictionary<string, float>();

    // 지난 분기 각 회사의 평소 매출 (회사 이름 → runtimeRevenue)
    // 수입 변화를 "시대 때문"과 "사업 때문"으로 갈라내려면 지난 매출이 있어야 한다.
    // 매출은 사업 성공·실패로만 바뀌므로, 매출이 움직였다는 것은 곧 사업 결과가 나왔다는 뜻이다
    private Dictionary<string, int> lastQuarterRevenue = new Dictionary<string, int>();

    // 진행 중인 사업 상태를 추적하는 클래스
    [System.Serializable]
    public class ActiveBusiness
    {
        public string projectName;  // 사업 이름 (저장/식별용)
        public string stockName;       // 대상 회사 이름 (저장용)
        public int remainingQuarters;  // 남은 분기 수 — 분기 마감일마다 하나씩 줄어든다
        public int totalQuarters;      // 착수 시점의 전체 기간 — "3분기 중 1분기 남음" 진행률 표시에 쓴다
        public int startedQuarter;     // 몇 분기에 착수했는지 — 이번 분기에 막 시작한 사업과 예전부터 진행 중인 사업을 구분한다
        public int investmentCost;     // 착수할 때 금고에서 빠져나간 투자 비용 (기록용)
        public int successRate;     // 성공 확률
        public int revenueBoost;    // 성공 시 평소 매출 증가량
        public int revenueDrop;     // 실패 시 평소 매출 감소량
        public int assetBoost;      // 성공 시 자산 증가량
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
        public int income;             // 시대 보정까지 반영된 이번 분기 수입
        public int revenue;            // 사업 성패가 누적된 런타임 매출
        public int baseIncome;         // [구버전 세이브 호환] 매출·운영비로 나누기 전 형식의 값
        public int asset;              // 회사 금고
        public int negativeQuarters;   // 금고가 연속으로 마이너스인 분기 수
        public bool isDelisted;        // 상장폐지 여부
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
                income = stock.income,
                revenue = stock.runtimeRevenue, // 사업 성패가 누적된 런타임 매출을 저장
                asset = stock.runtimeAsset,
                negativeQuarters = stock.negativeQuarters,
                isDelisted = stock.isDelisted,
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
            stock.income = saved.income;
            // 옛 세이브에는 revenue 칸이 없다. 그때는 baseIncome을 매출로 본다 (운영비가 0이던 시절의 값)
            stock.runtimeRevenue = saved.revenue > 0 ? saved.revenue : saved.baseIncome;
            stock.runtimeAsset = saved.asset;
            stock.negativeQuarters = saved.negativeQuarters;
            stock.isDelisted = saved.isDelisted;

            // 상폐된 회사는 보유 주식이 있어도 복원하지 않는다 — 상폐 시점에 이미 전액 손실 처리됐다
            if (!stock.isDelisted)
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
            // 사업 보너스가 쌓인 런타임 매출을 에셋 원본으로 되돌린다 (도메인 리로드 여부와 무관하게 항상 실행)
            stock.runtimeRevenue = stock.BaselineRevenue;
            stock.runtimeAsset = stock.BaselineAsset;
            stock.income = stock.runtimeRevenue - stock.operatingCost;
            // 즐겨찾기는 세이브 대상이지만 에셋에도 남으므로 먼저 끈다
            // 세이브가 있으면 바로 아래 LoadGame이 저장된 값으로 다시 켜준다
            stock.isFavorite = false;

            // 상폐 상태도 NonSerialized라 에디터에서 값이 남지 않지만, 도메인 리로드 없이 이어서
            // 새 게임을 시작하는 경우를 대비해 명시적으로 초기화한다. 세이브가 있으면 LoadGame이 다시 채운다
            stock.negativeQuarters = 0;
            stock.isDelisted = false;

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

    // 지금이 분기 마감일인지 — 10턴에 한 번 실적이 정산된다 (200턴이면 20분기)
    public bool IsQuarterEnd => currentDay % QUARTER_LENGTH == 0;

    // 현재 분기 번호 (1분기부터 시작)
    public int CurrentQuarter => (currentDay - 1) / QUARTER_LENGTH + 1;

    // 다음 날 버튼 클릭 시 호출 — 하루 동안 일어나는 모든 처리를 순서대로 실행
    // 각 단계의 순서에 의미가 있으므로(예: 시대 효과 → 분기 결산 → 가격 변동) 호출 순서를 바꾸지 말 것
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

        // 실적은 매일 정산하지 않는다 — 분기 마감일에만 몰아서 처리한다
        if (IsQuarterEnd) SettleQuarter();

        ApplyDailyDrift();
        ApplyActiveNewsEffects();

        activeNewsList.RemoveAll(active => active.Rday <= 0); // 지속일이 끝난 뉴스 제거

        RunNpcInvestors();
        UIManager.instance.UpdateDayText();

        RecordStockHistories();
        CheckGameOverConditions();

        SaveStockDataToCSV();
        UIManager.instance.UpdateTotalAssetText();
        UIManager.instance.favoritePanel.RefreshHome();
    }

    // 분기 마감 — 이 게임의 핵심 이벤트
    // 순서에 의미가 있다: 수입이 확정돼야 금고에 넣을 수 있고, 사업 결과가 수입을 바꾼 뒤라야
    // 목표 주가가 제대로 나오며, 배당은 금고에 돈이 들어온 다음에 빼야 한다
    // 새 사업 착수는 배당·부도 판정보다 뒤에 둔다 — 배당으로 나갈 돈까지 투자비로 써버리면
    // "금고 바닥 → 사업 줄임"이 성립하지 않는다. 최종 금고를 보고 착수 여부를 정해야 한다
    void SettleQuarter()
    {
        // 이 구간에서 나오는 알림은 전부 실적 발표 팝업으로 모인다
        isSettlingQuarter = true;
        quarterReportLines.Clear();
        quarterChangeReasons.Clear();

        RecalculateFinancials();   // ① 이번 분기 수입 확정 (시대 보정 반영)
        ResolveBusinesses();       // ② 진행 중이던 사업의 성공·실패 발표 → 수입이 바뀜
        RecalculateFinancials();   // ③ 사업 결과를 수입에 즉시 반영
        AccumulateAssets();        // ④ 확정된 수입을 금고에 넣는다
        PayDividends();            // ⑤ 배당형 회사가 금고에서 배당을 지급
        CheckSolvency();           // ⑥ 배당까지 다 나간 최종 금고로 부도 위기·상장폐지 판정
        StartNewBusinesses();      // ⑦ 최종 금고를 보고 새 사업 착수 여부를 정한다
        ReportOngoingBusinesses(); // ⑧ 아직 결과가 안 나온 사업들을 다시 한 번 알려준다
        AnnounceQuarterResults();  // ⑨ 실적 공시 — 지난 분기 대비 수입 증감
        ApplyFinancialGravity();   // ⑩ 바뀐 실적을 주가에 반영 (발표 쇼크)

        isSettlingQuarter = false;

        // 팝업으로 띄우고, 메인 알림창에는 한 줄만 남겨 발표가 있었다는 사실만 알린다
        if (UIManager.instance.quarterReportPanel != null)
            UIManager.instance.quarterReportPanel.Show(CurrentQuarter, quarterReportLines);

        if (quarterReportLines.Count > 0)
            AddNotice($"[{CurrentQuarter}분기 실적 발표] 소식 {quarterReportLines.Count}건");
    }

    // 알림 메시지를 오늘 목록에 추가하고, 지금까지 쌓인 메시지를 줄바꿈으로 이어서 NoticeText에 표시
    private void AddNotice(string message)
    {
        // 분기 결산 중이면 메인 알림창을 건드리지 않고 실적 발표 팝업 쪽으로 모은다
        if (isSettlingQuarter)
        {
            quarterReportLines.Add(message);
            return;
        }

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

        foreach (Stock stock in LiveStocks)
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
        foreach (Stock stock in LiveStocks)
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

    // 배당주 배당금 지급 — 영업이익이 0 이하면 삭감
    void PayDividends()
    {
        foreach (Stock stock in LiveStocks)
        {
            if (!stock.hasDividend) continue;

            // 실적 악화 시 배당 삭감
            if (stock.income <= 0)
            {
                AddNotice($"[배당 삭감] {stock.StockName} 실적 악화로 이번 분기 배당이 없습니다.");
                continue;
            }

            // 배당 재원은 회사 금고다. 이번 분기 수입 중 배당성향만큼을 주주 전체에게 나눠준다
            int totalDividend = Mathf.RoundToInt(stock.income * stock.dividendRate);
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

            int owned = Portfolio.GetAmount(stock);
            if (owned <= 0) continue;

            int received = Mathf.RoundToInt(perShare * owned);
            if (received <= 0) continue;

            UpdateGold(received);
            AddNotice($"[배당] {stock.StockName} {owned}주 → +{received} G (주당 {perShare:F1} G)");
        }
    }

    // 그래프·NPC 예측에 쓰이는 최근 기록 갱신 (최대 HISTORY_LENGTH일치 유지)
    void RecordStockHistories()
    {
        foreach (Stock stock in LiveStocks)
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
        foreach (Stock stock in LiveStocks)
        {
            // 시대는 매출만 밀거나 누른다. 운영비는 시대와 무관하게 그대로 나간다
            // 그래서 운영비가 무거운 회사일수록 같은 시대 배율에도 수입이 크게 흔들린다
            int revenue = Mathf.RoundToInt(stock.runtimeRevenue * GetEraRevenueModifier(stock));
            stock.income = revenue - stock.operatingCost;
        }
    }

    // 지금 이 회사의 매출에 걸려 있는 시대 배율 — 수혜 1.2 / 무관 1.0 / 타격 0.8
    public float GetEraRevenueModifier(Stock stock)
    {
        if (IsBoostedByEra(stock)) return ERA_REVENUE_BOOST;
        if (IsWeakenedByEra(stock)) return ERA_REVENUE_PENALTY;
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
            TryStartBusiness(stock);
    }

    // 회사 하나가 새 사업을 시작할지 판정한다
    void TryStartBusiness(Stock stock)
    {
        if (stock.businesses == null || stock.businesses.Length == 0) return;

        // 이미 사업 진행 중인 회사는 제외
        if (activeBusinesses.Exists(b => b.stockName == stock.StockName)) return;

        // 금고가 마이너스면 새 도박을 벌이지 않는다 — 빚진 회사가 사업을 벌이면 부도 카운트가 무의미해진다
        if (stock.runtimeAsset < 0) return;

        // 성향별로 착수 확률이 다르다 — 재투자형은 자주, 배당형은 신중하게
        float chance = stock.payoutStyle == Stock.PayoutStyle.Reinvest
            ? BUSINESS_START_CHANCE_REINVEST
            : BUSINESS_START_CHANCE_DIVIDEND;
        if (Random.value >= chance) return;

        // 착수 후에도 운영비 몇 분기치는 금고에 남는 사업만 후보로 삼는다 — 안전 마진 없이 다 지르면 전부 부도난다
        int safetyMargin = stock.operatingCost * BUSINESS_SAFETY_MARGIN_QUARTERS;
        List<BusinessProject> affordable = new List<BusinessProject>();
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
    void StartBusiness(Stock stock, BusinessProject picked)
    {
        stock.runtimeAsset -= picked.investmentCost;

        int quarters = Mathf.Max(1, picked.durationQuarters);
        activeBusinesses.Add(new ActiveBusiness
        {
            projectName = picked.projectName,
            stockName = stock.StockName,
            remainingQuarters = quarters,
            totalQuarters = quarters,
            startedQuarter = CurrentQuarter,
            investmentCost = picked.investmentCost,
            successRate = picked.successRate,
            revenueBoost = picked.revenueBoost,
            revenueDrop = picked.revenueDrop,
            assetBoost = picked.assetBoost,
            successNews = picked.successNews,
            failNews = picked.failNews,
        });

        // 착수 문구 한 줄만 띄우면 어느 회사가 무슨 사업을 언제까지 하는지 알 수 없다.
        // 여기서 회사·사업명·기간·성공확률·성패 시 효과를 한꺼번에 보여줘야
        // 몇 분기 뒤 실적이 튀었을 때 "아, 그때 그 사업 때문이구나"로 이어진다
        AddNotice($"[신사업] {stock.StockName} · {picked.projectName}\n" +
                  $"    {picked.startNews}\n" +
                  $"    투자 {UIFormat.Gold(picked.investmentCost)} · {quarters}분기 뒤({CurrentQuarter + quarters}분기) 결과 발표 · 성공 확률 {picked.successRate}%\n" +
                  $"    성공하면 매출 +{UIFormat.Gold(picked.revenueBoost)} / 실패하면 매출 -{UIFormat.Gold(picked.revenueDrop)}");
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

    // ── 금고 · 부도 · 상장폐지 ─────────────────────────

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

            if (stock.negativeQuarters >= SOLVENCY_DELIST_QUARTERS)
            {
                DelistStock(stock);
            }
            else if (stock.negativeQuarters >= SOLVENCY_CRISIS_QUARTERS)
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
            stock.runtimeAsset += stock.income; // 그 분기에 번 돈이 통째로 금고에 들어간다
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
            int step = Mathf.RoundToInt(gap * QUARTER_SHOCK);
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

            float changePercent = (stock.income - previous) * 100f / previous;
            if (Mathf.Abs(changePercent) < QUARTER_REPORT_THRESHOLD) continue; // 변화가 미미하면 공시하지 않는다

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
        float businessDelta = (stock.runtimeRevenue - previousRevenue) * currentModifier;

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

        char last = word[word.Length - 1];
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
        float improvement = (float)(stock.income - stock.BaselineIncome)
                            / Mathf.Max(stock.BaselineRevenue, 1);

        float multiplier = 1f + improvement * PROFIT_SENSITIVITY;

        // 폭락·폭등이 끝없이 이어지지 않도록 기준가의 30%~300% 사이로 제한
        multiplier = Mathf.Clamp(multiplier, FAIR_PRICE_MIN_RATIO, FAIR_PRICE_MAX_RATIO);
        return Mathf.RoundToInt(stock.StockBasePrice * multiplier);
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
