using TMPro;
using Unity.VisualScripting;
using XCharts.Runtime; // 상단에 추가
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Analytics;
// 주식 상세 정보 패널을 관리하는 클래스
// 기본 정보·서비스·중요 지표 탭 전환과 주식 데이터 표시를 담당
public class DetailPanel : MonoBehaviour
{
    public GameObject StockWithImagePrefab;   // 이미지 포함 주식 아이템 프리팹
    public Image StockImage;                 // 주식 아이콘 이미지
    public TextMeshProUGUI StockName;         // 주식 이름 표시 텍스트
    public TextMeshProUGUI StockPrice;        // 현재 주식 가격 표시 텍스트
    public TextMeshProUGUI StockChangePrice;  // 이전 가격 대비 변동률(%) 표시 텍스트
    public TextMeshProUGUI StockDescription;  // 주식 요약 설명 표시 텍스트
    public Transform DetailInfoPanel;         // 기본 정보 탭 패널
    public Transform ServicePanel;            // 서비스 탭 패널
    public Transform ImportantPanel;          // 중요 지표 탭 패널
    public TextMeshProUGUI StockDetailDescription; // 기업 상세 설명 표시 텍스트
    public TextMeshProUGUI revenuePrice;      // 매출액 표시 텍스트
    public TextMeshProUGUI ProfitPrice;       // 영업이익 표시 텍스트
    public TextMeshProUGUI MarketCapPrice;    // 시가총액 표시 텍스트
    public TextMeshProUGUI TotalSharePrice;   // 총 발행 주식 수 표시 텍스트               
    public Stock currentstock;               // 현재 상세 패널에 표시 중인 주식 데이터
    public Transform DetailwithPanel;        // 상세 패널 전체 루트 (구매/판매 패널 전환 시 비활성화)
    public int maxmount;                     // 구매 또는 판매 가능한 최대 수량
    public OrderPanel orderPanel;           // 구매/판매 수량 확인 패널 참조
    public SidebarTabManager sidebarTabManager; // 인스펙터에서 연결
    public GameObject marketListPanel; // TabManager가 붙어있는 주식 목록 화면 최상위 오브젝트
    public TextMeshProUGUI MarketLabelText; // 주가 변동 이유 라벨
    public Transform VolumeGraphPanel; // 막대 그래프가 들어갈 패널 (인스펙터 연결)
    public GameObject BarPrefab;       // 막대 하나짜리 프리팹 (인스펙터 연결)
    public Transform GraphPanel; // 그래프 탭 패널 (인스펙터 연결)
    public TextMeshProUGUI[] xAxisLabels;  // X축 날짜 텍스트 배열 (인스펙터에서 10개 연결)    
    public BarChart barChart; // 인스펙터에서 연결
    public LineChart priceChart; // Inspector에서 LineChart 오브젝트 연결



    void Start()
    {
        HideAllPanels(); // 시작 시 모든 탭 패널을 숨김
    }

    void Update()
    {
        
    }

    // 주식 데이터를 받아 상세 패널 UI 텍스트를 갱신
    public void ShowDetail(Stock stock)
{
    transform.SetAsLastSibling();
    currentstock = stock;
    StockName.text = stock.StockName;
    StockImage.sprite = stock.StockIcon;

    int startPrice = stock.StockCurrentPrice;
    float impactRatio = stock.TotalShare > 0 ? 1f / stock.TotalShare : 0f;
    int endPrice = Mathf.Max(1, Mathf.RoundToInt(startPrice * (1 + impactRatio)));
    float execPrice = (startPrice + endPrice) / 2f;
    StockPrice.text = $"{(int)execPrice} G";

    StockDescription.text = stock.StockDescription;
    StockDetailDescription.text = stock.StockDetailDescription;
    revenuePrice.text = $"{stock.revenue} G";
    ProfitPrice.text = $"{stock.OperatingProfit} G";
    MarketCapPrice.text = $"{stock.marketCap} G";
    TotalSharePrice.text = $"{stock.TotalShare} stock";

    int change = Mathf.RoundToInt(stock.ChangePercent);
    StockChangePrice.text = change > 0 ? $"+{change}%" : $"{change}%";

    MarketLabelText.text = GetMarketLabel(stock);

    if (stock.changePctHistory.Count == 0) return;

    // 최대 변동폭 기준 정규화 (Y축용)

    // Y축 레이블
    // XCharts 데이터 초기화 후 입력
    barChart.RemoveChartComponent<XAxis>();
    barChart.AddChartComponent<XAxis>();
    barChart.ClearData();
    var title = barChart.EnsureChartComponent<Title>();
    title.text = stock.StockName;
    var tooltip = barChart.GetChartComponent<Tooltip>();
    if (tooltip != null)
        tooltip.numericFormatter = "f1"; // 소수점 1자리
        tooltip.itemFormatter = "{a} - {b}: {c}%"; // 주식이름 - 날짜 형태로 표시
    var yAxis = barChart.GetChartComponent<YAxis>();
    if (yAxis != null)
        yAxis.axisLabel.numericFormatter = "f1";

    int today = GameManager.Instance.currentDay;
    int startDay = today - stock.changePctHistory.Count + 1;
    var barSerie = barChart.series.Count > 0 ? barChart.series[0] : barChart.AddSerie<Bar>();
    barSerie.serieName = stock.StockName;
    for (int i = 0; i < stock.changePctHistory.Count; i++)
    {
        float pct = stock.changePctHistory[i];
        barChart.AddXAxisData($"{startDay + i}일");
        SerieData serieData = barChart.AddData(0, pct);

    // itemStyle 컴포넌트를 실제로 데이터에 부착해야 색이 반영됨
        var itemStyle = serieData.EnsureComponent<ItemStyle>();
        itemStyle.show = true;
        itemStyle.color = pct >= 0f
            ? new Color(1f, 0.27f, 0.27f)   // 상승 빨강
            : new Color(0.27f, 0.53f, 1f);  // 하락 파랑
    }
}

    // 모든 탭 패널을 비활성화 (탭 전환 전 초기화에 사용)
    public void HideAllPanels()
    {
        DetailInfoPanel.gameObject.SetActive(false);
        ServicePanel.gameObject.SetActive(false);
        ImportantPanel.gameObject.SetActive(false);
        GraphPanel.gameObject.SetActive(false); // 추가
    }

    // 기본 정보 탭 버튼 클릭 시 기본 정보 패널만 활성화
    public void OnDetailInfoClick()
    {
        HideAllPanels();
        DetailInfoPanel.gameObject.SetActive(true);
    }

    // 서비스 탭 버튼 클릭 시 서비스 패널만 활성화
    public void OnServiceClick()
    {
        HideAllPanels();
        ServicePanel.gameObject.SetActive(true);
        foreach (Transform child in ServicePanel)
        {
            Destroy(child.gameObject);
        }
        foreach (Stock.ProductInfo product in currentstock.products)
        {
            GameObject ServicePrefab = Instantiate(StockWithImagePrefab, ServicePanel);
            Image ServiceImage = ServicePrefab.GetComponentInChildren<Image>();
            ServiceImage.sprite = product.productIcon;
            TextMeshProUGUI ServiceText = ServicePrefab.GetComponentInChildren<TextMeshProUGUI>();
            ServiceText.text = product.productName;
        }

        
    }

    // 중요 지표 탭 버튼 클릭 시 중요 지표 패널만 활성화
    public void OnImportantClick()
    {
        HideAllPanels();
        ImportantPanel.gameObject.SetActive(true);
        ImportantPanel.gameObject.SetActive(true);
    }
    // 닫기 버튼 클릭 시 서비스 프리팹 정리 후 상세 패널 닫고 전체 목록 패널로 복귀
    public void OnExitButton()
    {
        foreach (Transform child in ServicePanel)
        {
            Destroy(child.gameObject);
        }
        UIManager.instance.ReturnToStock(DetailwithPanel.gameObject);
    }
    // 구매 버튼 클릭 시 상세 패널 닫고 구매 패널 열기
    // 현재 주식에 해당하는 StockControl을 찾아 OrderPanel에 연결하고 최대 구매 수량 계산
    // DetailPanel.cs — OnBuyButtonClick() 교체
    public void OnBuyButtonClick()
    {
        DetailwithPanel.gameObject.SetActive(false);
        UIManager.instance.buyPanel.gameObject.SetActive(true);
        UIManager.instance.buyPanel.transform.SetAsLastSibling();
        StockControl sc = StockControl.Find(currentstock);
        if (sc != null)
            orderPanel.stockcontrol = sc;

    // 슬리피지 때문에 수량이 늘수록 단가도 오르므로, 한 주씩 늘려가며 실제 감당 가능한 최대치를 찾음
        maxmount = 0;
        while (true)
        {
            orderPanel.CalculateTrade(maxmount + 1, true, out _, out int cost);
            if (cost > GameManager.Instance.currentgold) break;
            maxmount++;
        }

        orderPanel.currentText.text = $"Maximum: {maxmount}";
        orderPanel.StockNum = 1;
        orderPanel.RefreshPurchaseText();
        orderPanel.isBuyMode = true;
}
    // 판매 버튼 클릭 시 상세 패널 닫고 판매 패널 열기
    // 현재 주식에 해당하는 StockControl을 찾아 OrderPanel에 연결하고 최대 판매 수량(보유량) 계산
    public void OnSellButtonClick()
    {
        DetailwithPanel.gameObject.SetActive(false);
        UIManager.instance.buyPanel.gameObject.SetActive(true);
        StockControl sc = StockControl.Find(currentstock);
        if (sc != null)
        {
            orderPanel.stockcontrol = sc; // 찾은 StockControl 넘기기
            maxmount = sc.ownedAmount;
        }
        orderPanel.currentText.text = $"Maximum: {maxmount}";
        orderPanel.StockNum = 1;
        orderPanel.PurchaseText.text = $"OwnedStock : {orderPanel.StockNum = 1}, \n {currentstock.StockCurrentPrice} gold.";
        orderPanel.isBuyMode = false; // 판매 모드로 설정
    }
    // DetailPanel.cs — 새 함수 추가
    string GetMarketLabel(Stock stock)
    {
        GameManager gm = GameManager.Instance;

        // 1순위: 대형 뉴스 활성화 중
        bool hasBigNews = gm.activeNewsList.Exists(a =>
            a.caseData.newsType == Case.NewsType.BigTriggerNews);
        if (hasBigNews)
            return "대형 사건의 여파입니다.";

        // 2순위: 시대 보정 중
        if (gm.currentEra != null)
        {
            bool isBoosted = System.Array.Exists(gm.currentEra.boostedIndustries, t => t == stock.IndustryTag);
            bool isWeakened = System.Array.Exists(gm.currentEra.weakenedIndustries, t => t == stock.IndustryTag);
            if (isBoosted) return "시대의 흐름이 밀어올리고 있습니다.";
            if (isWeakened) return "시대의 흐름이 짓누르고 있습니다.";
        }

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
public void OnGraphClick()
{
    HideAllPanels();
    GraphPanel.gameObject.SetActive(true);
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
            ? new Color(1f, 0.27f, 0.27f)   // 상승 빨강
            : new Color(0.27f, 0.53f, 1f);  // 하락 파랑
        serie.lineStyle.color = lineColor;   // 선 색
        serie.itemStyle.color = lineColor;   // 점(마커) 색 — 범례 색도 여기서 따라감

        for (int i = 0; i < priceHistory.Count; i++)
        {
            priceChart.AddXAxisData(i.ToString());
            serie.AddData(priceHistory[i]);
        }

        priceChart.RefreshChart(); // 변경사항 즉시 반영
    }

}
