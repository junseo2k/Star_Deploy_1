using UnityEngine;
using Unity.InferenceEngine;
using System.Collections.Generic;

// LSTM 모델을 사용해 주식 변동을 예측하고 자동 매매를 수행하는 NPC 투자자
public class NPCInvestor : MonoBehaviour
{
    [Header("모델 설정")]
    public ModelAsset modelAsset;   // Unity Inference Engine에 로드할 ONNX 모델 에셋
    public string investorName;     // 디버그 로그에 표시되는 NPC 이름

    [Header("성향 설정")]
    public float buyThreshold = 0.02f;   // 예측값이 평균보다 이 값 이상 높으면 매수
    public float sellThreshold = -0.02f; // 예측값이 평균보다 이 값 이하면 매도
    public float investRatio = 0.5f;     // 보유 골드 중 매수에 사용하는 비율

    [Header("상태")]
    public int gold = 5000; // 현재 보유 골드

    // Inspector에서 보이도록 직렬화
    [System.Serializable]
    public class StockHolding
    {
        public Stock stock;  // 보유 중인 주식 에셋
        public int amount;   // 보유 수량
    }
    public List<StockHolding> holdings = new List<StockHolding>(); // 현재 보유 주식 목록

    private Worker worker;                                                               // 모델 추론 실행기
    private const int WINDOW_SIZE = 10;                                                  // 예측에 사용하는 과거 데이터 윈도우 크기 (일)
    private Queue<float[]> priceHistory = new Queue<float[]>();                          // (미사용) 단일 주식 가격 이력
    private List<(Stock stock, float prediction)> dailyPredictions = new List<(Stock, float)>(); // 이번 턴에 수집한 주식별 예측값

    // 모델 로드 및 추론 워커 초기화
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

// 매일 하루가 지날 때 호출 — 가격 이력을 누적하고 윈도우가 차면 예측 실행
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

// 가격 이력을 정규화한 뒤 LSTM 모델로 다음 날 변동률을 예측하고 결과를 저장
void MakeDecision(Stock stock, Queue<float[]> history)
{
    float minPrice = float.MaxValue;
    float maxPrice = float.MinValue;
    foreach (var day in history)
    {
        if (day[0] < minPrice) minPrice = day[0];
        if (day[0] > maxPrice) maxPrice = day[0];
    }

    if (maxPrice == minPrice) return; // 가격 변동이 없으면 정규화 불가, 스킵

    // 입력 텐서 구성: [가격 정규화, 변동률, 뉴스 영향 여부] × WINDOW_SIZE
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

    // 모든 주식 예측이 끝난 후 호출 — 예측 평균 대비 상대값으로 매수/매도 결정
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

    // 보유 골드의 investRatio 비율로 해당 주식을 최대한 매수
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

    // 해당 주식 보유량 전량 매도
    void Sell(Stock stock)
    {
        int current = GetHolding(stock);
        if (current <= 0) return;

        gold += current * stock.StockCurrentPrice;
        SetHolding(stock, 0);
        Debug.Log($"[{investorName}] {stock.StockName} {current}주 매도");
    }

    // 오브젝트 파괴 시 추론 워커 해제
    void OnDestroy()
    {
        worker?.Dispose();
    }
}
