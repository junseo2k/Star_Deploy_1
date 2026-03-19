using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;

public class GameManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public static GameManager Instance; //static은 쉽게 접근하기 위해 사용!
    public int currentgold; //현재 골드
    public int currentDay; //현재 날짜
    public CaseOccur caseoccur;
    public Case OccurCase;
    public int randonindex;
    //게임 전체에서 공유되어야 할 데이터들을 작성
    //그 외에도, 게임 흐름이나 저장/불러오기 등도 여기서 작성

    void Awake() //start보다 먼저 일어남
    {
        Instance = this;// 외부에서의 연결을 위해 자기 자신을 instance에 넣음
    }
    void Start()
    {
        LoadGame(); //게임 로드
        UIManager.instance.UpdateDayText();
        UIManager.instance.UpdateGoldText();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    [System.Serializable] //json에 저장 시 필요함. 즉 저장할 데이터들을 class로 묶음
    public class StockSaveData
    //주식 관련 데이터
    {
        public string stockname;
        public int currentPrice;
        public int ownamount;
    }
    [System.Serializable]
    public class GameSaveData
    //골드, 날짜 등 게임 전체 데이터
    {
        public int currentgold;
        public int currentday;
        public List<StockSaveData> OwnStocks; //보유 주식
    }
    public void SaveGame() //게임 진행 상황을 json 파일에 저장
    {
        GameSaveData data = new GameSaveData(); //저장할 데이터 생성
        data.currentgold = currentgold;
        data.currentday = currentDay;
        data.OwnStocks = new List<StockSaveData>();
        StockControl[] allStocks = FindObjectsByType<StockControl>(FindObjectsSortMode.None); //주식들 검색
        foreach (StockControl stock in allStocks)
        {
            if (stock.ownedAmount > 0) //보유한 주식이 있다면 주식 데이터를 위에서 설정한 클래스에 맞춘 후 저장할 데이터에 넣음
            {
                StockSaveData stockdata = new StockSaveData();
                stockdata.stockname = stock.stockinfo.StockName;
                stockdata.currentPrice = stock.stockinfo.StockBasePrice;
                stockdata.ownamount = stock.ownedAmount;
                data.OwnStocks.Add(stockdata);
            }
        }
        
        string json = JsonUtility.ToJson(data);
        string path = Application.persistentDataPath + "/save.json"; //유니티가 정해준 저장 폴더에 json 파일 저장
        File.WriteAllText(path, json); //json으로 변환 후 저장
    }
    void LoadGame() //게임 로드
    {
        string path = Application.persistentDataPath + "/save.json";
        if (File.Exists(path)) //저장한 json 파일이 있다면
        {
            string json = File.ReadAllText(path);//파일을 문자열로 변환
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
            currentgold = data.currentgold;//값을 따로 업데이트
            currentDay = data.currentday;
            StockControl[] allStocks = FindObjectsByType<StockControl>(FindObjectsSortMode.None);
            foreach (StockSaveData savedstock in data.OwnStocks) //주식 정보도 업데이트
            {
                foreach (StockControl stock in allStocks)
                if (savedstock.stockname == stock.stockinfo.StockName)
                    {
                        stock.ownedAmount = savedstock.ownamount;
                        stock.stockinfo.StockCurrentPrice = savedstock.currentPrice;
                    }
            }
            
        }
        else //저장된 파일 없을 때 기본값 설정
        {
            currentgold = 10000;  // 시작 골드
            currentDay = 1;
        }
    }
    public void UpdateGold(int amount) //주식 구매/판매 시 골드 업데이트 함수
    {
        currentgold += amount;
    }
    public void OnNextDayButton() //다음날 버튼.
    {
        currentDay += 1;
        int randonindex = Random.Range(0, caseoccur.AllCases.Length);
        Case OccurCase = caseoccur.AllCases[randonindex];
        caseoccur.CaseOccurMethod(OccurCase);
        UIManager.instance.UpdateDayText();
    }
}
