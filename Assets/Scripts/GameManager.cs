using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;

public class GameManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public static GameManager Instance; //static은 쉽게 접근하기 위해 사용!
    public int currentgold;
    public int currentDay;
    public TextMeshProUGUI Ownedgold;
    //게임 전체에서 공유되어야 할 데이터들을 작성
    //그 외에도, 게임 흐름이나 저장/불러오기 등도 여기서 작성
    void Awake()
    {
        Instance = this;// 외부에서의 연결을 위해 자기 자신을 instance에 넣음
    }
    void Start()
    {
        LoadGame();
        Ownedgold.text = $"현재 골드 :{GameManager.Instance.currentgold}";
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    [System.Serializable]
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
        public List<StockSaveData> OwnStocks;
    }
    public void SaveGame()
    {
        GameSaveData data = new GameSaveData();
        data.currentgold = currentgold;
        data.currentday = currentDay;
        data.OwnStocks = new List<StockSaveData>();
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
        
        string json = JsonUtility.ToJson(data);
        string path = Application.persistentDataPath + "/save.json"; //유니티가 정해준 저장 폴더에 json 파일 저장
        File.WriteAllText(path, json);
    }
    void LoadGame()
    {
        string path = Application.persistentDataPath + "/save.json";
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);//파일을 문자열로 변환
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
            currentgold = data.currentgold;//값을 업데이트해줘야 함
            currentDay = data.currentday;
            StockControl[] allStocks = FindObjectsByType<StockControl>(FindObjectsSortMode.None);
            foreach (StockSaveData savedstock in data.OwnStocks)
            {
                foreach (StockControl stock in allStocks)
                if (savedstock.stockname == stock.stockinfo.StockName)
                    {
                        stock.ownedAmount = savedstock.ownamount;
                        stock.currentPrice = savedstock.currentPrice;
                    }
            }
            
        }
        else
        {
            currentgold = 10000;  // 시작 골드
            currentDay = 1;
        }
    }
    public void UpdateGold(int amount)
    {
        currentgold += amount;
        Ownedgold.text = $"현재 골드 : {currentgold}";
    }
}
