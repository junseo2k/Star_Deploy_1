using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StockControl : MonoBehaviour
{
    public Stock stockinfo; //stock타입의 오브젝트
    public TextMeshProUGUI StockNameText; //주식 이름 표시 UI
    public TextMeshProUGUI StockDescriptionText; // 주식 설명 표시 UI
    public UnityEngine.UI.Image stockImage;// 주식 이미지 표시 UI
    public TextMeshProUGUI StockPriceText;// 주식 가격 표시 UI
    public TextMeshProUGUI ownedAmountText;// 주식 보유 수량 표시 UI
    public int ownedAmount; //주식 보유 수량
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    void OnEnable() //탭이 닫혀있는 동안 가격이 바뀌었을 때, 다시 열면 최신 가격을 보여주기 위해 OnEnable 함수 작성. 
    // 이 함수는 이 스크립트가 붙은 GameObject가 활성화될 때마다 자동으로 호출되는 함수. 즉 주식 gameObject가 활성화될 때 마다 가격 업데이트.
    {
        Debug.Log($"OnEnable 호출! {stockinfo?.StockName} / {stockinfo?.StockCurrentPrice}");
        if (stockinfo != null) // stockinfo가 없을 때 오류 방지
        {
            UpdatePriceText();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void OnBuyClick() //구매 버튼. 0원 이하로 떨어지면 구매 막힘
    {
        if (GameManager.Instance.currentgold - stockinfo.StockCurrentPrice < 0)
        {
            UIManager.instance.NoticeGold("You don't have enough money");
            return;
        }
    

        GameManager.Instance.UpdateGold(-stockinfo.StockCurrentPrice);
        ownedAmount++;
        ownedAmountText.text = $"현재 보유 수량: {ownedAmount}";
        UIManager.instance.UpdateGoldText();
        GameManager.Instance.SaveGame();
    }
    public void UpdatePriceText() //가격 변동 시 업데이트
    {
        Debug.Log($"UpdatePriceText 호출! {stockinfo.StockName} / {stockinfo.StockCurrentPrice}");
        StockPriceText.text = $"{stockinfo.StockCurrentPrice} gold";
    }
    public void OnSellClick() //판매 버튼. 수량이 0일 때 판매 막힘
    {
        if (ownedAmount <= 0)
        {
            UIManager.instance.NoticeGold("You don't have enough stock.");
            return;
        }
        GameManager.Instance.UpdateGold(+stockinfo.StockCurrentPrice);
        ownedAmount--;
        ownedAmountText.text = $"현재 보유 수량: {ownedAmount}";
        UIManager.instance.UpdateGoldText();
    }
    public void Initialize() //게임 UI에 값(주식 이름, 설명, 가격 등) 표시 
    {
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
