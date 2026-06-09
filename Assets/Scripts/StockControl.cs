using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 개별 주식 아이템 UI를 제어하는 클래스
// 주식 정보 표시, 상세 패널 전환, 가격 갱신을 담당
public class StockControl : MonoBehaviour
{
    public Stock stockinfo;                       // 이 아이템이 표시하는 주식 데이터 (ScriptableObject)
    public TextMeshProUGUI StockNameText;         // 주식 이름 표시 UI
    public TextMeshProUGUI StockDescriptionText;  // 주식 설명 표시 UI
    public UnityEngine.UI.Image stockImage;       // 주식 아이콘 이미지 표시 UI
    public TextMeshProUGUI ownedAmountText;       // 보유 수량 표시 UI
    public int ownedAmount;                       // 현재 보유 수량
    public Transform EntirePanel;  // 전체 주식 목록 패널 (구매/판매 후 복귀 대상)
    public Transform BuyPanel;     // 구매/판매 입력 패널
    public OrderPanel orderPanel;  // 구매/판매 확인 패널 참조
    public int maxmount;           // 구매 또는 판매 가능한 최대 수량
    public float avgPurchasePrice; // 평균 매수 단가 (포트폴리오 손익 계산에 사용)

    void Start()
    {

    }

    // 이 GameObject가 활성화될 때마다 자동 호출
    // 탭이 닫혀 있는 동안 가격이 변동되었을 경우, 탭을 다시 열면 최신 가격을 표시하기 위해 사용
    void OnEnable()
    {
        if (stockinfo != null) // stockinfo가 아직 할당되지 않은 경우 오류 방지
        {
            UpdatePriceText();
        }
    }

    void Update()
    {

    }

    // 상세 패널이 이 주식을 표시 중일 때 상세 패널의 가격 텍스트를 최신 값으로 갱신
    public void UpdatePriceText()
    {
   
        if (UIManager.instance.detailpanel.currentstock == stockinfo)
        {
            UIManager.instance.detailpanel.StockPrice.text =
                $"{stockinfo.StockCurrentPrice} gold";
        }
    }

    // 프리팹 생성 직후 호출 — 주식 이름, 설명, 이미지, 보유 수량 등 UI 초기값 설정
    public void Initialize()
    {
        StockNameText.text = stockinfo.StockName;
        StockDescriptionText.text = stockinfo.StockDescription;
        stockImage.sprite = stockinfo.StockIcon;
        ownedAmountText.text = $"현재 보유 수량: {ownedAmount}";
        UIManager.instance.UpdateGoldText();
        UIManager.instance.UpdateDayText();
    }
    // 주식 아이콘 이미지 클릭 시 호출 — 상세 정보 패널 열기
    public void OnImageClick()
    {
        UIManager.instance.ShowDetail(stockinfo);
    }
}
