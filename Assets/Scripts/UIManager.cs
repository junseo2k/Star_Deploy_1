using TMPro;
using UnityEngine;

// UI 전반을 관리하는 싱글톤 클래스 — 공지 텍스트, 골드, 날짜 등 화면 표시를 담당
public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI NoticeText;  // 사건 발생·거래 결과 등을 표시하는 공지 텍스트
    public TextMeshProUGUI Ownedgold;  // 현재 보유 골드를 표시하는 텍스트
    public TextMeshProUGUI CurrentDay; // 현재 날짜를 표시하는 텍스트
    public DetailPanel detailpanel; // 주식 상세 패널 참조 (ShowDetail 호출에 사용)
    public Transform EntirePanel;   // 전체 주식 목록 패널 (상세 패널 열릴 때 비활성화 대상)
    public Transform newspanel;

    public static UIManager instance; // 어디서든 UIManager에 접근하기 위한 싱글톤 인스턴스
    [SerializeField] private NewsPanel newsPanelScript;

    // Start보다 먼저 실행 — 싱글톤 인스턴스 등록
    void Awake()
    {
        instance = this;
    }

    void Start()
    {
    }

    void Update()
    {

    }

    // GameManager의 현재 골드 값을 읽어 골드 텍스트 갱신
    public void UpdateGoldText()
    {
        Ownedgold.text = $"현재 골드 : {GameManager.Instance.currentgold}";
    }

    // 전달받은 문자열을 공지 텍스트에 표시 (거래 실패 등 알림에 사용)
    public void NoticeGold(string noticetext)
    {
        NoticeText.text = noticetext;
    }

    // GameManager의 현재 날짜 값을 읽어 날짜 텍스트 갱신
    public void UpdateDayText()
    {
        CurrentDay.text = $"현재 날짜 : {GameManager.Instance.currentDay}";
    }
    // StockControl은 프리팹이라 여러 개 존재해서 DetailPanel을 직접 참조하기 번거로움
    // UIManager 싱글톤을 중간 전달자로 활용해 코드 한 줄로 접근
    public void ShowDetail(Stock stock)
    {
        EntirePanel.gameObject.SetActive(false);
        detailpanel.ShowDetail(stock);
        detailpanel.gameObject.SetActive(true);
        
    }
    public void OpenNewsPanel()
    {
        EntirePanel.gameObject.SetActive(false);
        newspanel.gameObject.SetActive(true);
        newsPanelScript.BigNewsClick();
    }
    
}
