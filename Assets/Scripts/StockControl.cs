using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StockControl : MonoBehaviour
{
    public Stock stockinfo;
    public TextMeshProUGUI StockNameText;
    public TextMeshProUGUI StockDescriptionText;
    public UnityEngine.UI.Image stockImage;
    public TextMeshProUGUI StockPriceText;
    public TextMeshProUGUI ownedAmountText;
    public int ownedAmount;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    void OnEnable()
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
    public void OnBuyClick()
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
    public void UpdatePriceText()
    {
        Debug.Log($"UpdatePriceText 호출! {stockinfo.StockName} / {stockinfo.StockCurrentPrice}");
        StockPriceText.text = $"{stockinfo.StockCurrentPrice} gold";
    }
    public void OnSellClick()
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
    public void Initialize()
    {
        if (stockinfo.StockCurrentPrice == 0)
            stockinfo.StockCurrentPrice = stockinfo.StockBasePrice;
    
        StockNameText.text = stockinfo.StockName;
        StockDescriptionText.text = stockinfo.StockDescription;
        StockPriceText.text = $"{stockinfo.StockCurrentPrice} gold";
        stockImage.sprite = stockinfo.StockIcon;
        ownedAmountText.text = $"현재 보유 수량: {ownedAmount}";
        UIManager.instance.UpdateGoldText();
    }     
}
