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
    }

    void Update()
    {

    }

    // JSON 저장을 위한 주식 데이터 구조체
    [System.Serializable]
    public class StockSaveData
    {
        public string stockname;  // 주식 이름 (식별 키)
        public int currentPrice;  // 저장 시점의 주식 가격
        public int ownamount;     // 보유 수량
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
                        stock.stockinfo.StockCurrentPrice = savedstock.currentPrice;
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
        }

        if (Random.Range(0, 100) <= 1)
        {
            int RandomBig = Random.Range(0, BigRandomCases.Count);
            Case picked = BigRandomCases[RandomBig];
            if (!activeNewsList.Exists(active => active.caseData == picked))
            {
                AddActiveNews(picked);            
            }    
        }
        //대형 뉴스 출력. triggerKey >= 100인 것들
        if (triggerWar >= 100)
        {
            foreach (Case c in BigTriggerCases)
            {
                if (c.triggerKey == "전쟁")
                {
                    AddActiveNews(c);
                    triggerWar = 0;
                    break;
                }
            }
        }
        if (triggerPeace >= 100)
        {
            foreach (Case c in BigTriggerCases)
            {
                if (c.triggerKey == "평화")
                {
                    AddActiveNews(c);
                    triggerPeace = 0;
                    break;
                }
            }
        }
        if (triggerRevolt >= 100)
        {
            foreach (Case c in BigTriggerCases)
            {
                if (c.triggerKey == "반란")
                {
                    AddActiveNews(c);
                    triggerRevolt = 0;
                    break;
                }
            }
        }
        if (triggerTrade >= 100)
        {
            foreach (Case c in BigTriggerCases)
            {
                if (c.triggerKey == "무역")
                {
                    AddActiveNews(c);
                    triggerTrade = 0;
                    break;
                }
            }
        }
        if (triggerFestival >= 100)
        {
            foreach (Case c in BigTriggerCases)
            {
                if (c.triggerKey == "축제")
                {
                    AddActiveNews(c);
                    triggerFestival = 0;
                    break;
                }
            }
        }
        if (triggerDispute >= 100)
        {
            foreach (Case c in BigTriggerCases)
            {
                if (c.triggerKey == "분쟁")
                {
                    AddActiveNews(c);
                    triggerDispute = 0;
                    break;
                }
            }
        }
        if (triggerRevolution >= 100)
        {
            foreach (Case c in BigTriggerCases)
            {
                if (c.triggerKey == "혁명")
                {
                    AddActiveNews(c);
                    triggerRevolution = 0;
                    break;
                }
            }
        }
        if (triggerInnovation >= 100)
        {
            foreach (Case c in BigTriggerCases)
            {
                if (c.triggerKey == "혁신")
                {
                    AddActiveNews(c);
                    triggerInnovation = 0;
                    break;
                }
            }
        }

        for (int i = 0; i < tabmanager.allstock.Length; i++) //상세 패널에 전날 기록이 안 남도록 함
        {
            float range = 2f;
            tabmanager.allstock[i].StockChangePrice = 0;
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
            for (int i = 0; i < active.caseData.affectedCase.Length; i++)
            {
                int priceBefore = active.caseData.affectedCase[i].StockCurrentPrice; // 변동 전 가격 저장
                active.caseData.affectedCase[i].StockCurrentPrice = Mathf.RoundToInt(active.caseData.affectedCase[i].StockCurrentPrice * (1 + active.caseData.ChangePrice[i] / 100f * rate));
                if (active.caseData.affectedCase[i].StockCurrentPrice < 1)
                {
                    active.caseData.affectedCase[i].StockCurrentPrice = 1;
                }
                 Debug.Log($"[{active.caseData.CaseName}] " +
                        $"{active.caseData.affectedCase[i].StockName} | " +
                        $"변동 전: {priceBefore} | " +
                        $"변동률: {active.caseData.ChangePrice[i]}% | " +
                        $"감쇠율: {rate} | " +
                        $"변동 후: {active.caseData.affectedCase[i].StockCurrentPrice}");

                // 변경된 주식에 해당하는 StockControl UI 텍스트를 갱신
                foreach (StockControl stock in allstocks)
                {
                    if (stock.stockinfo.StockName == active.caseData.affectedCase[i].StockName)
                    {
                        stock.UpdatePriceText();
                    
                    }
                }
            }
            active.currentDay++;  // 모든 active에 적용
            active.Rday--;//
            UIManager.instance.NoticeText.text = active.caseData.CaseDescription;
        }
        activeNewsList.RemoveAll(active => active.Rday <= 0); 
        UIManager.instance.UpdateDayText();
    }
    public void AddActiveNews(Case casedata)
    {
        Debug.Log($"뉴스 추가: {casedata.CaseName}");
        ActiveCase newActive = new ActiveCase();
        newActive.caseData = casedata;
        newActive.Rday = casedata.lastingDay;
        newActive.currentDay = 0;
        activeNewsList.Add(newActive);
        recentIssues.Add(casedata);
    }
}
