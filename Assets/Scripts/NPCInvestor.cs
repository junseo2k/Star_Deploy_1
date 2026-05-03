using UnityEngine;
using Unity.InferenceEngine;
using System.Collections.Generic;

public class NPCInvestor : MonoBehaviour
{
    [Header("모델 설정")]
    public ModelAsset modelAsset;
    public string investorName;

    [Header("성향 설정")]
    public float buyThreshold = 0.02f;
    public float sellThreshold = -0.02f;
    public float investRatio = 0.5f;

    [Header("상태")]
    public int gold = 5000;

    // Inspector에서 보이도록 직렬화
    [System.Serializable]
    public class StockHolding
    {
        public Stock stock;
        public int amount;
    }
    public List<StockHolding> holdings = new List<StockHolding>();

    private Worker worker;
    private const int WINDOW_SIZE = 10;
    private Queue<float[]> priceHistory = new Queue<float[]>();
    private List<(Stock stock, float prediction)> dailyPredictions = new List<(Stock, float)>();

    void Start()
    {
        var model = ModelLoader.Load(modelAsset);
        worker = new Worker(model, BackendType.CPU);
    }

    // 특정 주식 보유량 가져오기
    int GetHolding(Stock stock)
    {
        StockHolding h = holdings.Find(x => x.stock == stock);
        return h != null ? h.amount : 0;
    }

    // 특정 주식 보유량 설정
    void SetHolding(Stock stock, int amount)
    {
        StockHolding h = holdings.Find(x => x.stock == stock);
        if (h != null)
        {
            if (amount <= 0)
                holdings.Remove(h);
            else
                h.amount = amount;
        }
        else if (amount > 0)
        {
            holdings.Add(new StockHolding { stock = stock, amount = amount });
        }
    }

    // 주식별 큐
private Dictionary<Stock, Queue<float[]>> priceHistories 
    = new Dictionary<Stock, Queue<float[]>>();

public void OnDayPassed(Stock stock, float priceChangePct, float isAffectedByNews)
{
    if (!priceHistories.ContainsKey(stock))
        priceHistories[stock] = new Queue<float[]>();

    var history = priceHistories[stock];
    history.Enqueue(new float[] {
        stock.StockCurrentPrice,
        priceChangePct,
        isAffectedByNews
    });

    if (history.Count > WINDOW_SIZE)
        history.Dequeue();

    if (history.Count == WINDOW_SIZE)
        MakeDecision(stock, history);
}

void MakeDecision(Stock stock, Queue<float[]> history)
{
    float minPrice = float.MaxValue;
    float maxPrice = float.MinValue;
    foreach (var day in history)
    {
        if (day[0] < minPrice) minPrice = day[0];
        if (day[0] > maxPrice) maxPrice = day[0];
    }

    if (maxPrice == minPrice) return;

    float[] inputData = new float[WINDOW_SIZE * 3];
    int i = 0;
    foreach (var day in history)
    {
        inputData[i * 3 + 0] = (day[0] - minPrice) / (maxPrice - minPrice);
        inputData[i * 3 + 1] = day[1];
        inputData[i * 3 + 2] = day[2];
        i++;
    }

    var inputTensor = new Tensor<float>(new TensorShape(1, WINDOW_SIZE, 3), inputData);
    worker.Schedule(inputTensor);
    var output = worker.PeekOutput("output_0") as Tensor<float>;
    float predictedChange = output.DownloadToArray()[0];

    Debug.Log($"[{investorName}] {stock.StockName} 예측: {predictedChange:F2}%");
    dailyPredictions.Add((stock, predictedChange));
    inputTensor.Dispose();
}

    public void DecideAfterAllPredictions()
    {
        if (dailyPredictions.Count == 0) return;

        float avg = 0f;
        foreach (var p in dailyPredictions) avg += p.prediction;
        avg /= dailyPredictions.Count;

        Debug.Log($"[{investorName}] 오늘 예측 평균: {avg:F2}%");

        foreach (var (stock, prediction) in dailyPredictions)
        {
            float diff = prediction - avg;
            if (diff > buyThreshold)
                Buy(stock);
            else if (diff < sellThreshold)
                Sell(stock);
        }

        dailyPredictions.Clear();
    }

    void Buy(Stock stock)
    {
        int budget = Mathf.FloorToInt(gold * investRatio);
        int amount = budget / stock.StockCurrentPrice;
        if (amount <= 0) return;

        gold -= amount * stock.StockCurrentPrice;
        int current = GetHolding(stock);
        SetHolding(stock, current + amount);
        Debug.Log($"[{investorName}] {stock.StockName} {amount}주 매수 (총 {current + amount}주)");
    }

    void Sell(Stock stock)
    {
        int current = GetHolding(stock);
        if (current <= 0) return;

        gold += current * stock.StockCurrentPrice;
        SetHolding(stock, 0);
        Debug.Log($"[{investorName}] {stock.StockName} {current}주 매도");
    }

    void OnDestroy()
    {
        worker?.Dispose();
    }
}