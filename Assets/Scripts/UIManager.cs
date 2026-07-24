using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI NoticeText;
    public TextMeshProUGUI Ownedgold;
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
    [SerializeField] private NewsPanel newsPanelScript;

    void Awake()
    {
        instance = this;
    }

    void Start() { }
    void Update() { }

    public void UpdateGoldText()
    {
        Ownedgold.text = $"현재 골드 : {GameManager.Instance.currentgold}";
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
        CurrentDay.text = $"현재 날짜 : {GameManager.Instance.currentDay}";
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