using TMPro;
using UnityEngine;

// 주식 상세 정보 패널을 관리하는 클래스
// 기본 정보·서비스·중요 지표·최근 이슈 탭 전환과 주식 데이터 표시를 담당
public class DetailPanel : MonoBehaviour
{
    public GameObject StockPrefab;            // 주식 아이템 기본 프리팹
    public GameObject StockWithImagePrefab;   // 이미지 포함 주식 아이템 프리팹
    public Sprite StockImage;                 // 주식 아이콘 이미지
    public TextMeshProUGUI StockName;         // 주식 이름 표시 텍스트
    public TextMeshProUGUI StockPrice;        // 현재 주식 가격 표시 텍스트
    public TextMeshProUGUI StockChangePrice;  // 이전 가격 대비 변동량 표시 텍스트 (비교 함수 필요)
    public TextMeshProUGUI StockDescription;  // 주식 요약 설명 표시 텍스트
    public Transform DetailInfoPanel;         // 기본 정보 탭 패널
    public Transform ServicePanel;            // 서비스 탭 패널
    public Transform ImportantPanel;          // 중요 지표 탭 패널
    public Transform RecentIssuePanel;        // 최근 이슈 탭 패널
    public TextMeshProUGUI StockDetailDescription; // 기업 상세 설명 표시 텍스트
    public TextMeshProUGUI revenuePrice;
    public TextMeshProUGUI ProfitPrice;
    public TextMeshProUGUI MarketCapPrice;
    public TextMeshProUGUI TotalSharePrice;
    public GameObject StockRefPrefab;


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
        StockName.text = stock.StockName;
        StockPrice.text = stock.StockCurrentPrice.ToString();
        StockDescription.text = stock.StockDescription;
        StockDetailDescription.text = stock.StockDetailDescription;
        revenuePrice.text = $"{stock.revenue} G";
        ProfitPrice.text = $"{stock.OperatingProfit} G";
        MarketCapPrice.text = $"{stock.marketCap} G";
        TotalSharePrice.text = $"{stock.TotalShare} stock";
    }

    // 모든 탭 패널을 비활성화 (탭 전환 전 초기화에 사용)
    public void HideAllPanels()
    {
        DetailInfoPanel.gameObject.SetActive(false);
        ServicePanel.gameObject.SetActive(false);
        ImportantPanel.gameObject.SetActive(false);
        RecentIssuePanel.gameObject.SetActive(false);
    }

    // 기본 정보 탭 버튼 클릭 시 기본 정보 패널만 활성화
    public void OnDetailInfoClick()
    {
        DetailInfoPanel.gameObject.SetActive(true);
        ServicePanel.gameObject.SetActive(false);
        ImportantPanel.gameObject.SetActive(false);
        RecentIssuePanel.gameObject.SetActive(false);
    }

    // 서비스 탭 버튼 클릭 시 서비스 패널만 활성화
    public void OnServiceClick()
    {
        DetailInfoPanel.gameObject.SetActive(false);
        ServicePanel.gameObject.SetActive(true);
        ImportantPanel.gameObject.SetActive(false);
        RecentIssuePanel.gameObject.SetActive(false);
    }

    // 중요 지표 탭 버튼 클릭 시 중요 지표 패널만 활성화
    public void OnImportantClick()
    {
        DetailInfoPanel.gameObject.SetActive(false);
        ServicePanel.gameObject.SetActive(false);
        ImportantPanel.gameObject.SetActive(true);
        RecentIssuePanel.gameObject.SetActive(false);
    }

    // 최근 이슈 탭 버튼 클릭 시 최근 이슈 패널만 활성화
    public void OnIssueClick()
    {
        DetailInfoPanel.gameObject.SetActive(false);
        ServicePanel.gameObject.SetActive(false);
        ImportantPanel.gameObject.SetActive(false);
        RecentIssuePanel.gameObject.SetActive(true);
    }

}
