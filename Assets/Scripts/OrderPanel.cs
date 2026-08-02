using TMPro;
using UnityEngine;

// 주식 구매/판매 확인 패널을 관리하는 클래스
// 수량 조절 버튼(+/-), 확인/취소 버튼 처리를 담당
public class OrderPanel : MonoBehaviour
{
    public StockControl stockcontrol; // 거래 대상 주식의 StockControl 참조
    public int StockNum;             // 현재 입력된 거래 수량
    public TextMeshProUGUI NoticeText;    // 오류 메시지 등을 표시하는 공지 텍스트
    public TextMeshProUGUI PlusText;      // + 버튼 텍스트
    public TextMeshProUGUI MinusText;     // - 버튼 텍스트
    public TextMeshProUGUI Check1Text;    // 구매 여부 안내 텍스트
    public TextMeshProUGUI Check2Text;    // 확인 버튼 텍스트
    public TextMeshProUGUI PurchaseText;  // 현재 수량·금액 표시 텍스트
    public TextMeshProUGUI currentText;   // 최대 구매/판매 가능 수량 표시 텍스트
    public TextMeshProUGUI CancelText;    // 취소 버튼 텍스트
    public bool isBuyMode;           // true: 구매 모드, false: 판매 모드

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

    // 현재 수량·금액 표시 텍스트 갱신
    public void RefreshPurchaseText()
    {
        CalculateTrade(StockNum, isBuyMode, out int endPrice, out int totalAmount);
    
        int startPrice = stockcontrol.stockinfo.StockCurrentPrice;
        float execPrice = (startPrice + endPrice) / 2f;

        PurchaseText.text = $"수량: {StockNum}주\n" +
                            $"현재가: {startPrice} G\n" +
                            $"예상 체결가: {(int)execPrice} G\n" +  // 슬리피지 반영 평균가
                            $"총액: {totalAmount} G";
    }

    // + 버튼 클릭 시 거래 수량 1 증가 및 수량·금액 표시 갱신
    public void OnPlusClick()
    {
        StockNum++;
        RefreshPurchaseText();
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
        RefreshPurchaseText();
    }

    // 확인 버튼 클릭 시 거래 처리
    // 구매 모드: 골드 부족 시 차단 → 골드 차감·보유 수량 증가
    // 판매 모드: 보유 수량 부족 시 차단 → 골드 증가·보유 수량 감소
    // 처리 후 게임 저장 및 패널 전환
    // OrderPanel.cs — OncheckClick() 전체 교체
    public void OncheckClick()
    {
        Stock stock = stockcontrol.stockinfo;

        if (isBuyMode == true)
        {
            CalculateTrade(StockNum, true, out int newPrice, out int totalCost);

        // 잔액 부족 시 구매 차단 (미리보기와 동일한 계산식이라 이제 표시값과 일치함)
            if (GameManager.Instance.currentgold - totalCost < 0)
            {
                NoticeText.text = "You don't have enough money";
                return;
            }

        float totalAvgCost = stockcontrol.avgPurchasePrice * stockcontrol.ownedAmount + totalCost;
        stockcontrol.ownedAmount += StockNum;
        stockcontrol.avgPurchasePrice = totalAvgCost / stockcontrol.ownedAmount;
        GameManager.Instance.UpdateGold(-totalCost);

        stock.StockCurrentPrice = newPrice;
    }
    else
    {
        if (StockNum > stockcontrol.ownedAmount)
        {
            NoticeText.text = "You don't have enough Stocks";
            return;
        }

        CalculateTrade(StockNum, false, out int newPrice, out int totalRevenue);

        GameManager.Instance.UpdateGold(totalRevenue);
        stockcontrol.ownedAmount -= StockNum;

        stock.StockCurrentPrice = newPrice;
    }

    stockcontrol.ownedAmountText.text = $"현재 보유 수량: {stockcontrol.ownedAmount}";
    UIManager.instance.UpdateGoldText();

    UIManager.instance.ReturnToEntire(UIManager.instance.buyPanel.gameObject);
}

    // 취소 버튼 클릭 시 거래 패널 닫고 전체 주식 목록 패널로 복귀
    public void OnCancelClick()
    {
        UIManager.instance.ReturnToEntire(UIManager.instance.buyPanel.gameObject);
    }
    public void CalculateTrade(int amount, bool isBuy, out int endPrice, out int totalAmount)
    {
        Stock stock = stockcontrol.stockinfo;
        int startPrice = stock.StockCurrentPrice;
        float impactRatio = stock.TotalShare > 0 ? (float)amount / stock.TotalShare : 0f;

        endPrice = isBuy
            ? Mathf.Max(1, Mathf.RoundToInt(startPrice * (1 + impactRatio)))
            : Mathf.Max(1, Mathf.RoundToInt(startPrice * (1 - impactRatio)));

        float execPrice = (startPrice + endPrice) / 2f; // 시작가~종료가 평균 = 슬리피지 반영 체결가
        totalAmount = Mathf.RoundToInt(execPrice * amount);
    }
}
