using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 관심 종목 카드 UI를 관리
// FavoritePanel에서 프리팹 인스턴스화 후 Setup(stock) 호출
public class FavoriteCard : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI changeText;

    private Stock currentStock;

    // 주식 데이터로 카드 UI 채우기
    public void Setup(Stock stock)
    {
        currentStock = stock;
        icon.sprite = stock.StockIcon;
        nameText.text = stock.StockName;
        changeText.text = UIFormat.SignedPercent(stock.ChangePercent, 1);
    }

    // 카드 클릭 시 상세 패널 열기
    public void OnCardClick()
    {
        if (currentStock != null)
            UIManager.instance.ShowDetail(currentStock);
    }
}
