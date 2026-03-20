using TMPro;
using UnityEngine;

public class OrderPanel : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public int ownedAmount;
    public StockControl stockcontrol;
    private int PurchaseNum;
    public TextMeshProUGUI NoticeText;
    public Transform BuyPanel;
    public Transform EntirePanel;
    public TextMeshProUGUI PlusText;
    public TextMeshProUGUI MinusText;
    public TextMeshProUGUI Check1Text;
    public TextMeshProUGUI Check2Text;
    public TextMeshProUGUI PurchaseText;
    public TextMeshProUGUI currentText;
    public TextMeshProUGUI CancelText;
    
    void Start()
    {
        PurchaseNum = 1;
        PlusText.text = "+";
        MinusText.text = "-";
        Check1Text.text = "Purchase?";
        Check2Text.text = "Check";
        PurchaseText.text = $"OwnedStock : {PurchaseNum}";
        NoticeText.text = "";
        CancelText.text = "Cancel";
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void OnPlusClick()
    {
        PurchaseNum++;
        PurchaseText.text = $"OwnedStock : {PurchaseNum}";
    }
    public void OnMinusClick()
    {
        PurchaseNum--;
        if (PurchaseNum < 1)
        {
            NoticeText.text = "You should buy at least 1 Stock.";
            return;
        }
        PurchaseText.text = $"OwnedStock : {PurchaseNum}";
    }
    public void OnBuyClick() //구매 버튼. 0원 이하로 떨어지면 구매 막힘
    {
        if (GameManager.Instance.currentgold - stockcontrol.stockinfo.StockCurrentPrice * PurchaseNum < 0)
        {
            NoticeText.text = "You don't have enough money";
            return;
        }
    

        GameManager.Instance.UpdateGold(-stockcontrol.stockinfo.StockCurrentPrice * PurchaseNum);
        stockcontrol.ownedAmount += PurchaseNum;
        stockcontrol.ownedAmountText.text = $"현재 보유 수량: {stockcontrol.ownedAmount}";
        UIManager.instance.UpdateGoldText();
        GameManager.Instance.SaveGame();
        BuyPanel.gameObject.SetActive(false);
        EntirePanel.gameObject.SetActive(true);
    }
    public void OnCancelClick()
    {
        BuyPanel.gameObject.SetActive(false);
        EntirePanel.gameObject.SetActive(true);
    }
}
