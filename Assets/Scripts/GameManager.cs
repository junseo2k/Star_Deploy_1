using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;

// 게임 전체 흐름을 관리하는 싱글톤 클래스 — 골드·날짜·사건 처리·저장/불러오기를 담당
public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // 어디서든 GameManager에 접근하기 위한 싱글톤 인스턴스
    public int currentgold;  // 현재 보유 골드
    public int currentDay;   // 현재 게임 날짜
    public CaseOccur caseoccur; // 사건 발생 처리 컴포넌트 참조
    public Case OccurCase;      // 이번 날짜에 발생한 사건 (참고용)
    public int randonindex;     // 랜덤으로 선택된 사건 인덱스 (참고용)
    public TabManager tabmanager;
    public List<Case> recentIssues;

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

    // 다음 날 버튼 클릭 시 호출 — 날짜 증가, 랜덤 사건 발생, UI 갱신
    public void OnNextDayButton()
    {
        currentDay += 1;
        int randomindex = Random.Range(0, caseoccur.AllCases.Length); // 랜덤 사건 선택
        Case OccurCase = caseoccur.AllCases[randomindex];
        for (int i = 0; i < tabmanager.allstock.Length; i++)
        {
            tabmanager.allstock[i].StockChangePrice = 0;
        }
        caseoccur.CaseOccurMethod(OccurCase); // 사건 효과 적용
        recentIssues.Add(OccurCase);
        UIManager.instance.UpdateDayText();
    }
}
