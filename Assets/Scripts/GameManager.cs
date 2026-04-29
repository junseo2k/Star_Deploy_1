using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;
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
    public List<Case> BigTriggerCases = new List<Case>();
    public int lowGoldStreak;
    public int lastBigNewsDay = -1;
    public int scheduledBigNewsDay = -1;
    public int scheduledBigNewsVibeDay = -1;
    public int scheduledBigNewsCaseIndex = -1;
    public int scheduledForeshadowIndex = -1;
    public string scheduledNationPair = "";

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

        if (scheduledBigNewsDay <= currentDay)
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
    }
    // JSON 저장을 위한 게임 전체 데이터 구조체
    [System.Serializable]
    public class GameSaveData
    {
        public int currentgold;
        public int currentday;
        public List<StockSaveData> OwnStocks;
        public int lastBigNewsDay;
        public int scheduledBigNewsDay;
        public int scheduledBigNewsVibeDay;
        public int scheduledBigNewsCaseIndex;
        public int scheduledForeshadowIndex;
        public string scheduledNationPair;
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

        data.lastBigNewsDay = lastBigNewsDay;
        data.scheduledBigNewsDay = scheduledBigNewsDay;
        data.scheduledBigNewsVibeDay = scheduledBigNewsVibeDay;
        data.scheduledBigNewsCaseIndex = scheduledBigNewsCaseIndex;
        data.scheduledForeshadowIndex = scheduledForeshadowIndex;
        data.scheduledNationPair = scheduledNationPair;

        string json = JsonUtility.ToJson(data);
        string path = Application.persistentDataPath + "/save.json";
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
            lastBigNewsDay = data.lastBigNewsDay;
            scheduledBigNewsDay = data.scheduledBigNewsDay;
            scheduledBigNewsVibeDay = data.scheduledBigNewsVibeDay;
            scheduledBigNewsCaseIndex = data.scheduledBigNewsCaseIndex;
            scheduledForeshadowIndex = data.scheduledForeshadowIndex;
            scheduledNationPair = data.scheduledNationPair ?? "";

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
        foreach (ActiveCase active in activeNewsList)
        {
            active.Rday--;
            active.currentDay++;    
        }
        currentDay += 1;
        int smallnewscount = SmallNewsCases.Count > 0 ? Random.Range(0, 4) : 0;
        for (int i = 0; i < smallnewscount; i++)
        {
            int RandomSmall = Random.Range(0, SmallNewsCases.Count);
            Case picked = SmallNewsCases[RandomSmall];
            if (activeNewsList.Exists(active => active.caseData == picked)) continue;
            AddActiveNews(picked);
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
        int offset = (foreshadow != null && foreshadow.lastingDay > 0) ? foreshadow.lastingDay : 2;
        scheduledBigNewsVibeDay = scheduledBigNewsDay - offset;
    }

    public void AddActiveNews(Case casedata, string forcedNationPair = null)
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

        activeNewsList.Add(newActive);
        recentIssues.Add(casedata);
    }
}
