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

    [Header("즐겨찾기")]
    public Image favoriteIcon;       // 별표 아이콘 (버튼의 이미지)
    public Sprite favoriteOnSprite;  // 즐겨찾기 켜진 상태 스프라이트 (채워진 별)
    public Sprite favoriteOffSprite; // 즐겨찾기 꺼진 상태 스프라이트 (빈 별)

    private Stock currentStock;

    // 주식 데이터로 카드 UI 채우기
    public void Setup(Stock stock)
    {
        currentStock = stock;
        icon.sprite = stock.StockIcon;
        nameText.text = stock.StockName;
        changeText.text = UIFormat.SignedPercent(stock.ChangePercent, 1);
        RefreshFavoriteIcon();
    }

    // 카드 클릭 시 상세 패널 열기
    public void OnCardClick()
    {
        if (currentStock != null)
            UIManager.instance.ShowDetail(currentStock);
    }

    // 별표 버튼 클릭 시 호출 — 즐겨찾기를 끄면 이 카드가 목록에서 빠진다
    // 갱신하면 이 카드 자체가 사라지므로, 아이콘부터 바꾸고 마지막에 갱신을 부른다
    public void ToggleFavorite()
    {
        if (currentStock == null) return;

        currentStock.isFavorite = !currentStock.isFavorite;
        RefreshFavoriteIcon();
        GameManager.Instance.SaveGame();
        UIManager.instance.RefreshFavoriteUI();
    }

    // 현재 isFavorite 값에 맞게 별표 아이콘 스프라이트 갱신
    public void RefreshFavoriteIcon()
    {
        if (favoriteIcon != null && currentStock != null)
            favoriteIcon.sprite = currentStock.isFavorite ? favoriteOnSprite : favoriteOffSprite;
    }
}
