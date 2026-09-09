using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// 게임 내 화면 전환과 공용 상단 정보(골드·날짜·시대·손익률) 표시를 담당하는 클래스
public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("상단 정보")]
    public TextMeshProUGUI NoticeText;      // 오늘 발생한 알림 메시지
    public TextMeshProUGUI Ownedgold;       // 현재 보유 골드
    public TextMeshProUGUI TotalAssetText;  // 총 자산(골드 + 주식 평가금액)
    public TextMeshProUGUI TotalReturnText; // 보유 주식 기준 손익률(%)
    public TextMeshProUGUI CurrentDay;      // 현재 날짜
    public TextMeshProUGUI EraText;         // 현재 시대

    [Header("화면 패널")]
    public Transform EntirePanel;       // 홈 화면
    public Transform StockPanel;        // 종목 목록 화면
    public Transform newspanel;         // 뉴스 화면
    public Transform portfolioPanel;    // 자산 화면
    public Transform loanPanel;         // 대출 화면
    public Transform buyPanel;          // 구매/판매 화면
    public Transform favoriteFullPanel; // 관심 종목 전체 보기 화면

    [Header("패널 스크립트")]
    public DetailPanel detailpanel;
    public FavoritePanel favoritePanel;
    public QuarterReportPanel quarterReportPanel; // 분기 실적 발표 팝업 (화면 전환이 아니라 위에 덮어씀)
    public SidebarTabManager sidebarTabManager;
    [SerializeField] private NewsPanel newsPanelScript;
    [SerializeField] private NewsScreenBinder newsScreenBinder; // 새 뉴스 탭 화면에 게임 데이터를 넣어주는 연결 스크립트
    [SerializeField] private VaultScreenBinder vaultScreenBinder; // 새 자산 탭 화면에 게임 데이터를 넣어주는 연결 스크립트
    [SerializeField] private MarketScreenBinder marketScreenBinder; // 새 시장(종목) 탭 화면에 게임 데이터를 넣어주는 연결 스크립트

    void Awake()
    {
        instance = this;
    }

    // ── 상단 정보 갱신 ────────────────────────────────

    public void UpdateGoldText()
    {
        Ownedgold.text = $"<b>현재 골드 : \n<size=150%>{GameManager.Instance.currentgold}</size></b>";
        UpdateTotalAssetText(); // 골드가 바뀌면 총 자산도 함께 갱신
    }

    public void UpdateDayText()
    {
        CurrentDay.text = $"<b>현재 날짜 : \n<size=150%>{GameManager.Instance.currentDay}</size></b>";
    }

    // GameManager의 현재 시대를 읽어 시대 텍스트 갱신 — currentEra가 null이면 "안정기"로 표시
    public void UpdateEraText()
    {
        Era era = GameManager.Instance.currentEra;
        string eraName = era != null ? era.eraName : "안정기";
        EraText.text = $"<b>현재 시대 : \n<size=130%>{eraName}</size></b>";
    }

    // 메인 화면 가운데 UI — 예전에는 총 자산을 보여줬지만, 지금은 현재 보유 골드를 크게 보여준다
    // (총 자산·손익 요약은 자산 탭에서 확인한다)
    // 보유 주식 기준 손익률(%)은 상단 TotalReturnText에 계속 표시한다
    public void UpdateTotalAssetText()
    {
        long stockValue = Portfolio.TotalValue();          // 보유 주식 현재 평가금액 합
        float purchaseCost = Portfolio.TotalPurchaseCost(); // 평균 매수가 기준 원금 합

        TotalAssetText.text = $"<b><size=50%>현재 골드:</size> \n{GameManager.Instance.currentgold:N0} G</b>";

        // 손익률 = (현재 평가 - 매수 원금) / 매수 원금 × 100 — 매수한 주식이 없으면 0% 처리
        if (purchaseCost > 0f)
        {
            float returnPct = (stockValue - purchaseCost) / purchaseCost * 100f;
            TotalReturnText.text = UIFormat.SignedPercent(returnPct, 1); // 소수 첫째 자리까지 (예: +14.9%)
        }
        else
        {
            TotalReturnText.text = "0.0%";
        }
    }

    public void NoticeGold(string noticetext)
    {
        NoticeText.text = noticetext;
    }

    // 즐겨찾기가 바뀌었을 때 관련 화면을 한 번에 갱신한다
    // 별표를 누르는 곳이 두 군데(시장 탭 카드, 관심 종목 카드)라, 각자 따로 갱신하면 한쪽이 옛 상태로 남는다
    public void RefreshFavoriteUI()
    {
        if (favoritePanel != null) favoritePanel.RefreshAll();
        if (marketScreenBinder != null) marketScreenBinder.RefreshFavoriteStars();
    }

    // ── 화면 전환 ─────────────────────────────────────

    // 화면(홈, 종목, 상세, 뉴스, 자산, 대출, 구매, 관심 종목)을 하나만 열고 나머지는 전부 끈다
    public void OpenScreen(Transform target)
    {
        EntirePanel.gameObject.SetActive(target == EntirePanel);
        StockPanel.gameObject.SetActive(target == StockPanel);
        detailpanel.gameObject.SetActive(target == detailpanel.transform);
        newspanel.gameObject.SetActive(target == newspanel);
        portfolioPanel.gameObject.SetActive(target == portfolioPanel);
        loanPanel.gameObject.SetActive(target == loanPanel);
        buyPanel.gameObject.SetActive(target == buyPanel);
        favoriteFullPanel.gameObject.SetActive(target == favoriteFullPanel);

        // 닫기 버튼이 없는 화면(홈/뉴스/종목/자산)에서만 사이드바 표시
        // DetailPanel과 buyPanel은 자체 닫기 버튼이 있으므로 사이드바 숨김
        bool needsSidebar = target != buyPanel && target != detailpanel.transform;
        if (sidebarTabManager != null)
            sidebarTabManager.gameObject.SetActive(needsSidebar);
    }

    // 홈 화면으로 복귀 (사이드바도 홈 탭으로 되돌림)
    public void ReturnToEntire(GameObject selfPanel)
    {
        OpenScreen(EntirePanel);
        if (sidebarTabManager != null)
            sidebarTabManager.OpenTab(0);
    }

    // 종목 목록 화면으로 복귀 (사이드바도 종목 탭으로 되돌림)
    public void ReturnToStock(GameObject selfPanel)
    {
        OpenScreen(StockPanel);
        if (marketScreenBinder != null)
            marketScreenBinder.Refresh(); // 매매 후 돌아오면 바뀐 가격·보유 상태를 다시 그린다
        if (sidebarTabManager != null)
            sidebarTabManager.OpenTab(2);
    }

    public void ShowDetail(Stock stock)
    {
        OpenScreen(detailpanel.transform);
        detailpanel.ShowDetail(stock);
    }

    // 시장(종목) 탭 열기 — 새 화면(CurrentMarketScreen)을 지금 게임 상태로 다시 그린다
    public void OpenMarketPanel()
    {
        OpenScreen(StockPanel);

        // 연결 스크립트가 없으면(아직 인스펙터 연결 전) 그냥 넘어간다
        if (marketScreenBinder != null)
            marketScreenBinder.Refresh();
    }

    public void OpenNewsPanel()
    {
        OpenScreen(newspanel);
        newsPanelScript.BigNewsClick();

        // 새 뉴스 탭 화면을 지금 게임 상태로 다시 그린다 (연결 안 돼 있으면 그냥 넘어간다)
        if (newsScreenBinder != null)
            newsScreenBinder.Refresh();
    }

    public void OpenPortfolioPanel()
    {
        OpenScreen(portfolioPanel);

        // 자산 탭 화면을 지금 게임 상태로 다시 그린다 (연결 안 돼 있으면 그냥 넘어간다)
        if (vaultScreenBinder != null)
            vaultScreenBinder.Refresh();
    }

    public void OpenFavoriteFullPanel()
    {
        OpenScreen(favoriteFullPanel);
        favoritePanel.RefreshFull();
    }

    public void OpenLoanPanel()
    {
        OpenScreen(loanPanel);
    }

    public void OnSaveLoadClick()
    {
        // 중복 클릭으로 저장 화면이 여러 개 열리지 않도록 한다.
        if (SceneManager.GetSceneByName("SaveLoad").isLoaded) return;

        PlayerPrefs.SetInt("FromMainMenu", 0);
        PlayerPrefs.Save();
        // 게임 씬을 유지해야 뉴스·차트·NPC 등 저장 파일 밖의 상태도 이어진다.
        SceneManager.LoadScene("SaveLoad", LoadSceneMode.Additive);
    }
}
