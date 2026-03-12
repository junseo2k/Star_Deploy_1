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

    public int currentPrice;
    public int ownedAmount;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StockNameText.text = stockinfo.StockName;
        StockDescriptionText. text = stockinfo.StockDescription;
        StockPriceText.text = $"{stockinfo.StockBasePrice} gold";
        stockImage.sprite = stockinfo.StockIcon;
        ownedAmountText.text = $"현재 보유 수량: {ownedAmount}";
        currentPrice = stockinfo.StockBasePrice;
        UIManager.instance.UpdateGoldText();
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void OnBuyClick()
    {
        if (GameManager.Instance.currentgold - currentPrice < 0)
        {
            UIManager.instance.NoticeGold("You don't have enough money");
        }
        else
        {
            GameManager.Instance.UpdateGold(-currentPrice);
            ownedAmount++;
            ownedAmountText.text = $"현재 보유 수량: {ownedAmount}";
            UIManager.instance.UpdateGoldText();
        }

        GameManager.Instance.SaveGame();
    }    
}
