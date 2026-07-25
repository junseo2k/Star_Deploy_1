using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI NoticeText;
    public TextMeshProUGUI Ownedgold;
    public TextMeshProUGUI TotalAssetText;    // ← 추가: 총 자산(골드+주식) 표시
    public TextMeshProUGUI TotalReturnText;   // ← 추가: 손익률(%) 표시
    public TextMeshProUGUI CurrentDay;
    public DetailPanel detailpanel;
    public Transform EntirePanel;
    public Transform newspanel;
    public Transform portfolioPanel;
    public PortfolioPanel portfolioPanelScript;
    public Transform loanPanel;
    public LoanPanel loanPanelScript;
    public Transform buyPanel;
    public SidebarTabManager sidebarTabManager; // 사이드바 탭 초기화용 참조 추가
    public static UIManager instance;
    public Transform StockPanel; 
    public TextMeshProUGUI EraText;    // ← 추가: 현재 시대를 표시하는 텍스트
    [SerializeField] private NewsPanel newsPanelScript;

    void Awake()
    {
        instance = this;
    }

    void Start() { }
    void Update() { }

    public void UpdateGoldText()
    {
        Ownedgold.text = $"현재 골드 : \n<size=150%><b>{GameManager.Instance.currentgold}</size></b>";
        UpdateTotalAssetText(); // ← 추가: 골드가 바뀌면 총 자산도 함께 갱신
    }

    public void NoticeGold(string noticetext)
    {
        NoticeText.text = noticetext;
    }

    // 화면(EntirePanel, DetailPanel, News, Portfolio, Loan, Buy)을 하나만 열고 나머지는 전부 끈다
    // + 사이드바 탭도 항상 초기화 (열려있던 탭 표시 제거)
    public void OpenScreen(Transform target)
{
    EntirePanel.gameObject.SetActive(target == EntirePanel);
    StockPanel.gameObject.SetActive(target == StockPanel);
    detailpanel.gameObject.SetActive(target == detailpanel.transform);
    newspanel.gameObject.SetActive(target == newspanel);
    portfolioPanel.gameObject.SetActive(target == portfolioPanel);
    loanPanel.gameObject.SetActive(target == loanPanel);
    buyPanel.gameObject.SetActive(target == buyPanel);

    // 닫기 버튼이 없는 화면(홈/뉴스/종목/자산)에서만 사이드바 표시
    // DetailPanel과 buyPanel은 자체 닫기 버튼이 있으므로 사이드바 숨김
    bool needsSidebar = (target != buyPanel && target != detailpanel.transform);
    if (sidebarTabManager != null)
        sidebarTabManager.gameObject.SetActive(needsSidebar);
}

    // selfPanel을 숨기고 전체 주식 목록 패널로 복귀
    public void ReturnToEntire(GameObject selfPanel)
    {
        OpenScreen(EntirePanel);
        if (sidebarTabManager != null)
            sidebarTabManager.OpenTab(0); // 홈 탭으로 복원
    }
    public void ReturnToStock(GameObject selfPanel)
    {
        OpenScreen(StockPanel);
        if (sidebarTabManager != null)
            sidebarTabManager.OpenTab(2); // 홈 탭으로 복원
    }

    public void UpdateDayText()
    {
        CurrentDay.text = $"현재 날짜 : \n<size=150%><b>{GameManager.Instance.currentDay}</size></b>";
        
    }
    // GameManager의 현재 시대를 읽어 시대 텍스트 갱신
// currentEra가 null이면 "안정기"로 표시
    public void UpdateEraText()
{
    if (GameManager.Instance.currentEra != null)
        EraText.text = $"현재 시대 : \n<size=150%><b>{GameManager.Instance.currentEra.eraName}</size></b>";
    else
        EraText.text = "현재 시대 : \n<size=150%><b>안정기</size></b>";
}
    // 보유 주식 평가금액 + 현재 골드 = 총 자산
// 보유 주식 기준 손익률(%)도 함께 계산해서 표시
public void UpdateTotalAssetText()
{
    StockControl[] allStocks = FindObjectsByType<StockControl>(
        FindObjectsInactive.Include, FindObjectsSortMode.None);

    int stockValue = 0;      // 보유 주식 현재 평가금액 합
    float purchaseCost = 0f; // 평균 매수가 기준 원금 합

    foreach (StockControl sc in allStocks)
    {
        if (sc.ownedAmount <= 0) continue;
        stockValue += sc.stockinfo.StockCurrentPrice * sc.ownedAmount;
        purchaseCost += sc.avgPurchasePrice * sc.ownedAmount;
    }

    // 총 자산 = 현재 골드 + 주식 평가금액
    int totalAsset = GameManager.Instance.currentgold + stockValue;
    TotalAssetText.text = $"<size=50%>총 자산:</size> \n<b>{totalAsset:N0} G</b>";

    // 손익률 = (현재 평가 - 매수 원금) / 매수 원금 × 100
    // 매수한 주식이 없으면 0% 처리
    if (purchaseCost > 0f)
    {
        float returnPct = (stockValue - purchaseCost) / purchaseCost * 100f;
        string sign = returnPct >= 0f ? "+" : "";
        string color = returnPct >= 0f ? "#FF4444" : "#4488FF"; // 상승 빨강, 하락 파랑
        TotalReturnText.text = $"<color={color}>{sign}{returnPct:F2}%</color>";
    }
    else
    {
        TotalReturnText.text = "0.00%";
    }
}
    public void ShowDetail(Stock stock)
    {
        OpenScreen(detailpanel.transform);
        detailpanel.ShowDetail(stock);
    }

    public void OpenNewsPanel()
    {
        OpenScreen(newspanel);
        newsPanelScript.BigNewsClick();
    }

    public void OpenPortfolioPanel()
    {
        OpenScreen(portfolioPanel);
        portfolioPanelScript.Open();
    }

    public void OpenLoanPanel()
    {
        OpenScreen(loanPanel);
    }

    public void OnSaveLoadClick()
    {
        PlayerPrefs.SetInt("FromMainMenu", 0);
        PlayerPrefs.Save();
        SceneManager.LoadScene("SaveLoad");
    }
}