using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XCharts.Runtime;

// 주식 상세 정보 패널을 관리하는 클래스
// 기본 정보·서비스·중요 지표·그래프 탭 전환과 주식 데이터 표시를 담당
public class DetailPanel : MonoBehaviour
{
    [Header("프리팹")]
    public GameObject StockWithImagePrefab; // 이미지 포함 주식 아이템 프리팹

    [Header("기본 정보 표시")]
    public Image StockImage;                       // 주식 아이콘 이미지
    public TextMeshProUGUI StockName;              // 주식 이름 표시 텍스트
    public TextMeshProUGUI StockPrice;             // 현재 주식 가격 표시 텍스트
    public TextMeshProUGUI StockChangePrice;       // 이전 가격 대비 변동률(%) 표시 텍스트
    public TextMeshProUGUI StockDescription;       // 주식 요약 설명 표시 텍스트
    public TextMeshProUGUI StockDetailDescription; // 기업 상세 설명 표시 텍스트
    public TextMeshProUGUI revenuePrice;           // 수입 표시 텍스트
    public TextMeshProUGUI AssetPrice;             // 회사 금고 표시 텍스트
    public TextMeshProUGUI MarketCapPrice;         // 시가총액 표시 텍스트
    public TextMeshProUGUI TotalSharePrice;        // 총 발행 주식 수 표시 텍스트
    public TextMeshProUGUI MarketLabelText;        // 주가 변동 이유 라벨

    [Header("탭 패널")]
    public Transform DetailInfoPanel;  // 기본 정보 탭 패널
    public Transform ServicePanel;     // 서비스 탭 패널
    public Transform ImportantPanel;   // 중요 지표 탭 패널
    public Transform GraphPanel;       // 그래프 탭 패널
    public Transform DetailwithPanel;  // 상세 패널 전체 루트 (구매/판매 패널 전환 시 비활성화)

    [Header("그래프")]
    public LineChart lineChart;  // 주가(Serie 0) + 거래량(Serie 1) 복합 차트
    public LineChart priceChart; // DrawPriceChart 전용 단일 차트

    [Header("연결")]
    public OrderPanel orderPanel;  // 구매/판매 수량 확인 패널 참조
    public Stock currentstock;     // 현재 상세 패널에 표시 중인 주식 데이터

    private const float PRICE_AXIS_PADDING_RATIO = 0.1f; // Y축 위아래 여백 비율 (작은 변동도 잘 보이도록)

    void Start()
    {
        HideAllPanels(); // 시작 시 모든 탭 패널을 숨김
    }

    // 주식 데이터를 받아 상세 패널 UI 텍스트와 그래프를 갱신
    public void ShowDetail(Stock stock)
    {
        transform.SetAsLastSibling();
        currentstock = stock;

        StockName.text = stock.StockName;
        StockImage.sprite = stock.StockIcon;
        StockPrice.text = $"{stock.StockCurrentPrice} G";
        StockDescription.text = stock.StockDescription;
        StockDetailDescription.text = stock.StockDetailDescription;
        revenuePrice.text = UIFormat.Gold(stock.income);
        // 금고 칸은 나중에 추가된 항목이라, 인스펙터 연결이 빠져 있어도 패널 전체가 죽지 않도록 확인 후 표시
        if (AssetPrice != null) AssetPrice.text = UIFormat.Gold(stock.runtimeAsset);
        MarketCapPrice.text = UIFormat.Gold(stock.marketCap);
        TotalSharePrice.text = $"{stock.TotalShare} stock";
        StockChangePrice.text = UIFormat.SignedPercentPlain(Mathf.RoundToInt(stock.ChangePercent));
        MarketLabelText.text = GetMarketLabel(stock);

        RefreshHistoryChart(stock);
    }

    // 최근 주가·거래량 히스토리를 복합 차트에 반영
    void RefreshHistoryChart(Stock stock)
    {
        if (lineChart == null || stock.priceHistory.Count == 0) return;

        lineChart.ClearData();

        Title chartTitle = lineChart.GetChartComponent<Title>();
        if (chartTitle != null)
            chartTitle.text = stock.StockName;

        ApplyPriceAxisRange(stock.priceHistory);

        // X축 두 개(위: 주가용, 아래: 거래량용) 모두에 같은 날짜 라벨 입력
        int startDay = GameManager.Instance.currentDay - stock.priceHistory.Count + 1;
        for (int i = 0; i < stock.priceHistory.Count; i++)
        {
            string label = $"{startDay + i}일";
            lineChart.AddXAxisData(label, 0); // Grid 0 (주가) X축
            lineChart.AddXAxisData(label, 1); // Grid 1 (거래량) X축
        }

        // 데이터 입력 — Serie 0(Line)에 주가, Serie 1(Bar)에 거래량
        foreach (int price in stock.priceHistory)
            lineChart.AddData(0, price);

        foreach (float volume in stock.volumeHistory)
            lineChart.AddData(1, volume);
    }

    // 주가 Y축(인덱스 0) 범위를 실제 최소·최대에 여백을 더한 값으로 재계산
    void ApplyPriceAxisRange(List<int> priceHistory)
    {
        YAxis priceYAxis = lineChart.GetChartComponent<YAxis>(0);
        if (priceYAxis == null) return;

        int minPrice = int.MaxValue;
        int maxPrice = int.MinValue;
        foreach (int p in priceHistory)
        {
            if (p < minPrice) minPrice = p;
            if (p > maxPrice) maxPrice = p;
        }

        int padding = Mathf.Max(1, Mathf.RoundToInt((maxPrice - minPrice) * PRICE_AXIS_PADDING_RATIO));
        priceYAxis.min = minPrice - padding;
        priceYAxis.max = maxPrice + padding;
    }

    // ── 탭 전환 ───────────────────────────────────────

    // 모든 탭 패널을 비활성화 (탭 전환 전 초기화에 사용)
    public void HideAllPanels()
    {
        DetailInfoPanel.gameObject.SetActive(false);
        ServicePanel.gameObject.SetActive(false);
        ImportantPanel.gameObject.SetActive(false);
        GraphPanel.gameObject.SetActive(false);
    }

    // 기본 정보 탭 버튼 클릭 시 기본 정보 패널만 활성화
    public void OnDetailInfoClick()
    {
        HideAllPanels();
        DetailInfoPanel.gameObject.SetActive(true);
    }

    // 서비스 탭 버튼 클릭 시 서비스 패널만 활성화하고 상품 목록을 다시 생성
    public void OnServiceClick()
    {
        HideAllPanels();
        ServicePanel.gameObject.SetActive(true);
        ClearServiceItems();

        foreach (Stock.ProductInfo product in currentstock.products)
        {
            GameObject item = Instantiate(StockWithImagePrefab, ServicePanel);
            item.GetComponentInChildren<Image>().sprite = product.productIcon;
            item.GetComponentInChildren<TextMeshProUGUI>().text = product.productName;
        }
    }

    // 중요 지표 탭 버튼 클릭 시 중요 지표 패널만 활성화
    public void OnImportantClick()
    {
        HideAllPanels();
        ImportantPanel.gameObject.SetActive(true);
    }

    // 그래프 탭 버튼 클릭 시 그래프 패널만 활성화
    public void OnGraphClick()
    {
        HideAllPanels();
        GraphPanel.gameObject.SetActive(true);
    }

    // 닫기 버튼 클릭 시 서비스 프리팹 정리 후 상세 패널 닫고 종목 목록 패널로 복귀
    public void OnExitButton()
    {
        ClearServiceItems();
        UIManager.instance.ReturnToStock(DetailwithPanel.gameObject);
    }

    // 서비스 탭에 생성해둔 프리팹 정리
    void ClearServiceItems()
    {
        foreach (Transform child in ServicePanel)
            Destroy(child.gameObject);
    }

    // ── 매매 ─────────────────────────────────────────

    // 구매 버튼 클릭 시 상세 패널 닫고 구매 패널 열기
    public void OnBuyButtonClick()
    {
        OpenOrderPanel(true, CalculateMaxBuyAmount());
    }

    // 판매 버튼 클릭 시 상세 패널 닫고 판매 패널 열기 (최대 수량 = 보유량)
    public void OnSellButtonClick()
    {
        OpenOrderPanel(false, Portfolio.GetAmount(currentstock));
    }

    // 구매/판매 패널을 열고 거래 대상·모드·최대 수량을 전달
    void OpenOrderPanel(bool isBuyMode, int maxAmount)
    {
        DetailwithPanel.gameObject.SetActive(false);
        UIManager.instance.buyPanel.gameObject.SetActive(true);
        UIManager.instance.buyPanel.transform.SetAsLastSibling();

        orderPanel.Setup(currentstock, isBuyMode, maxAmount);
    }

    // 슬리피지 때문에 수량이 늘수록 단가도 오르므로, 한 주씩 늘려가며 실제 감당 가능한 최대치를 찾음
    // 주가 하한이 1G이라 보유 골드를 넘는 수량은 나올 수 없어 루프는 반드시 끝난다
    int CalculateMaxBuyAmount()
    {
        int gold = GameManager.Instance.currentgold;
        orderPanel.targetStock = currentstock; // CalculateTrade가 대상 주식을 참조하므로 먼저 지정

        int amount = 0;
        while (amount < gold)
        {
            orderPanel.CalculateTrade(amount + 1, true, out _, out int cost);
            if (cost > gold) break;
            amount++;
        }
        return amount;
    }

    // ── 표시 문구 ─────────────────────────────────────

    // 현재 주가 흐름의 이유를 우선순위에 따라 한 줄로 설명
    string GetMarketLabel(Stock stock)
    {
        GameManager gm = GameManager.Instance;

        // 1순위: 대형 뉴스 활성화 중
        if (gm.activeNewsList.Exists(a => a.caseData.newsType == Case.NewsType.BigTriggerNews))
            return "대형 사건의 여파입니다.";

        // 2순위: 시대 보정 중
        if (gm.IsBoostedByEra(stock)) return "시대의 흐름이 밀어올리고 있습니다.";
        if (gm.IsWeakenedByEra(stock)) return "시대의 흐름이 짓누르고 있습니다.";

        // 3순위: 소형 뉴스 영향 중
        bool hasSmallNews = gm.activeNewsList.Exists(a =>
            a.caseData.affectedCase != null &&
            System.Array.Exists(a.caseData.affectedCase, s => s == stock));
        if (hasSmallNews)
            return "시장에 소문이 돌고 있습니다.";

        // 4순위: 등락률 기준
        float change = stock.ChangePercent;
        if (change >= 2f) return "기관이 담고 있습니다.";
        if (change <= -2f) return "개미들이 던지고 있습니다.";
        return "조용한 장세입니다.";
    }

    // 주가 그래프를 그리고, 오늘 등락에 따라 선 색을 정한다
    public void DrawPriceChart(List<double> priceHistory, float changePercent)
    {
        priceChart.ClearData(); // 이전 데이터 초기화

        Serie serie = priceChart.series.Count > 0
            ? priceChart.series[0]
            : priceChart.AddSerie<Line>();

        // 상승(0 이상) → 빨강, 하락 → 파랑
        Color lineColor = changePercent >= 0f
            ? new Color(1f, 0.27f, 0.27f)
            : new Color(0.27f, 0.53f, 1f);
        serie.lineStyle.color = lineColor; // 선 색
        serie.itemStyle.color = lineColor; // 점(마커) 색 — 범례 색도 여기서 따라감

        for (int i = 0; i < priceHistory.Count; i++)
        {
            priceChart.AddXAxisData(i.ToString());
            serie.AddData(priceHistory[i]);
        }

        priceChart.RefreshChart(); // 변경사항 즉시 반영
    }
}
