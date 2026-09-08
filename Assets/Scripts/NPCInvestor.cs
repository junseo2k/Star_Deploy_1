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
    public int maxBuyCount = 3;          // 하루에 매수할 종목 수 상한 — 예측이 높은 순으로 이만큼만 산다

    [Header("상태")]
    public int gold = 60000; // 현재 보유 골드 (주가 4배 스케일에 맞춰 상향)

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
    private const int FEATURES_PER_DAY = 13; // 하루치 입력 feature 개수 (정규화 가격 1 + StockDailyFeatures 12) — model.py의 N_FEATURES와 동일

    private Worker worker; // 모델 추론 실행기
    private bool predictionDisabled; // 모델 입력이 안 맞아 예측을 포기한 상태 (경고를 한 번만 띄우기 위한 플래그)
    private readonly List<(Stock stock, float prediction, float confidence)> dailyPredictions = new(); // 이번 턴에 수집한 주식별 예측값(기댓값)과 확신도(0~1)
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
        if (predictionDisabled) return; // 모델이 안 맞아서 예측을 포기한 상태 — 게임 진행은 막지 않는다

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
        //  volatilityType, quarterDayIndex/QUARTER_LENGTH, isQuarterEndToday, hasActiveBusiness,
        //  personality, fairPriceRatio]
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
            inputData[baseIdx + 8] = (float)f.quarterDayIndex / GameBalance.QUARTER_LENGTH;
            inputData[baseIdx + 9] = f.isQuarterEndToday ? 1f : 0f;
            inputData[baseIdx + 10] = f.hasActiveBusiness ? 1f : 0f;
            inputData[baseIdx + 11] = f.personality;
            inputData[baseIdx + 12] = f.fairPriceRatio;
            i++;
        }

        Tensor<float> inputTensor = new(new TensorShape(1, WINDOW_SIZE, FEATURES_PER_DAY), inputData);
        try
        {
            worker.Schedule(inputTensor);
            Tensor<float> output = worker.PeekOutput("output_0") as Tensor<float>;
            float[] result = output.DownloadToArray(); // [0]=기댓값, [1]=log(분산) — model.py의 2-head 출력과 동일 순서

            float predictedChange = result[0];
            float logVar = result[1];
            float stdDev = Mathf.Exp(0.5f * logVar); // 분산 = exp(log_var), 표준편차 = 분산의 제곱근
            float confidence = Mathf.Clamp01(1f / (1f + stdDev)); // 표준편차가 클수록(불확실할수록) 0에 가까워지는 확신도

            Debug.Log($"[{investorName}] {stock.StockName} 예측: {predictedChange:F2}% (표준편차 {stdDev:F2}, 확신도 {confidence:F2})");
            dailyPredictions.Add((stock, predictedChange, confidence));
        }
        catch (System.Exception e)
        {
            // 여기로 오는 가장 흔한 이유는 feature 개수가 바뀐 채로 모델을 아직 재학습하지 않은 것이다.
            // (예: FEATURES_PER_DAY를 12→13으로 늘렸는데 .onnx는 아직 12개짜리)
            //
            // 이때 그냥 예외를 터뜨리면 학습 데이터를 모으려고 자동 시뮬레이션을 돌릴 수조차 없다.
            // CSV를 뽑으려면 게임이 돌아야 하는데 NPC가 게임을 멈춰버리는 교착이 생기기 때문이다.
            // 그래서 예측만 조용히 끄고 게임은 계속 진행시킨다 — 경고는 딱 한 번만 남긴다
            predictionDisabled = true;
            dailyPredictions.Clear();
            Debug.LogWarning(
                $"[{investorName}] 예측을 중단합니다. 이 NPC는 이번 플레이 동안 매매하지 않습니다.\n" +
                $"    원인: 모델 입력 형태가 맞지 않습니다 (코드가 보내는 값: {WINDOW_SIZE}일 × {FEATURES_PER_DAY}개).\n" +
                $"    feature를 추가/삭제했다면 CSV를 새로 뽑아 Model.py로 재학습한 뒤 stock_model.onnx를 다시 임포트하세요.\n" +
                $"    (학습 데이터 수집은 이 상태로도 정상 진행됩니다)\n" +
                $"    원본 오류: {e.Message}", this);
        }
        finally
        {
            inputTensor.Dispose(); // 예측 도중 예외가 나도 텐서가 새지 않도록 보장
        }
    }

    // 모든 주식 예측이 끝난 후 호출 — 예측 평균 대비 상대값으로 매수/매도 결정
    // 순서에 의미가 있다: ① 매도로 현금을 먼저 확보하고 ② 오늘 쓸 예산을 확정한 뒤 ③ 상위 몇 개만 매수한다
    //
    // 예전에는 종목 배열 순서대로 돌면서 매번 "남은 골드의 investRatio만큼"을 썼다. 그러면 앞쪽 종목이
    // 돈을 거의 다 가져가고(상위 3개가 매수액의 65%) 뒤쪽은 한 주도 못 사는 일이 생겼다.
    // 예측이 좋아서가 아니라 인스펙터 배열 순서(사실상 알파벳순) 때문이라 완전히 무의미한 편향이었다
    public void DecideAfterAllPredictions()
    {
        if (dailyPredictions.Count == 0) return;

        float avg = 0f;
        foreach (var p in dailyPredictions) avg += p.prediction;
        avg /= dailyPredictions.Count;

        Debug.Log($"[{investorName}] 오늘 예측 평균: {avg:F2}%");

        // ① 매도 먼저 — 여기서 나온 현금이 오늘 매수 예산에 포함되도록
        foreach (var (stock, prediction, _) in dailyPredictions)
        {
            if (prediction - avg < sellThreshold)
                Sell(stock);
        }

        // ② 매수 후보를 모아 예측이 높은 순으로 정렬 — 이제 "유망한 순"이 곧 우선순위다
        List<(Stock stock, float prediction, float confidence)> candidates = new();
        foreach (var p in dailyPredictions)
        {
            if (p.prediction - avg > buyThreshold)
                candidates.Add(p);
        }
        candidates.Sort((a, b) => b.prediction.CompareTo(a.prediction)); // 내림차순

        int buyCount = Mathf.Min(maxBuyCount, candidates.Count);
        if (buyCount > 0)
        {
            // ③ 오늘 예산을 여기서 딱 한 번 확정한다 — 루프 안에서 gold가 줄어도 배분이 흔들리지 않는다
            int todayBudget = Mathf.FloorToInt(gold * investRatio);
            int perStock = todayBudget / buyCount;

            for (int i = 0; i < buyCount; i++)
            {
                // 확신이 없는 날은 배정액을 그만큼 덜 쓴다 — 안 쓴 돈은 현금으로 남는다
                // (분기말처럼 크게 흔들리는 날 무모하게 지르지 않게 하는 안전장치)
                int budget = Mathf.FloorToInt(perStock * candidates[i].confidence);
                Buy(candidates[i].stock, budget);
            }
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

    // 배정받은 예산 안에서 최대한 매수한다
    // 예산 계산은 DecideAfterAllPredictions가 미리 해서 넘겨준다 — 여기서 gold를 보고 다시 계산하면
    // 종목을 살 때마다 예산이 줄어드는 예전 문제가 그대로 돌아온다
    void Buy(Stock stock, int budget)
    {
        int amount = budget / stock.StockCurrentPrice;
        if (amount <= 0) return; // 예산이 1주 값도 안 되면 산다고 하지 않는다

        int cost = amount * stock.StockCurrentPrice;
        if (cost > gold) return; // 안전장치 — 예산 계산이 어긋나도 골드가 마이너스로 가지 않게

        gold -= cost;
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