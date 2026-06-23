using TMPro;
using UnityEngine;

// 주식 구매/판매 확인 패널을 관리하는 클래스
// 수량 조절 버튼(+/-), 확인/취소 버튼 처리를 담당
public class OrderPanel : MonoBehaviour
{
    public int ownedAmount;          // 현재 보유 수량 (참고용)
    public StockControl stockcontrol; // 거래 대상 주식의 StockControl 참조
    public int StockNum;             // 현재 입력된 거래 수량
    public TextMeshProUGUI NoticeText;    // 오류 메시지 등을 표시하는 공지 텍스트
    public Transform BuyPanel;            // 구매/판매 입력 패널 (확인 후 숨김)
    public Transform EntirePanel;         // 전체 주식 목록 패널 (확인 후 복귀)
    public TextMeshProUGUI PlusText;      // + 버튼 텍스트
    public TextMeshProUGUI MinusText;     // - 버튼 텍스트
    public TextMeshProUGUI Check1Text;    // 구매 여부 안내 텍스트
    public TextMeshProUGUI Check2Text;    // 확인 버튼 텍스트
    public TextMeshProUGUI PurchaseText;  // 현재 수량·금액 표시 텍스트
    public TextMeshProUGUI currentText;   // 최대 구매/판매 가능 수량 표시 텍스트
    public TextMeshProUGUI CancelText;    // 취소 버튼 텍스트
    public bool isBuyMode;           // true: 구매 모드, false: 판매 모드
    public int OwnedNum;             // 현재 보유 수량 스냅샷 (패널 열릴 때 설정)

    // 구매 패널 열릴 때 UI 텍스트 초기값 설정
    void Start()
    {
        PlusText.text = "+";
        MinusText.text = "-";
        Check1Text.text = "Purchase?";
        Check2Text.text = "Check";
        NoticeText.text = "";
        CancelText.text = "Cancel";
    }

    void Update()
    {

    }

    // + 버튼 클릭 시 거래 수량 1 증가 및 수량·금액 표시 갱신
    public void OnPlusClick()
    {
        StockNum++;
        PurchaseText.text = $"OwnedStock : {StockNum}, \n {stockcontrol.stockinfo.StockCurrentPrice * StockNum} gold.";
    }

    // - 버튼 클릭 시 거래 수량 1 감소
    // 수량이 1 미만이면 경고 메시지를 출력하고 감소를 막음
    public void OnMinusClick()
    {
        if (isBuyMode == true)
        {
            if (StockNum < 1)
            {
                NoticeText.text = "You should buy at least 1 Stock.";
                return;
            }
        }
        else
        {
            if (StockNum < 1)
            {
                NoticeText.text = "You should sell at least 1 Stock.";
                return;
            }
        }
        StockNum--;
        PurchaseText.text = $"OwnedStock : {StockNum}, \n {stockcontrol.stockinfo.StockCurrentPrice * StockNum} gold.";
    }

    // 확인 버튼 클릭 시 거래 처리
    // 구매 모드: 골드 부족 시 차단 → 골드 차감·보유 수량 증가
    // 판매 모드: 보유 수량 부족 시 차단 → 골드 증가·보유 수량 감소
    // 처리 후 게임 저장 및 패널 전환
    public void OncheckClick()
    {
        if (isBuyMode == true)
        {
            // 잔액 부족 시 구매 차단
            if (GameManager.Instance.currentgold - stockcontrol.stockinfo.StockCurrentPrice * StockNum < 0)
            {
                NoticeText.text = "You don't have enough money";
                return;
            }
            // 평균 매수 단가 갱신: (기존 평균 × 기존 수량 + 이번 매수 금액) / 매수 후 총 수량
            float totalCost = stockcontrol.avgPurchasePrice * stockcontrol.ownedAmount
                            + stockcontrol.stockinfo.StockCurrentPrice * StockNum;
            stockcontrol.ownedAmount += StockNum;
            stockcontrol.avgPurchasePrice = totalCost / stockcontrol.ownedAmount;
            GameManager.Instance.UpdateGold(-stockcontrol.stockinfo.StockCurrentPrice * StockNum);
        }
        else
        {
            // 보유 수량 부족 시 판매 차단
            if (StockNum > stockcontrol.ownedAmount)
            {
                NoticeText.text = "You don't have enough Stocks";
                return;
            }
            GameManager.Instance.UpdateGold(stockcontrol.stockinfo.StockCurrentPrice * StockNum);
            stockcontrol.ownedAmount -= StockNum;
        }

        // 보유 수량 UI 갱신 및 골드 UI 갱신
        stockcontrol.ownedAmountText.text = $"현재 보유 수량: {stockcontrol.ownedAmount}";
        UIManager.instance.UpdateGoldText();


        // 구매/판매 패널 닫고 전체 주식 목록 패널로 복귀
        BuyPanel.gameObject.SetActive(false);
        EntirePanel.gameObject.SetActive(true);
    }

    // 취소 버튼 클릭 시 거래 패널 닫고 전체 주식 목록 패널로 복귀
    public void OnCancelClick()
    {
        BuyPanel.gameObject.SetActive(false);
        EntirePanel.gameObject.SetActive(true);
    }
}
