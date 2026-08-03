using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 개별 주식 아이템 UI를 제어하는 클래스
// 주식 정보 표시, 상세 패널 전환, 가격 갱신을 담당
// 같은 주식이 종족 탭과 산업 탭 양쪽에 표시되면 이 컴포넌트가 2개 생기므로,
// 보유 수량 같은 게임 상태는 여기 두지 않고 Portfolio에서 읽고 쓴다
public class StockControl : MonoBehaviour
{
    public Stock stockinfo;                      // 이 아이템이 표시하는 주식 데이터 (ScriptableObject)
    public TextMeshProUGUI StockNameText;        // 주식 이름 표시 UI
    public TextMeshProUGUI StockDescriptionText; // 주식 설명 표시 UI
    public Image stockImage;                     // 주식 아이콘 이미지 표시 UI
    public TextMeshProUGUI ownedAmountText;      // 보유 수량 표시 UI

    [Header("즐겨찾기")]
    public Image favoriteIcon;       // 별표 아이콘 (버튼의 이미지)
    public Sprite favoriteOnSprite;  // 즐겨찾기 켜진 상태 스프라이트 (채워진 별)
    public Sprite favoriteOffSprite; // 즐겨찾기 꺼진 상태 스프라이트 (빈 별)

    // 보유 수량 — 실제 값은 Portfolio가 보관하므로 인스턴스가 여러 개여도 항상 같은 값을 본다
    public int ownedAmount
    {
        get => Portfolio.GetAmount(stockinfo);
        set => Portfolio.SetAmount(stockinfo, value);
    }

    // 평균 매수 단가 (포트폴리오 손익 계산에 사용)
    public float avgPurchasePrice
    {
        get => Portfolio.GetAvgPurchasePrice(stockinfo);
        set => Portfolio.SetAvgPurchasePrice(stockinfo, value);
    }

    // 씬에 존재하는 모든 StockControl 반환 (비활성 탭 포함)
    public static StockControl[] FindAll()
    {
        return FindObjectsByType<StockControl>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    }

    // 모든 StockControl의 보유 수량 텍스트를 갱신 — 거래 직후 호출
    public static void RefreshAllOwnedText()
    {
        foreach (StockControl sc in FindAll())
            sc.RefreshOwnedText();
    }

    // 이 GameObject가 활성화될 때마다 자동 호출
    // 탭이 닫혀 있는 동안 가격·보유 수량이 바뀌었을 수 있으므로 다시 열릴 때 최신 값으로 맞춘다
    void OnEnable()
    {
        if (stockinfo == null) return; // stockinfo가 아직 할당되지 않은 경우 오류 방지

        UpdatePriceText();
        RefreshOwnedText();
    }

    // 상세 패널이 이 주식을 표시 중일 때 상세 패널의 가격 텍스트를 최신 값으로 갱신
    public void UpdatePriceText()
    {
        if (UIManager.instance.detailpanel.currentstock == stockinfo)
            UIManager.instance.detailpanel.StockPrice.text = $"{stockinfo.StockCurrentPrice} gold";
    }

    // 보유 수량 텍스트 갱신
    public void RefreshOwnedText()
    {
        if (ownedAmountText != null)
            ownedAmountText.text = $"현재 보유 수량: {ownedAmount}";
    }

    // 프리팹 생성 직후 호출 — 주식 이름, 설명, 이미지, 보유 수량 등 UI 초기값 설정
    public void Initialize()
    {
        StockNameText.text = stockinfo.StockName;
        StockDescriptionText.text = stockinfo.StockDescription;
        stockImage.sprite = stockinfo.StockIcon;
        RefreshOwnedText();
        RefreshFavoriteIcon();

        UIManager.instance.UpdateGoldText();
        UIManager.instance.UpdateDayText();
    }

    // 주식 아이콘 이미지 클릭 시 호출 — 상세 정보 패널 열기
    public void OnImageClick()
    {
        UIManager.instance.ShowDetail(stockinfo);
    }

    // 별표 버튼 클릭 시 호출 — 상태 토글, 아이콘 갱신, 저장
    public void ToggleFavorite()
    {
        stockinfo.isFavorite = !stockinfo.isFavorite;
        RefreshFavoriteIcon();
        GameManager.Instance.SaveGame();
        UIManager.instance.favoritePanel.RefreshHome();
    }

    // 현재 isFavorite 값에 맞게 별표 아이콘 스프라이트 갱신
    public void RefreshFavoriteIcon()
    {
        if (favoriteIcon != null)
            favoriteIcon.sprite = stockinfo.isFavorite ? favoriteOnSprite : favoriteOffSprite;
    }
}
