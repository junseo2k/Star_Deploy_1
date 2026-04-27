using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;
using NUnit.Framework;
using UnityEditor.ShaderGraph;
using Unity.VisualScripting;

// 게임 전체 흐름을 관리하는 싱글톤 클래스 — 골드·날짜·사건 처리·저장/불러오기를 담당
public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // 어디서든 GameManager에 접근하기 위한 싱글톤 인스턴스
    public int currentgold;  // 현재 보유 골드
    public int currentDay;   // 현재 게임 날짜
    public Case[] AllCases; // 사건 발생 처리 컴포넌트 참조
    public int randonindex;     // 랜덤으로 선택된 사건 인덱스 (참고용)
    public TabManager tabmanager;
    public List<Case> recentIssues;
    public List<ActiveCase> activeNewsList = new List<ActiveCase>();
    public int randomindex;
    public List<Case> SmallNewsCases = new List<Case>();
    public List<Case> SmallVibeCases = new List<Case>();
    public List<Case> BigRandomCases = new List<Case>();
    public List<Case> BigTriggerCases = new List<Case>();
    public int triggerWar;
    public int triggerPeace;
    public int triggerRevolt;
    public int triggerTrade;
    public int triggerFestival;
    public int triggerDispute;
    public int triggerRevolution;
    public int triggerInnovation;
    public int lowGoldStreak; // 골드가 1000 이하로 지속된 연속 턴 수

    // Start보다 먼저 실행 — 싱글톤 인스턴스 등록
    void Awake()
    {
        Instance = this; // 외부에서 GameManager.Instance로 접근 가능하도록 자신을 등록
    }

    void Start()
    {
        LoadGame(); // 저장된 게임 데이터 불러오기
        UIManager.instance.UpdateDayText();
        UIManager.instance.UpdateGoldText();
        Debug.Log(Application.persistentDataPath);
        foreach (Case c in AllCases)
        {
           switch(c.newsType)
            {
                case Case.NewsType.SmallIndustryNews:
                case Case.NewsType.SmallCompanyNews:// 하나의 리스트에 같이 넣기
                SmallNewsCases.Add(c);
                break;
                case Case.NewsType.SmallVibeNews:
                SmallVibeCases.Add(c);
                break;
                case Case.NewsType.BigRandomNews:
                BigRandomCases.Add(c);
                break;
                case Case.NewsType.BigTriggerNews:
                BigTriggerCases.Add(c);
                break;

            }
        }
         foreach (Stock stock in tabmanager.allstock)
        {
            stock.StockCurrentPrice = stock.StockBasePrice;
            stock.StockPreviousPrice = stock.StockBasePrice;
        }
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
    }
    // JSON 저장을 위한 게임 전체 데이터 구조체
    [System.Serializable]
    public class GameSaveData
    {
        public int currentgold;           // 보유 골드
        public int currentday;            // 현재 날짜
        public List<StockSaveData> OwnStocks; // 보유 중인 주식 목록
    }

    // 현재 게임 상태를 JSON 파일로 저장
    public void SaveGame()
    {
        GameSaveData data = new GameSaveData();
        data.currentgold = currentgold;
        data.currentday = currentDay;
        data.OwnStocks = new List<StockSaveData>();

        // 씬의 모든 StockControl을 검색하여 보유 수량이 있는 주식만 저장 목록에 추가
        StockControl[] allStocks = FindObjectsByType<StockControl>(FindObjectsSortMode.None);
        foreach (StockControl stock in allStocks)
        {
            if (stock.ownedAmount > 0)
            {
                StockSaveData stockdata = new StockSaveData();
                stockdata.stockname = stock.stockinfo.StockName;
                stockdata.currentPrice = stock.stockinfo.StockBasePrice;
                stockdata.ownamount = stock.ownedAmount;
                stockdata.avgPurchasePrice = stock.avgPurchasePrice;
                data.OwnStocks.Add(stockdata);
            }
        }

        // GameSaveData를 JSON 문자열로 변환 후 파일에 저장
        string json = JsonUtility.ToJson(data);
        string path = Application.persistentDataPath + "/save.json"; // 유니티 지정 저장 경로
        File.WriteAllText(path, json);
    }

    // JSON 파일에서 게임 데이터를 불러와 복원
    void LoadGame()
    {
        string path = Application.persistentDataPath + "/save.json";
        if (File.Exists(path)) // 저장 파일이 있을 경우 불러오기
        {
            string json = File.ReadAllText(path); // JSON 파일을 문자열로 읽기
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
            currentgold = data.currentgold;
            currentDay = data.currentday;

            // 저장된 주식 보유 정보를 씬의 StockControl에 복원
            StockControl[] allStocks = FindObjectsByType<StockControl>(FindObjectsSortMode.None);
            foreach (StockSaveData savedstock in data.OwnStocks)
            {
                foreach (StockControl stock in allStocks)
                    if (savedstock.stockname == stock.stockinfo.StockName)
                    {
                        stock.ownedAmount = savedstock.ownamount;
                        stock.avgPurchasePrice = savedstock.avgPurchasePrice;
                    }
            }
        }
        else // 저장 파일이 없으면 기본값으로 시작
        {
            currentgold = 10000; // 시작 골드
            currentDay = 1;
        }
    }

    // 주식 구매/판매 시 골드 증감 처리 (구매 시 음수, 판매 시 양수 전달)
    public void UpdateGold(int amount)
    {
        currentgold += amount;
    }

    // 다음 날 버튼 클릭 시 호출 — 날짜 증가, 랜덤 사건 발생, UI 갱신. 감쇠는 매일 발생하므로 이곳에 계산식을 둠.
    public void OnNextDayButton()
    {
        currentDay += 1;
        int smallnewscount = Random.Range(0, 4);
        for (int i = 0; i < smallnewscount; i++)
        {
            int RandomSmall = Random.Range(0, SmallNewsCases.Count);
            Case picked = SmallNewsCases[RandomSmall];
            if (activeNewsList.Exists(active => active.caseData == picked)) continue;
            if (!string.IsNullOrEmpty(picked.triggerKey))
            {
                switch(picked.triggerKey)
                {
                    case "전쟁":
                    triggerWar += picked.triggervalue;
                    break;
                    case "평화":
                    triggerPeace += picked.triggervalue;
                    break;
                    case "반란":
                    triggerRevolt += picked.triggervalue;
                    break;
                    case "무역":
                    triggerTrade += picked.triggervalue;
                    break;
                    case "축제":
                    triggerFestival += picked.triggervalue;
                    break;
                    case "분쟁":
                    triggerDispute += picked.triggervalue;
                    break;
                    case "혁명":
                    triggerRevolution += picked.triggervalue;
                    break;
                    case "혁신":
                    triggerInnovation += picked.triggervalue;
                    break;
                }
            }
            AddActiveNews(picked);
        }
        int smallvibecount = Random.Range(0, 2);
        for (int i = 0; i < smallvibecount; i++)
        {
            int RandomvibeSmall = Random.Range(0, SmallVibeCases.Count);
            Case Vibepicked = SmallVibeCases[RandomvibeSmall];
            if (activeNewsList.Exists(active => active.caseData == Vibepicked)) continue;
            switch(Vibepicked.triggerKey)
            {
                case "전쟁":
                triggerWar += Vibepicked.triggervalue;
                break;
                case "평화":
                triggerPeace += Vibepicked.triggervalue;
                break;
                case "반란":
                triggerRevolt += Vibepicked.triggervalue;
                break;
                case "무역":
                triggerTrade += Vibepicked.triggervalue;
                break;
                case "축제":
                triggerFestival += Vibepicked.triggervalue;
                break;
                case "분쟁":
                triggerDispute += Vibepicked.triggervalue;
                break;
                case "혁명":
                triggerRevolution += Vibepicked.triggervalue;
                break;
                case "혁신":
                triggerInnovation += Vibepicked.triggervalue;
                break;
            }
            AddActiveNews(Vibepicked);            
        }

        bool bigNewsFired = false;
        if (Random.Range(0, 100) <= 1)
        {
            int RandomBig = Random.Range(0, BigRandomCases.Count);
            Case picked = BigRandomCases[RandomBig];
            if (!activeNewsList.Exists(active => active.caseData == picked))
            {
                AddActiveNews(picked);
                bigNewsFired = true;
            }
        }
        //게이지 자연 감소
        triggerWar        = Mathf.Max(0, triggerWar        - 5);
        triggerPeace      = Mathf.Max(0, triggerPeace      - 5);
        triggerRevolt     = Mathf.Max(0, triggerRevolt     - 5);
        triggerTrade      = Mathf.Max(0, triggerTrade      - 5);
        triggerFestival   = Mathf.Max(0, triggerFestival   - 5);
        triggerDispute    = Mathf.Max(0, triggerDispute    - 5);
        triggerRevolution = Mathf.Max(0, triggerRevolution - 5);
        triggerInnovation = Mathf.Max(0, triggerInnovation - 5);

        //대형 뉴스 출력. 구간별 확률로 발생 (31~60: 5%, 61~80: 15%, 81~100: 30%)
        if (!bigNewsFired)
        {
            TryFireBigTrigger(ref triggerWar, "전쟁", ref bigNewsFired);
            TryFireBigTrigger(ref triggerPeace, "평화", ref bigNewsFired);
            TryFireBigTrigger(ref triggerRevolt, "반란", ref bigNewsFired);
            TryFireBigTrigger(ref triggerTrade, "무역", ref bigNewsFired);
            TryFireBigTrigger(ref triggerFestival, "축제", ref bigNewsFired);
            TryFireBigTrigger(ref triggerDispute, "분쟁", ref bigNewsFired);
            TryFireBigTrigger(ref triggerRevolution, "혁명", ref bigNewsFired);
            TryFireBigTrigger(ref triggerInnovation, "혁신", ref bigNewsFired);
        }

        for (int i = 0; i < tabmanager.allstock.Length; i++) //상세 패널에 전날 기록이 안 남도록 함
        {
            float range = 2f;
            tabmanager.allstock[i].StockChangePrice = 0;
            tabmanager.allstock[i].StockPreviousPrice = tabmanager.allstock[i].StockCurrentPrice;
            switch(tabmanager.allstock[i].volatilityType)
            {
                case Stock.VolatilityType.Stable:
                    range = 2f; break;
                case Stock.VolatilityType.Normal:
                    range = 3f; break;
                case Stock.VolatilityType.Volatile:
                    range = 5f; break;
            }
            float Randomchange = Random.Range(-range, range);
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

    // 대형 트리거 뉴스 — 산업군 기반으로 처리
    if ((active.caseData.newsType == Case.NewsType.BigTriggerNews
        || active.caseData.newsType == Case.NewsType.BigRandomNews)
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
        // 소형 뉴스 또는 BigRandomNews — 기존 방식
        for (int i = 0; i < active.caseData.affectedCase.Length; i++)
        {

            active.caseData.affectedCase[i].StockCurrentPrice = Mathf.RoundToInt(
                active.caseData.affectedCase[i].StockCurrentPrice *
                (1 + Random.Range(active.caseData.ChangePriceMin[i],
                active.caseData.ChangePriceMax[i] + 1) / 100f * rate));

            if (active.caseData.affectedCase[i].StockCurrentPrice < 1)
                active.caseData.affectedCase[i].StockCurrentPrice = 1;

            foreach (Stock s in tabmanager.allstock)
            {
                if (s.StockName == active.caseData.affectedCase[i].StockName)
                {
                    Debug.Log($"같은 오브젝트? : {ReferenceEquals(s, active.caseData.affectedCase[i])}");
                    break;
                }
            }   
            Debug.Log($"[{active.caseData.CaseName}] " +
                $"{active.caseData.affectedCase[i].StockName} | " +
                $"변동 전: {active.caseData.affectedCase[i].StockPreviousPrice} | 감쇠율: {rate} | " +
                $"변동 후: {active.caseData.affectedCase[i].StockCurrentPrice}");

            foreach (StockControl stock in allstocks)
            {
                if (stock.stockinfo.StockName == active.caseData.affectedCase[i].StockName)
                    stock.UpdatePriceText();
            }
        }
    }

    // UI 갱신 — 대형 뉴스는 모든 StockControl 갱신
    if (active.caseData.newsType == Case.NewsType.BigTriggerNews
    || active.caseData.newsType == Case.NewsType.BigRandomNews)
    {
        foreach (StockControl sc in allstocks)
            sc.UpdatePriceText();
    }

    active.currentDay++;
    active.Rday--;
    UIManager.instance.NoticeText.text = active.displayDescription;

        }
        activeNewsList.RemoveAll(active => active.Rday <= 0);
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
    }
    private void TryFireBigTrigger(ref int triggerValue, string key, ref bool fired)
    {
        if (fired) return;

        int chance = 0;
        if (triggerValue >= 81) chance = 30;
        else if (triggerValue >= 61) chance = 15;
        else if (triggerValue >= 31) chance = 5;

        if (chance > 0 && Random.Range(0, 100) < chance)
        {
            foreach (Case c in BigTriggerCases)
            {
                if (c.triggerKey == key)
                {
                    AddActiveNews(c);
                    triggerValue = 0;
                    fired = true;
                    break;
                }
            }
        }
    }

    public void AddActiveNews(Case casedata)
    {
        Debug.Log($"뉴스 추가: {casedata.CaseName}");
        ActiveCase newActive = new ActiveCase();
        newActive.caseData = casedata;
        newActive.Rday = casedata.lastingDay;
        newActive.currentDay = 0;
        if (casedata.nationPairs != null && casedata.nationPairs.Length > 0)
        {
            int randomIndex = Random.Range(0, casedata.nationPairs.Length);
            string picked = casedata.nationPairs[randomIndex];

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

        activeNewsList.Add(newActive);
        recentIssues.Add(casedata);
    }
}
