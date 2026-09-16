using System.Collections.Generic;
using UnityEngine;

// 게임 전체 흐름을 관리하는 싱글톤 클래스 — 골드·날짜·사건 처리·저장/불러오기를 담당
// 책임별로 여러 파일에 나눠진 partial class다. 이 파일(Core)에는 싱글톤 초기화, 필드·상수,
// 골드/대출, 하루 진행 오케스트레이션(OnNextDayButton·SettleQuarter)만 남긴다
//   GameManager.SaveLoad.cs    — JSON 저장/불러오기
//   GameManager.News.cs        — 소형·대형 뉴스 생성/예약/효과
//   GameManager.Era.cs         — 시대 시작/종료, 산업 보정
//   GameManager.Market.cs      — 매일 가격 변동, NPC 매매, 히스토리 기록
//   GameManager.Business.cs    — 사업 착수/진행/성패, 뜬금 이벤트
//   GameManager.Finance.cs     — 배당, 금고, 부도/상장폐지, 실적 기반 주가
//   GameManager.DataExport.cs  — NPC 학습용 CSV 기록
public partial class GameManager : MonoBehaviour
{
    public static GameManager Instance; // 어디서든 GameManager.Instance로 접근하기 위한 싱글톤 인스턴스

    [Header("기본 상태")]
    public long currentgold;  // 현재 보유 골드
    public int currentDay;   // 현재 게임 날짜
    public long loanAmount;   // 현재 대출금
    public int lowGoldStreak; // 골드가 기준치 이하인 연속 턴 수 (기준 도달 시 게임 오버)

    [Header("참조")]
    public TabManager tabmanager; // 모든 주식 목록(allstock)에 접근하기 위한 참조

    [Header("사건 데이터")]
    public Case[] AllCases;                                // 게임에 등록된 모든 사건(Case ScriptableObject) 배열
    public List<ActiveCase> activeNewsList = new();        // 현재 진행 중인 활성 뉴스 목록
    public List<Case> SmallNewsCases = new();               // SmallIndustryNews·SmallCompanyNews 타입 케이스 목록
    public List<Case> BigTriggerCases = new();               // BigTriggerNews 타입 케이스 목록

    [Header("대형 뉴스 스케줄")]
    public int lastBigNewsDay = -1;          // 마지막 대형 뉴스가 실제로 발생한 날짜
    public int scheduledBigNewsDay = -1;     // 예약된 대형 뉴스 발생 날짜
    public int scheduledBigNewsVibeDay = -1; // 전조 뉴스 발생 날짜 (대형 뉴스 며칠 전)
    public int scheduledBigNewsCaseIndex = -1; // 예약된 대형 뉴스의 케이스 인덱스
    public int scheduledForeshadowIndex = -1;  // 예약된 전조 뉴스의 인덱스
    public string scheduledNationPair = "";    // 대형 뉴스에 사용할 국가 쌍 (전조 뉴스와 공유)

    [Header("소형 뉴스")]
    public NewsTemplate[] newsTemplates;               // 소형 뉴스 생성용 템플릿 목록
    public int smallNewsTimer;                          // 소형 뉴스 생성 주기 타이머
    public List<PendingNews> pendingNewsList = new();   // 불확정 뉴스 대기열

    [Header("시대 시스템")]
    public Era[] allEras;              // 전체 시대 목록 (Inspector에 등록)
    public Era currentEra;             // 현재 진행 중인 시대 (없으면 null)
    public int eraEndDay = -1;         // 현재 시대 종료 날짜
    public int nextEraStartDay = 20;   // 다음 시대 시작 가능 날짜 (최초 20턴)
    public int eraHintShownDay = -1;   // 힌트 뉴스를 보여준 날짜 (중복 출력 방지)

    [Header("사업 시스템")]
    // 사업 템플릿은 이제 Stock.businesses 배열에 회사별로 등록한다 — 여기서 전체 목록을 따로 관리하지 않는다
    public List<ActiveBusiness> activeBusinesses = new(); // 현재 진행 중인 사업

    // 밸런스 숫자(시작 골드, 확률, 배율 등)는 전부 GameBalance.cs로 옮겼다 — 값 하나 조절하려고
    // 이 파일 저 파일 뒤질 필요 없이 그 파일 하나만 열면 되도록 하기 위함이다

    private Era pendingEra;                             // 힌트 뉴스로 예고된, 아직 시작 안 한 시대
    private readonly List<string> dailyNotices = new(); // 오늘 발생한 알림 메시지 모음 (줄바꿈으로 이어서 표시)

    // 분기 결산 중에는 알림이 메인 알림창 대신 실적 발표 팝업으로 모인다
    // 사업 결과·배당·공시가 한꺼번에 쏟아져서 알림창이 감당하지 못하기 때문
    private bool isSettlingQuarter;
    private readonly List<string> quarterReportLines = new();

    // 이번 분기에 각 회사의 실적이 왜 변했는지 한 줄 이유를 기억해둔다 (회사 이름 → 이유)
    // 실적 공시에 "▲12%"만 뜨면 원인을 알 수 없어서, 사업 성패 같은 원인을 여기 담아뒀다가 같이 보여준다
    private readonly Dictionary<string, string> quarterChangeReasons = new();

    // 지난 분기에 각 회사에 걸려 있던 시대 배율 (회사 이름 → 1.2 / 1.0 / 0.8)
    // "지금 시대가 미는가"만 봐서는 시대가 끝나서 수입이 원래대로 떨어진 분기를 설명할 수 없다.
    // 시대 종료야말로 수입이 가장 크게 꺾이는 순간이라, 지난 배율과 비교해야 원인을 제대로 짚는다
    private readonly Dictionary<string, float> lastQuarterEraModifier = new();

    // 지난 분기 각 회사의 평소 매출 (회사 이름 → runtimeRevenue)
    // 수입 변화를 "시대 때문"과 "사업 때문"으로 갈라내려면 지난 매출이 있어야 한다.
    // 매출은 사업 성공·실패로만 바뀌므로, 매출이 움직였다는 것은 곧 사업 결과가 나왔다는 뜻이다
    private readonly Dictionary<string, int> lastQuarterRevenue = new();

    // 진행 중인 사업 상태를 추적하는 클래스
    [System.Serializable]
    public class ActiveBusiness
    {
        public string stockId;
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
        public string stockId;
        public int previousPrice;
        public int lastQuarterIncome;
        public float lastEraModifier = 1f;
        public int lastRevenue;
        public List<int> priceHistory = new();
        public List<float> changeHistory = new();
        public List<float> volumeHistory = new();
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
        public int version;
        public string currentEraId;
        public string pendingEraId;
        public string scheduledCaseId;
        public string scheduledHintId;
        public List<NewsSaveData> news = new();
        public List<NPCInvestor.SaveData> npcs = new();
        public List<string> notices = new();
        public List<string> quarterReport = new();
        public bool quarterReportOpen;
        public long currentgold;
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
        public long loanAmount;
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
        ClassifyCases();
        LoadGame();

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

    // ── 골드 / 대출 ───────────────────────────────────

    // 주식 구매/판매 시 골드 증감 처리 (구매 시 음수, 판매 시 양수 전달)
    public void UpdateGold(long amount)
    {
        currentgold = MoneyMath.Add(currentgold, amount);
    }

    // 현재 대출 한도 — 보유 골드의 50%
    public long MaxLoan => System.Math.Max(0L, currentgold / GameBalance.LOAN_MAX_RATIO_DIVISOR);

    // 이번 턴에 부과될 대출 이자
    public long TurnInterest => MoneyMath.Ceiling(loanAmount * (double)GameBalance.LOAN_INTEREST_RATE);

    // 대출 실행 — 자산의 50%까지만 가능
    public bool TakeLoan(long amount)
    {
        if (amount <= 0 || amount > MaxLoan || amount > long.MaxValue - loanAmount || currentgold > long.MaxValue - amount)
        {
            GameDiagnostics.Log("대출 한도 초과");
            return false;
        }
        loanAmount += amount;
        currentgold += amount;
        UIManager.instance.UpdateGoldText();
        SaveGame();
        return true;
    }

    // 상환 — 보유 골드보다 많이 상환 불가
    public bool RepayLoan(long amount)
    {
        if (amount <= 0 || amount > currentgold)
        {
            GameDiagnostics.Log("골드 부족");
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

    // 지금이 분기 마감일인지 — QUARTER_LENGTH턴에 한 번 실적이 정산된다 (20턴 기준, 400턴이면 20분기)
    public bool IsQuarterEnd => currentDay % GameBalance.QUARTER_LENGTH == 0;

    // 현재 분기 번호 (1분기부터 시작)
    public int CurrentQuarter => (currentDay - 1) / GameBalance.QUARTER_LENGTH + 1;

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

        // ApplyDailyDrift()가 StockPreviousPrice를 스냅샷 뜬다 — 시대/사업 효과는 반드시 그 "뒤"에 적용해야
        // 오늘 등락률(priceChangePct)에 잡힌다. 스냅샷보다 먼저 적용하면 가격은 실제로 바뀌어도
        // "어제 가격" 쪽에 섞여 들어가서 변동률에는 영원히 안 보이게 된다
        ApplyDailyDrift();

        ApplyEraPriceEffects();

        // 실적은 매일 정산하지 않는다 — 분기 마감일에만 몰아서 처리한다
        if (IsQuarterEnd) SettleQuarter();

        ApplyActiveNewsEffects();

        activeNewsList.RemoveAll(active => active.Rday <= 0); // 지속일이 끝난 뉴스 제거

        RunNpcInvestors();
        UIManager.instance.UpdateDayText();

        RecordStockHistories();
        CheckGameOverConditions();

        SaveStockDataToCSV();
        UIManager.instance.UpdateTotalAssetText();
        UIManager.instance.favoritePanel.RefreshHome();
        AutoSave();
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
    public void AddNotice(string message)
    {
        // 분기 결산 중이면 메인 알림창을 건드리지 않고 실적 발표 팝업 쪽으로 모은다
        if (isSettlingQuarter)
        {
            quarterReportLines.Add(message);
            return;
        }

        dailyNotices.Add(message);
        UIManager.instance.NoticeText.text = string.Join("\n", dailyNotices);
        GameDiagnostics.Log($"[알림 {dailyNotices.Count}개] {string.Join(" | ", dailyNotices)}");
    }

    // 게임 오버 조건 검사 — 지금은 비활성화 상태다 (요청에 따라 임시로 꺼둠)
    // 되살리려면 아래 주석 처리한 원래 로직을 복구하면 된다:
    //   · 골드가 LOW_GOLD_THRESHOLD 이하로 LOW_GOLD_GAMEOVER_STREAK턴 연속 → 게임 오버
    //   · loanAmount > 0 이고 currentgold < loanAmount (대출 상환 불가) → 게임 오버
    void CheckGameOverConditions()
    {
        // 게임 오버 없음 — 골드가 0 이하가 되든 대출을 못 갚든 게임은 계속 진행된다
        lowGoldStreak = 0;

        // --- 원래 게임 오버 로직 (임시 비활성화) ---
        // if (currentgold <= GameBalance.LOW_GOLD_THRESHOLD)
        // {
        //     lowGoldStreak++;
        //     if (lowGoldStreak >= GameBalance.LOW_GOLD_GAMEOVER_STREAK)
        //     {
        //         LoadGameOverScene();
        //         return;
        //     }
        // }
        // else
        // {
        //     lowGoldStreak = 0;
        // }
        //
        // if (loanAmount > 0 && currentgold < loanAmount)
        //     LoadGameOverScene();
    }

    // 게임 오버 씬 전환 — CheckGameOverConditions를 되살릴 때 함께 사용한다
    void LoadGameOverScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameOver");
    }
}
