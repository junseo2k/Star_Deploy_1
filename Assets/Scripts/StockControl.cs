using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 개별 주식 아이템 UI를 제어하는 클래스
// 주식 정보 표시, 구매/판매 버튼 처리, 가격 갱신을 담당
public class StockControl : MonoBehaviour
{
    public Stock stockinfo;                       // 이 아이템이 표시하는 주식 데이터 (ScriptableObject)
    public TextMeshProUGUI StockNameText;         // 주식 이름 표시 UI
    public TextMeshProUGUI StockDescriptionText;  // 주식 설명 표시 UI
    public UnityEngine.UI.Image stockImage;       // 주식 아이콘 이미지 표시 UI
    public TextMeshProUGUI StockPriceText;        // 주식 현재 가격 표시 UI
    public TextMeshProUGUI ownedAmountText;       // 보유 수량 표시 UI
    public int ownedAmount;                       // 현재 보유 수량
    public Transform EntirePanel;  // 전체 주식 목록 패널 (구매/판매 후 복귀 대상)
    public Transform BuyPanel;     // 구매/판매 입력 패널
    public OrderPanel orderPanel;  // 구매/판매 확인 패널 참조
    public int maxmount;           // 구매 또는 판매 가능한 최대 수량

    void Start()
    {

    }

    // 이 GameObject가 활성화될 때마다 자동 호출
    // 탭이 닫혀 있는 동안 가격이 변동되었을 경우, 탭을 다시 열면 최신 가격을 표시하기 위해 사용
    void OnEnable()
    {
        Debug.Log($"OnEnable 호출! {stockinfo?.StockName} / {stockinfo?.StockCurrentPrice}");
        if (stockinfo != null) // stockinfo가 아직 할당되지 않은 경우 오류 방지
        {
            UpdatePriceText();
        }
    }

    void Update()
    {

    }

    // 구매 버튼 클릭 시 호출
    // 전체 패널을 숨기고 구매 입력 패널을 열며, OrderPanel에 이 주식 정보와 구매 모드를 전달
    public void OnBuyButtonClick()
    {
        EntirePanel.gameObject.SetActive(false);
        BuyPanel.gameObject.SetActive(true);
        orderPanel.stockcontrol = this;
        maxmount = GameManager.Instance.currentgold / stockinfo.StockCurrentPrice; // 최대 구매 가능 수량 계산
        orderPanel.currentText.text = $"Maximum: {maxmount}";
        orderPanel.PurchaseText.text = $"OwnedStock : 1, \n {stockinfo.StockCurrentPrice} gold.";
        orderPanel.OwnedNum = ownedAmount;
        orderPanel.isBuyMode = true; // 구매 모드로 설정
    }

    // 판매 버튼 클릭 시 호출
    // 전체 패널을 숨기고 판매 입력 패널을 열며, OrderPanel에 이 주식 정보와 판매 모드를 전달
    public void OnSellButtonClick()
    {
        EntirePanel.gameObject.SetActive(false);
        BuyPanel.gameObject.SetActive(true);
        orderPanel.stockcontrol = this;
        maxmount = ownedAmount; // 최대 판매 가능 수량 = 보유 수량
        orderPanel.currentText.text = $"Maximum: {maxmount}";
        orderPanel.StockNum = 1;
        orderPanel.PurchaseText.text = $"OwnedStock : {orderPanel.StockNum}, \n {stockinfo.StockCurrentPrice} gold.";
        orderPanel.OwnedNum = ownedAmount;
        orderPanel.isBuyMode = false; // 판매 모드로 설정
    }

    // 주식 가격이 변동되었을 때 가격 텍스트를 최신 값으로 갱신
    public void UpdatePriceText()
    {
        Debug.Log($"UpdatePriceText 호출! {stockinfo.StockName} / {stockinfo.StockCurrentPrice}");
        StockPriceText.text = $"{stockinfo.StockCurrentPrice} gold";
    }

    // 프리팹 생성 직후 호출 — 주식 이름, 설명, 가격, 이미지, 보유 수량 등 UI 초기값 설정
    public void Initialize()
    {
        // 현재 가격이 0이면 기준 가격으로 초기화 (첫 실행 시)
        if (stockinfo.StockCurrentPrice == 0)
            stockinfo.StockCurrentPrice = stockinfo.StockBasePrice;

        StockNameText.text = stockinfo.StockName;
        StockDescriptionText.text = stockinfo.StockDescription;
        StockPriceText.text = $"{stockinfo.StockCurrentPrice} gold";
        stockImage.sprite = stockinfo.StockIcon;
        ownedAmountText.text = $"현재 보유 수량: {ownedAmount}";
        UIManager.instance.UpdateGoldText();
        UIManager.instance.UpdateDayText();
    }
}
