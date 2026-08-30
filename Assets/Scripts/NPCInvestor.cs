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

    [Header("자산 현황")]
    public int totalAssets; // 보유 골드 + 보유 주식 평가금액 합계 — 매일 거래 끝나고 자동 갱신 (읽기 전용, 직접 수정 X)

    // Inspector에서 보이도록 직렬화
    [System.Serializable]
    public class StockHolding
    {
        public Stock stock;  // 보유 중인 주식 에셋
        public int amount;   // 보유 수량
    }
    public List<StockHolding> holdings = new(); // 현재 보유 주식 목록

    private const int WINDOW_SIZE = 10;      // 예측에 사용하는 과거 데이터 윈도우 크기 (일) — model.py의 window_size와 동일
    private const int FEATURES_PER_DAY = 12; // 하루치 입력 feature 개수 (정규화 가격 1 + StockDailyFeatures 11) — model.py의 N_FEATURES와 동일

    private Worker worker; // 모델 추론 실행기
    private readonly List<(Stock stock, float prediction)> dailyPredictions = new(); // 이번 턴에 수집한 주식별 예측값
    private readonly Dictionary<Stock, Queue<(float price, StockDailyFeatures features)>> priceHistories = new(); // 주식별 입력 이력 큐

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
            holdings.Add(new() { stock = stock, amount = amount });
        }
    }

    // 매일 하루가 지날 때 호출 — 가격+상태값 이력을 누적하고 윈도우가 차면 예측 실행
    // GameManager.RunNpcInvestors()가 GetDailyFeatures()로 만든 값을 그대로 넘겨준다 (CSV 학습 데이터와 동일한 계산)
    public void OnDayPassed(Stock stock, StockDailyFeatures features)
    {
        if (!priceHistories.TryGetValue(stock, out Queue<(float price, StockDailyFeatures features)> history))
        {
            history = new();
            priceHistories[stock] = history;
        }

        history.Enqueue((stock.StockCurrentPrice, features));

        if (history.Count > WINDOW_SIZE)
            history.Dequeue();

        if (history.Count == WINDOW_SIZE)
            MakeDecision(stock, history);
    }

    // 가격+상태값 이력을 정규화한 뒤 LSTM 모델로 다음 날 변동률을 예측하고 결과를 저장
    void MakeDecision(Stock stock, Queue<(float price, StockDailyFeatures features)> history)
    {
        float minPrice = float.MaxValue;
        float maxPrice = float.MinValue;
        foreach (var day in history)
        {
            if (day.price < minPrice) minPrice = day.price;
            if (day.price > maxPrice) maxPrice = day.price;
        }

        if (maxPrice == minPrice) return; // 가격 변동이 없으면 정규화 불가, 스킵

        // 입력 텐서 구성 — model.py의 FEATURE_COLUMNS 순서·스케일링과 한 글자도 다르면 안 된다
        // [가격 정규화, priceChangePct, newsCategory, isForeshadowPending,
        //  daysSinceLastSmallNews/10, daysSinceLastBigNews/50, eraRatePct,
        //  volatilityType, quarterDayIndex/10, isQuarterEndToday, hasActiveBusiness, personality]
        float[] inputData = new float[WINDOW_SIZE * FEATURES_PER_DAY];
        int i = 0;
        foreach (var day in history)
        {
            StockDailyFeatures f = day.features;
            int baseIdx = i * FEATURES_PER_DAY;

            inputData[baseIdx + 0] = (day.price - minPrice) / (maxPrice - minPrice);
            inputData[baseIdx + 1] = f.priceChangePct;
            inputData[baseIdx + 2] = f.newsCategory;
            inputData[baseIdx + 3] = f.isForeshadowPending ? 1f : 0f;
            inputData[baseIdx + 4] = f.daysSinceLastSmallNews / 10f;
            inputData[baseIdx + 5] = f.daysSinceLastBigNews / 50f;
            inputData[baseIdx + 6] = f.eraRatePct;
            inputData[baseIdx + 7] = f.volatilityType;
            inputData[baseIdx + 8] = f.quarterDayIndex / 10f;
            inputData[baseIdx + 9] = f.isQuarterEndToday ? 1f : 0f;
            inputData[baseIdx + 10] = f.hasActiveBusiness ? 1f : 0f;
            inputData[baseIdx + 11] = f.personality;
            i++;
        }

        Tensor<float> inputTensor = new(new TensorShape(1, WINDOW_SIZE, FEATURES_PER_DAY), inputData);
        try
        {
            worker.Schedule(inputTensor);
            Tensor<float> output = worker.PeekOutput("output_0") as Tensor<float>;
            float predictedChange = output.DownloadToArray()[0];

            Debug.Log($"[{investorName}] {stock.StockName} 예측: {predictedChange:F2}%");
            dailyPredictions.Add((stock, predictedChange));
        }
        finally
        {
            inputTensor.Dispose(); // 예측 도중 예외가 나도 텐서가 새지 않도록 보장
        }
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
        UpdateTotalAssets();
    }

    // 보유 골드 + 보유 주식 전부를 현재가로 환산한 값을 합쳐서 totalAssets에 반영 — 인스펙터 확인용
    void UpdateTotalAssets()
    {
        int stockValue = 0;
        foreach (StockHolding h in holdings)
            stockValue += h.amount * h.stock.StockCurrentPrice;

        totalAssets = gold + stockValue;
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