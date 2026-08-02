using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;
using Unity.VisualScripting;
using System.Text;

// 게임 전체 흐름을 관리하는 싱글톤 클래스 — 골드·날짜·사건 처리·저장/불러오기를 담당
public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // 어디서든 GameManager에 접근하기 위한 싱글톤 인스턴스
    public int currentgold;  // 현재 보유 골드
    public int currentDay;   // 현재 게임 날짜
    public Case[] AllCases; // 게임에 등록된 모든 사건(Case ScriptableObject) 배열
    public TabManager tabmanager;                                    // 모든 주식 목록(allstock)에 접근하기 위한 참조
    public List<Case> recentIssues;                                  // 최근 발생한 사건 이력
    public List<ActiveCase> activeNewsList = new List<ActiveCase>(); // 현재 진행 중인 활성 뉴스 목록
    public List<Case> SmallNewsCases = new List<Case>();             // SmallIndustryNews·SmallCompanyNews 타입 케이스 목록
    public List<Case> BigTriggerCases = new List<Case>();            // BigTriggerNews 타입 케이스 목록
    public int lowGoldStreak;                                        // 골드 1000 이하 연속 턴 수 (5턴 도달 시 게임 오버)
    public int lastBigNewsDay = -1;                                  // 마지막 대형 뉴스가 실제로 발생한 날짜
    public int scheduledBigNewsDay = -1;                             // 예약된 대형 뉴스 발생 날짜
    public int scheduledBigNewsVibeDay = -1;                         // 전조 뉴스 발생 날짜 (대형 뉴스 며칠 전)
    public int scheduledBigNewsCaseIndex = -1;                       // 예약된 대형 뉴스의 케이스 인덱스
    public int scheduledForeshadowIndex = -1;                        // 예약된 전조 뉴스의 인덱스
    public string scheduledNationPair = "";                          // 대형 뉴스에 사용할 국가 쌍 (전조 뉴스와 공유)
    public int smallNewsTimer = 0;
    public int loanAmount = 0; // 현재 대출금
    [Header("시대 시스템")]
    public Era[] allEras;              // 전체 시대 목록 (Inspector에 등록)
    public Era currentEra;             // 현재 진행 중인 시대 (없으면 null)
    public int eraEndDay = -1;         // 현재 시대 종료 날짜
    public int nextEraStartDay = 20;   // 다음 시대 시작 가능 날짜 (최초 20턴)
    public int eraHintShownDay = -1;   // 힌트 뉴스를 보여준 날짜 (중복 출력 방지)

    private const int ERA_FIRST_START_DAY = 20; // 시대 시스템 최초 발생 가능 날짜
    private const int ERA_DURATION = 15;        // 시대 지속 턴 수
    private const int ERA_COOLDOWN = 5;         // 시대 종료 후 휴지기 턴 수
    private const int ERA_HINT_DAYS_BEFORE = 2; // 힌트 뉴스를 며칠 전에 보여줄지
    private Era pendingEra; // 힌트 뉴스로 예고된, 아직 시작 안 한 시대
    public NewsTemplate[] newsTemplates;
    public List<PendingNews> pendingNewsList = new List<PendingNews>(); // 불확정 뉴스 대기열
    private const int UNCERTAIN_NEWS_DELAY = 2;       // 불확정 뉴스 효과가 나타나기까지 걸리는 턴 수
    private List<string> dailyNotices = new List<string>(); // 오늘 발생한 알림 메시지 모음 (줄바꿈으로 이어서 표시)
        // 진행 중인 사업 상태를 추적하는 클래스
    [System.Serializable]
    public class ActiveBusiness
    {
    public string projectName;       // 사업 이름 (저장/식별용)
    public string stockName;         // 대상 회사 이름 (저장용)
    public int remainingTurns;       // 남은 턴 수
    public int investmentCost;       // 매턴 차감할 투자 비용
    public int successRate;          // 성공 확률
    public int revenueBoost;         // 성공 시 baseRevenue 증가량
    public string successNews;       // 성공 뉴스 문구
    public string failNews;          // 실패 뉴스 문구
    }
    [Header("사업 시스템")]
    public BusinessProject[] allBusinessProjects;  // 전체 사업 템플릿 목록 (인스펙터 등록)
    public List<ActiveBusiness> activeBusinesses = new List<ActiveBusiness>(); // 현재 진행 중인 사업
    private int businessTimer = 0;                 // 사업 착수 주기 타이머

    // Start보다 먼저 실행 — 싱글톤 인스턴스 등록
    void Awake()
    {
        Instance = this; // 외부에서 GameManager.Instance로 접근 가능하도록 자신을 등록
    }

    // 저장 데이터 로드 → UI 갱신 → AllCases를 타입별로 분류 → 대형 뉴스 스케줄 설정
    void Start()
    {
        LoadGame(); // 저장된 게임 데이터 불러오기
        UIManager.instance.UpdateDayText();
        UIManager.instance.UpdateGoldText();
        Debug.Log(Application.persistentDataPath);
        UIManager.instance.UpdateEraText(); // ← 추가
        foreach (Case c in AllCases)
        {
           switch(c.newsType)
            {
                case Case.NewsType.SmallIndustryNews:
                case Case.NewsType.SmallCompanyNews:// 하나의 리스트에 같이 넣기
                SmallNewsCases.Add(c);
                break;
                case Case.NewsType.BigTriggerNews:
                BigTriggerCases.Add(c);
                break;
            }
        }
        if (scheduledBigNewsDay <= currentDay) // 저장 데이터에 유효한 스케줄이 없으면 새로 예약
            ScheduleNextBigNews();
    }

    void Update()
    {

    }

    // JSON 저장을 위한 주식 데이터 구조체
    [System.Serializable]
    public class StockSaveData
    {
        public string stockname;       // 주식 이름 (식별 키)
        public int currentPrice;       // 저장 시점의 주식 가격
        public int ownamount;          // 보유 수량
        public float avgPurchasePrice; // 평균 매수 단가
        public bool isFavorite;   // ← 추가
        public int revenue;            // 추가
        public int operatingProfit;    // 추가\
        public int baseRevenue;   // 추가
        public int baseCost;       // 추가
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
        public string currentEraName;  // ScriptableObject는 직렬화 안 되니 이름으로 저장
        public string pendingEraName;
        public List<ActiveBusiness> activeBusinesses;  // 추가
        public int businessTimer;         
    }

    // 현재 게임 상태를 JSON 파일로 저장
    // 선택된 슬롯 번호 기반으로 저장 경로 반환
    // 선택된 슬롯 번호 기반으로 저장 경로 반환
    string GetSavePath()
    {
        int slot = PlayerPrefs.GetInt("SelectedSlot", 1);
        return Application.persistentDataPath + $"/save{slot}.json";
    }
    public void SaveGame()
    {
        GameSaveData data = new GameSaveData();
        data.currentgold = currentgold;
        data.currentday = currentDay;
        data.OwnStocks = new List<StockSaveData>();
        data.smallNewsTimer = smallNewsTimer;
        data.loanAmount = loanAmount;
        data.eraEndDay = eraEndDay;
        data.nextEraStartDay = nextEraStartDay;
        data.eraHintShownDay = eraHintShownDay;
        data.currentEraName = currentEra != null ? currentEra.eraName : "";
        data.pendingEraName = pendingEra != null ? pendingEra.eraName : "";
        data.activeBusinesses = activeBusinesses;
        data.businessTimer = businessTimer;

        // 씬의 모든 StockControl을 검색하여 보유 수량이 있는 주식만 저장 목록에 추가
        StockControl[] allStocks = FindObjectsByType<StockControl>(FindObjectsSortMode.None);
        // 기존: 보유 수량 > 0인 주식만 저장
        // 변경: 모든 주식 저장
        foreach (Stock stock in tabmanager.allstock)
        {
            StockSaveData stockdata = new StockSaveData();
            stockdata.stockname = stock.StockName;
            stockdata.currentPrice = stock.StockCurrentPrice;
            stockdata.ownamount = 0;
            stockdata.avgPurchasePrice = 0;
            stockdata.isFavorite = stock.isFavorite; // ← 추가
            stockdata.revenue = stock.revenue;                 // 추가
            stockdata.operatingProfit = stock.OperatingProfit;  // 추가
            stockdata.baseRevenue = stock.baseRevenue;
            stockdata.baseCost = stock.baseCost;

            // 보유 중인 주식이면 수량/평균가 추가
            foreach (StockControl sc in allStocks)
            {
                if (sc.stockinfo.StockName == stock.StockName)
                {
                    stockdata.ownamount = sc.ownedAmount;
                    stockdata.avgPurchasePrice = sc.avgPurchasePrice;
                    
                    break;
                }
            }
            data.OwnStocks.Add(stockdata);
        }

        data.lastBigNewsDay = lastBigNewsDay;
        data.scheduledBigNewsDay = scheduledBigNewsDay;
        data.scheduledBigNewsVibeDay = scheduledBigNewsVibeDay;
        data.scheduledBigNewsCaseIndex = scheduledBigNewsCaseIndex;
        data.scheduledForeshadowIndex = scheduledForeshadowIndex;
        data.scheduledNationPair = scheduledNationPair;

        string json = JsonUtility.ToJson(data);
        string path = GetSavePath();
        File.WriteAllText(path, json);
    }

    // JSON 파일에서 게임 데이터를 불러와 복원
    void LoadGame()
    {
        string path = GetSavePath();
        if (File.Exists(path)) // 저장 파일이 있을 경우 불러오기
        {
            string json = File.ReadAllText(path); // JSON 파일을 문자열로 읽기
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
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

            // 저장된 주식 보유 정보를 씬의 StockControl에 복원
            StockControl[] allStocks = FindObjectsByType<StockControl>(FindObjectsSortMode.None);
            foreach (StockSaveData savedstock in data.OwnStocks)
            {
                foreach (Stock stock in tabmanager.allstock)
                {
                    if (savedstock.stockname == stock.StockName)
                    {
                        stock.StockCurrentPrice = savedstock.currentPrice;
                        stock.StockPreviousPrice = savedstock.currentPrice;
                        stock.isFavorite = savedstock.isFavorite; // ← 추가
                        stock.revenue = savedstock.revenue;                 // 추가
                        stock.OperatingProfit = savedstock.operatingProfit;  // 추가
                        stock.baseRevenue = savedstock.baseRevenue;
                        stock.baseCost = savedstock.baseCost;
                    }
                }
                foreach (StockControl sc in allStocks)
                {
                    if (savedstock.stockname == sc.stockinfo.StockName)
                    {
                        sc.ownedAmount = savedstock.ownamount;
                        sc.avgPurchasePrice = savedstock.avgPurchasePrice;
                    }
                }
            }
        }
        else // 저장 파일이 없으면 기본값으로 시작
        {
            foreach (Stock stock in tabmanager.allstock)
            {
                stock.StockCurrentPrice = stock.StockBasePrice;
                stock.StockPreviousPrice = stock.StockBasePrice;
                stock.revenue = stock.baseRevenue;                      // 추가
                stock.OperatingProfit = stock.baseRevenue - stock.baseCost; // 추가
            }
            currentgold = 5000; // 시작 골드
            currentDay = 1;
        }
    }

    // 주식 구매/판매 시 골드 증감 처리 (구매 시 음수, 판매 시 양수 전달)
    public void UpdateGold(int amount)
    {
        currentgold += amount;
    }
    // 하루가 끝날 때 모든 주식의 가격·변동률·뉴스 영향 여부를 CSV에 누적 기록 (ML 학습 데이터용)
    void SaveStockDataToCSV()
    {
        string path = Application.persistentDataPath + "/stock_history.csv";

        if (!File.Exists(path))
        {
        // isAffectedByNews 컬럼 추가
            string header = "day,stockName,currentPrice,previousPrice,priceChangePct,isAffectedByNews,eraEffect\n";
            File.WriteAllText(path, header);
        }

        StringBuilder sb = new StringBuilder();
        foreach (Stock stock in tabmanager.allstock)
        {
            // 이 주식이 오늘 뉴스에 직접 영향받았는지 확인
           bool isAffected = activeNewsList.Exists(active =>
            {
                // ① 소형 뉴스 직접 영향 (지속 기간 포함 ✅)
                if (active.caseData.affectedCase != null &&
                    System.Array.Exists(active.caseData.affectedCase, s => s == stock))
                    return true;

                // ② 연쇄 효과
                if (active.caseData.chainEffects != null &&
                    System.Array.Exists(active.caseData.chainEffects, e => e.stock == stock))
                    return true;

                // ③ 대형 뉴스 산업군 영향
                if (active.caseData.newsType == Case.NewsType.BigTriggerNews &&
                    active.caseData.industryRates != null &&
                    System.Array.Exists(active.caseData.industryRates, ir => ir.industryTag == stock.IndustryTag))
                    return true;

                return false;
            });

        // 가격 변동률 계산
            float changePct = stock.ChangePercent;
            int eraEffect = 0;
            if (currentEra != null)
            {
                if (System.Array.Exists(currentEra.boostedIndustries, t => t == stock.IndustryTag))
                    eraEffect = 1;
                else if (System.Array.Exists(currentEra.weakenedIndustries, t => t == stock.IndustryTag))
                    eraEffect = -1;
            }
            sb.AppendLine(
                $"{currentDay}," +
                $"{stock.StockName}," +
                $"{stock.StockCurrentPrice}," +
                $"{stock.StockPreviousPrice}," +
                $"{changePct:F2}," +
                $"{(isAffected ? 1 : 0)}," +
                $"{eraEffect}"
            );
        }

        File.AppendAllText(path, sb.ToString());
    }
    // 대출 실행 — 자산의 50%까지만 가능
    public bool TakeLoan(int amount)
    {
        int maxLoan = currentgold / 2;
        if (amount > maxLoan)
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

    // 다음 날 버튼 클릭 시 호출 — 날짜 증가, 랜덤 사건 발생, UI 갱신. 감쇠는 매일 발생하므로 이곳에 계산식을 둠.
    // 알림 메시지를 오늘 목록에 추가하고, 지금까지 쌓인 메시지를 줄바꿈으로 이어서 NoticeText에 표시
        private void AddNotice(string message)
        {
            dailyNotices.Add(message);
            UIManager.instance.NoticeText.text = string.Join("\n", dailyNotices);
            Debug.Log($"[알림 {dailyNotices.Count}개] {string.Join(" | ", dailyNotices)}");
        }

    
    public void OnNextDayButton()
    {
        dailyNotices.Clear();
        foreach (ActiveCase active in activeNewsList)
        {
            active.Rday--;
            active.currentDay++;    
        }
        // 불확정 뉴스 대기 처리 — 지정된 턴이 지나면 실제 효과 발동
        for (int i = pendingNewsList.Count - 1; i >= 0; i--)
        {
            pendingNewsList[i].daysLeft--;
            if (pendingNewsList[i].daysLeft <= 0)
            {
                AddNotice($"[속보] {pendingNewsList[i].caseData.CaseDescription}"); // 소문이 사실로 확정됐음을 알
                pendingNewsList.RemoveAt(i);
            }
        }
        currentDay += 1;
        // 대출 이자 시스템
        if (loanAmount > 0)
        {
            int interest = Mathf.CeilToInt(loanAmount * 0.01f); // 턴당 1%
            currentgold -= interest;
            UIManager.instance.UpdateGoldText(); 
            Debug.Log($"이자 {interest}G 차감. 남은 대출금: {loanAmount}G");
        }
        
        smallNewsTimer++; //소형 뉴스 생성 타이머 (템플릿 기반으로 통일 — 트리거는 기존과 동일한 3~5턴 주기)
        if (newsTemplates.Length > 0 && smallNewsTimer >= Random.Range(3, 6))
        {
            smallNewsTimer = 0;
            NewsTemplate t = newsTemplates[Random.Range(0, newsTemplates.Length)];
            var result = TemplateNewsGenerator.Generate(t);
            if (result.HasValue)
            {
                if (result.Value.isUncertain)
                {
                    // 불확정 뉴스 — 지금 당장 가격에 영향 주지 않고 대기열에 등록
                    pendingNewsList.Add(new PendingNews { caseData = result.Value.caseData, daysLeft = UNCERTAIN_NEWS_DELAY });
                    AddNotice($"[소문] {result.Value.caseData.CaseDescription}");
                }
                else
                {
                    // 확정 뉴스 — 기존 방식대로 바로 활성화
                    AddActiveNews(result.Value.caseData);
                }
            }
        }      
        // 전조 뉴스 — 대형 뉴스 발생 2~3턴 전
        if (currentDay == scheduledBigNewsVibeDay
            && scheduledBigNewsCaseIndex >= 0
            && scheduledBigNewsCaseIndex < BigTriggerCases.Count)
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
        if (scheduledBigNewsDay > 0 && currentDay >= scheduledBigNewsDay)
        {
            if (scheduledBigNewsCaseIndex >= 0 && scheduledBigNewsCaseIndex < BigTriggerCases.Count)
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
    UIManager.instance.UpdateEraText(); // ← 추가
}

// 시대 종료
if (currentEra != null && currentDay >= eraEndDay)
{
    AddNotice($"{currentEra.eraName}이(가) 막을 내렸습니다.");
    currentEra = null;
    nextEraStartDay = currentDay + ERA_COOLDOWN;
    UIManager.instance.UpdateEraText(); // ← 추가
}

// 시대 효과 적용 — 기존 가격 변동 루프(tabmanager.allstock 순회하는 부분) 안에 끼워 넣기
if (currentEra != null)
{
    foreach (Stock stock in tabmanager.allstock)
    {
        if (System.Array.Exists(currentEra.boostedIndustries, t => t == stock.IndustryTag))
            stock.StockCurrentPrice = Mathf.RoundToInt(stock.StockCurrentPrice * (1 + currentEra.boostedRate / 100f));
        else if (System.Array.Exists(currentEra.weakenedIndustries, t => t == stock.IndustryTag))
            stock.StockCurrentPrice = Mathf.RoundToInt(stock.StockCurrentPrice * (1 - currentEra.weakenedRate / 100f));

        if (stock.StockCurrentPrice < 1) stock.StockCurrentPrice = 1;
    }
}

    RecalculateFinancials();
    ProcessBusinesses(); 

        for (int i = 0; i < tabmanager.allstock.Length; i++) //상세 패널에 전날 기록이 안 남도록 함
        {
            float range = 2f;
            tabmanager.allstock[i].StockChangePrice = 0;
            tabmanager.allstock[i].StockPreviousPrice = tabmanager.allstock[i].StockCurrentPrice;
            switch(tabmanager.allstock[i].volatilityType)
            {
                case Stock.VolatilityType.Stable:
                    range = 1.5f; break;
                case Stock.VolatilityType.Normal:
                    range = 2.5f; break;
                case Stock.VolatilityType.Volatile:
                    range = 4f; break;
            }
            float bias = 0.15f;
            float Randomchange = Random.Range(-range, range) + bias;
            tabmanager.allstock[i].StockCurrentPrice = Mathf.RoundToInt(tabmanager.allstock[i].StockCurrentPrice * (1 + Randomchange / 100f));

            if (tabmanager.allstock[i].StockCurrentPrice < 1)
            {
                tabmanager.allstock[i].StockCurrentPrice = 1;
            }
        }
        StockControl[] allstocks = FindObjectsByType<StockControl>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (ActiveCase active in activeNewsList)
        {
            float rate = active.currentDay < active.caseData.lowPercent.Length
                ? active.caseData.lowPercent[active.currentDay]
                : 0f;

            if (active.caseData.newsType == Case.NewsType.BigTriggerNews
                && active.caseData.industryRates != null
                && active.caseData.industryRates.Length > 0)

            {
                foreach (Stock stock in tabmanager.allstock)
                {
                    // 이 주식의 산업군에 맞는 변동률 찾기
                    Case.IndustryRate matchedRate = default;
                    bool found = false;
                    foreach (Case.IndustryRate ir in active.caseData.industryRates)
                    {
                        if (ir.industryTag == stock.IndustryTag)
                        {
                            matchedRate = ir;
                            found = true;
                            break;
                        }
                    }
                    if (!found) continue;

            // 당사국 여부 확인
            bool isInvolved = active.involvedNations.Contains(stock.nation);

            int minRate = isInvolved ? matchedRate.changePriceMin : matchedRate.changePriceMinNon;
            int maxRate = isInvolved ? matchedRate.changePriceMax : matchedRate.changePriceMaxNon;

            stock.StockCurrentPrice = Mathf.RoundToInt(
                stock.StockCurrentPrice * (1 + Random.Range(minRate, maxRate + 1) / 100f * rate));

            if (stock.StockCurrentPrice < 1) stock.StockCurrentPrice = 1;

            Debug.Log($"[{active.caseData.CaseName}] {stock.StockName} | " +
                $"산업군: {stock.IndustryTag} | 당사국: {isInvolved} | " +
                $"변동 전: {stock.StockPreviousPrice} | 변동 후: {stock.StockCurrentPrice}");
        }
    }
    else
    {
        // 소형 뉴스 — 주 효과
        for (int i = 0; i < active.caseData.affectedCase.Length; i++)
        {
            Stock affected = active.caseData.affectedCase[i];
            if (affected == null)
            {
                Debug.LogError($"[{active.caseData.CaseName}]의 affectedCase[{i}]가 null입니다!");
                continue;
            }
            affected.StockCurrentPrice = Mathf.RoundToInt(
                affected.StockCurrentPrice *
                (1 + Random.Range(active.caseData.ChangePriceMin[i],
                active.caseData.ChangePriceMax[i] + 1) / 100f * rate));
            if (affected.StockCurrentPrice < 1) affected.StockCurrentPrice = 1;
            Debug.Log($"[{active.caseData.CaseName}] {affected.StockName} | " +
                $"변동 전: {affected.StockPreviousPrice} | 감쇠율: {rate} | 변동 후: {affected.StockCurrentPrice}");
            foreach (StockControl sc in allstocks)
                if (sc.stockinfo.StockName == affected.StockName) sc.UpdatePriceText();
        }

        // 연쇄 효과 — 뉴스 2일차에만 실행
        if (active.currentDay == 1 && active.caseData.chainEffects != null)
        {
            foreach (Case.ChainEffect effect in active.caseData.chainEffects)
            {
                if (effect.stock == null) continue;
                effect.stock.StockCurrentPrice = Mathf.RoundToInt(
                    effect.stock.StockCurrentPrice *
                    (1 + Random.Range((float)effect.priceMin, effect.priceMax) / 100f * rate));
                if (effect.stock.StockCurrentPrice < 1) effect.stock.StockCurrentPrice = 1;
                foreach (StockControl sc in allstocks)
                    if (sc.stockinfo.StockName == effect.stock.StockName) sc.UpdatePriceText();
            }
        }
    }
    

        if (active.caseData.newsType == Case.NewsType.BigTriggerNews)
        {
            foreach (StockControl sc in allstocks)
                sc.UpdatePriceText();
        }

        if (active.currentDay == 0)
        {
            AddNotice(active.displayDescription);
        }
    }
        activeNewsList.RemoveAll(active => active.Rday <= 0); // 지속일이 끝난 뉴스 제거
        // 씬의 모든 NPC에게 오늘 주식 데이터를 전달한 뒤 매매 결정 실행
        NPCInvestor[] npcs = FindObjectsByType<NPCInvestor>(FindObjectsSortMode.None);
        foreach (NPCInvestor npc in npcs)
        {
            foreach (Stock stock in tabmanager.allstock)
            {
                bool isAffected = activeNewsList.Exists(active =>
                    active.caseData.affectedCase != null &&
                    System.Array.Exists(active.caseData.affectedCase, s => s == stock));

                npc.OnDayPassed(stock, stock.StockChangePrice, isAffected ? 1f : 0f);
            }
            npc.DecideAfterAllPredictions();
        }
        UIManager.instance.UpdateDayText();

        // 골드 1000 이하 연속 5턴 지속 시 게임 오버
        if (currentgold <= 1000)
        {
            lowGoldStreak++;
            if (lowGoldStreak >= 5)
                UnityEngine.SceneManagement.SceneManager.LoadScene("GameOver");
        }
        else
        {
            lowGoldStreak = 0;
        }
        // 기존 lowGoldStreak 체크 아래에 추가
        if (loanAmount > 0 && currentgold < loanAmount)
        {
            // 골드가 대출금보다 적으면 상환 불가 → 게임 오버
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameOver");
        }
        SaveStockDataToCSV();
        UIManager.instance.UpdateTotalAssetText(); // ← 추가: 주가 변동 후 총 자산 갱신
        UIManager.instance.favoritePanel.RefreshHome(); // ← 추가
    }
        
    
    // 데이터 생성용 임시 함수 — 나중에 삭제
    public void AutoSimulate(int days)
    {
        for (int i = 0; i < days; i++)
        {
            OnNextDayButton();
        }
        Debug.Log($"{days}일 시뮬레이션 완료! CSV 확인하세요.");
    }
    // 다음 대형 뉴스 발생 날짜·케이스·국가 쌍·전조 뉴스를 무작위로 예약
    void ScheduleNextBigNews()
    {
        if (BigTriggerCases.Count == 0) return;
        scheduledBigNewsCaseIndex = Random.Range(0, BigTriggerCases.Count);
        scheduledBigNewsDay = currentDay + Random.Range(30, 51);
        Case bigCase = BigTriggerCases[scheduledBigNewsCaseIndex];

        // 대형 뉴스의 국가 쌍을 미리 결정 — 전조 뉴스와 공유
        if (bigCase.nationPairs != null && bigCase.nationPairs.Length > 0)
            scheduledNationPair = bigCase.nationPairs[Random.Range(0, bigCase.nationPairs.Length)];
        else
            scheduledNationPair = "";

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
        int offset = ((foreshadow != null && foreshadow.lastingDay > 0) ? foreshadow.lastingDay : 2) + 1;
        scheduledBigNewsVibeDay = scheduledBigNewsDay - offset;
    }

    // 케이스를 활성 뉴스로 등록 — 국가 쌍을 플레이스홀더에 치환하고 관련 국가 목록을 초기화
    public void AddActiveNews(Case casedata, string forcedNationPair = null,  bool isRumorConfirmed = false)
    {
        Debug.Log($"뉴스 추가: {casedata.CaseName}");
        ActiveCase newActive = new ActiveCase();
        newActive.caseData = casedata;
        newActive.Rday = casedata.lastingDay;
        newActive.currentDay = 0;
        if (casedata.nationPairs != null && casedata.nationPairs.Length > 0)
        {
            string picked = (!string.IsNullOrEmpty(forcedNationPair))
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
                newActive.displayDescription = casedata.CaseDescription
                    .Replace("[국가]", picked);
                newActive.involvedNations.Add(picked);
            }
        }
        else
        {
            newActive.displayDescription = casedata.CaseDescription;
        }
        if (isRumorConfirmed)
        newActive.displayDescription = "[속보] " + newActive.displayDescription; // 소문이 사실로 확정됐음을 표시 — 매일 효과 루프에서 이 문구가 그대로 한 번만 뜸

        activeNewsList.Add(newActive);
        recentIssues.Add(casedata);
    }
    // 매턴 모든 회사의 매출·영업이익을 재계산
    void RecalculateFinancials()
    {
        foreach (Stock stock in tabmanager.allstock)
        {
            float revenueModifier = 1.0f;

            if (currentEra != null)
            {
                if (System.Array.Exists(currentEra.boostedIndustries, 
                    t => t == stock.IndustryTag))
                    revenueModifier = 1.2f;
                else if (System.Array.Exists(currentEra.weakenedIndustries, 
                    t => t == stock.IndustryTag))
                    revenueModifier = 0.8f;
            }

            stock.revenue = Mathf.RoundToInt(stock.baseRevenue * revenueModifier);
            stock.OperatingProfit = stock.revenue - stock.baseCost;
        }
    }
    // 사업 시스템 — 매턴 호출
    void ProcessBusinesses()
    {
    // ── 진행 중인 사업 처리 ──
        for (int i = activeBusinesses.Count - 1; i >= 0; i--)
        {
            ActiveBusiness biz = activeBusinesses[i];
            biz.remainingTurns--;

            // 진행 중: 매턴 투자비만큼 영업이익 감소 (비용 증가 효과)
            Stock stock = System.Array.Find(tabmanager.allstock, 
                s => s.StockName == biz.stockName);
            if (stock != null)
            {
                stock.OperatingProfit -= biz.investmentCost;
            }

            // 사업 완료 판정
            if (biz.remainingTurns <= 0)
            {
                bool success = Random.Range(0, 100) < biz.successRate;

                if (success && stock != null)
                {
                    // 성공: baseRevenue 영구 상승
                    stock.baseRevenue += biz.revenueBoost;
                    AddNotice($"[사업 성공] {biz.successNews}");
                }
                else
                {
                    // 실패: 투자금은 이미 날린 상태, 뉴스만 출력
                    AddNotice($"[사업 실패] {biz.failNews}");
                }

                activeBusinesses.RemoveAt(i);
            }
        }

    // ── 새 사업 착수 (일정 주기마다) ──
        businessTimer++;
        if (businessTimer >= Random.Range(8, 15)) // 8~14턴 주기
        {
            businessTimer = 0;
            TryStartNewBusiness();
        }
    }

// 사업 풀에서 하나 골라서 착수
    void TryStartNewBusiness()
    {
        if (allBusinessProjects.Length == 0) return;

        // 이미 사업 진행 중인 회사는 제외
        BusinessProject picked = allBusinessProjects[Random.Range(0, allBusinessProjects.Length)];
        if (picked.targetStock == null) return;

        bool alreadyBusy = activeBusinesses.Exists(
            b => b.stockName == picked.targetStock.StockName);
        if (alreadyBusy) return;

        // 사업 착수
        ActiveBusiness newBiz = new ActiveBusiness();
        newBiz.projectName = picked.projectName;
        newBiz.stockName = picked.targetStock.StockName;
        newBiz.remainingTurns = picked.duration;
        newBiz.investmentCost = picked.investmentCost;
        newBiz.successRate = picked.successRate;
        newBiz.revenueBoost = picked.revenueBoost;
        newBiz.successNews = picked.successNews;
        newBiz.failNews = picked.failNews;

        activeBusinesses.Add(newBiz);
        AddNotice($"[신사업] {picked.startNews}");
    }    
}