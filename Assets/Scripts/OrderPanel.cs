using TMPro;
using UnityEngine;

// 주식 구매/판매 확인 패널을 관리하는 클래스
// 수량 조절 버튼(+/-), 확인/취소 버튼 처리를 담당
public class OrderPanel : MonoBehaviour
{
    public Stock targetStock;            // 거래 대상 주식 (DetailPanel이 패널을 열 때 지정)
    public int StockNum;                 // 현재 입력된 거래 수량
    public TextMeshProUGUI NoticeText;   // 오류 메시지 등을 표시하는 공지 텍스트
    public TextMeshProUGUI PlusText;     // + 버튼 텍스트
    public TextMeshProUGUI MinusText;    // - 버튼 텍스트
    public TextMeshProUGUI Plus10Text;     // + 버튼 텍스트
    public TextMeshProUGUI Minus10Text;    // - 버튼 텍스트
    public TextMeshProUGUI MaxText;    // - 버튼 텍스트
    public TextMeshProUGUI Check1Text;   // 구매 여부 안내 텍스트
    public TextMeshProUGUI Check2Text;   // 확인 버튼 텍스트
    public TextMeshProUGUI PurchaseText; // 현재 수량·금액 표시 텍스트
    public TextMeshProUGUI currentText;  // 최대 구매/판매 가능 수량 표시 텍스트
    public TextMeshProUGUI CancelText;   // 취소 버튼 텍스트
    public bool isBuyMode;               // true: 구매 모드, false: 판매 모드
    public int maxTradeAmount;           // 이번 거래에서 가능한 최대 수량 (구매: 최대 구매가능수량 / 판매: 보유량)

    private const int MIN_TRADE_AMOUNT = 1;  // 한 번에 거래할 수 있는 최소 수량
    private const int STEP_10 = 10;          // 10단위 버튼이 한 번에 움직이는 수량

    // 구매 패널 열릴 때 UI 텍스트 초기값 설정
    void Start()
    {
        PlusText.text = "+";
        MinusText.text = "-";
        Plus10Text.text = "+10";
        Minus10Text.text = "-10";
        MaxText.text = "최대";
        Check1Text.text = "Purchase?";
        Check2Text.text = "Check";
        NoticeText.text = "";
        CancelText.text = "Cancel";
    }

    // 거래 대상과 모드를 지정하고 패널 표시를 초기화 — DetailPanel에서 호출
    public void Setup(Stock stock, bool buyMode, int maxAmount)
    {
        targetStock = stock;
        isBuyMode = buyMode;
        maxTradeAmount = maxAmount;
        StockNum = MIN_TRADE_AMOUNT;
        NoticeText.text = "";
        currentText.text = $"Maximum: {maxAmount}";
        RefreshPurchaseText();
        
    }

    // 현재 수량·금액 표시 텍스트 갱신
    public void RefreshPurchaseText()
    {
        if (targetStock == null) return;

        CalculateTrade(StockNum, isBuyMode, out int endPrice, out int totalAmount);

        int startPrice = targetStock.StockCurrentPrice;
        float execPrice = (startPrice + endPrice) / 2f;

        PurchaseText.text = $"수량: {StockNum}주\n" +
                            $"현재가: {startPrice} G\n" +
                            $"예상 체결가: {(int)execPrice} G\n" + // 슬리피지 반영 평균가
                            $"총액: {totalAmount} G";
    }

    // + 버튼 클릭 시 거래 수량 1 증가 및 수량·금액 표시 갱신
    public void OnPlusClick()
    {
        StockNum++;
        RefreshPurchaseText();
    }

    // - 버튼 클릭 시 거래 수량 1 감소
    // 최소 수량 미만으로는 내려가지 않도록 경고 메시지를 출력하고 감소를 막음
    public void OnMinusClick()
    {
        if (StockNum <= MIN_TRADE_AMOUNT)
        {
            NoticeText.text = isBuyMode
                ? "You should buy at least 1 Stock."
                : "You should sell at least 1 Stock.";
            return;
        }

        StockNum--;
        RefreshPurchaseText();
    }

    // +10 버튼 클릭 시 거래 수량 10 증가 — 최대 거래 가능 수량을 넘지 않도록 잘라준다
    public void OnPlus10Click()
    {
        SetStockNum(StockNum + STEP_10);
    }

    // -10 버튼 클릭 시 거래 수량 10 감소 — 최소 수량(1) 아래로는 내려가지 않는다
    public void OnMinus10Click()
    {
        SetStockNum(StockNum - STEP_10);
    }

    // 최대 버튼 클릭 시 거래 가능한 최대 수량으로 한 번에 맞춘다
    // 구매 모드면 "지금 골드로 살 수 있는 최대 수량", 판매 모드면 "보유량 전부"
    public void OnMaxClick()
    {
        int effectiveMax = Mathf.Max(MIN_TRADE_AMOUNT, maxTradeAmount);
        SetStockNum(effectiveMax);

        if (maxTradeAmount <= 0)
            NoticeText.text = isBuyMode
                ? "You can't afford even 1 Stock."
                : "You don't own any Stock.";
    }

    // 거래 수량을 [최소, 최대] 범위로 잘라서 반영하고 표시를 갱신하는 공통 처리
    void SetStockNum(int value)
    {
        int effectiveMax = Mathf.Max(MIN_TRADE_AMOUNT, maxTradeAmount);
        StockNum = Mathf.Clamp(value, MIN_TRADE_AMOUNT, effectiveMax);
        RefreshPurchaseText();
    }

    // 확인 버튼 클릭 시 거래 처리
    // 구매 모드: 골드 부족 시 차단 → 골드 차감·보유 수량 증가
    // 판매 모드: 보유 수량 부족 시 차단 → 골드 증가·보유 수량 감소
    public void OncheckClick()
    {
        if (targetStock == null) return;

        bool traded = isBuyMode ? TryBuy() : TrySell();
        if (!traded) return;

        StockControl.RefreshAllOwnedText();
        UIManager.instance.UpdateGoldText();
        UIManager.instance.ReturnToEntire(UIManager.instance.buyPanel.gameObject);
    }

    // 구매 처리 — 성공 시 true, 잔액 부족으로 차단되면 false
    bool TryBuy()
    {
        CalculateTrade(StockNum, true, out int newPrice, out int totalCost);

        // 잔액 부족 시 구매 차단 (미리보기와 동일한 계산식이라 표시값과 일치함)
        if (GameManager.Instance.currentgold < totalCost)
        {
            NoticeText.text = "You don't have enough money";
            return false;
        }

        int owned = Portfolio.GetAmount(targetStock);
        float totalAvgCost = Portfolio.GetAvgPurchasePrice(targetStock) * owned + totalCost;
        int newOwned = owned + StockNum;

        Portfolio.SetHolding(targetStock, newOwned, totalAvgCost / newOwned);
        GameManager.Instance.UpdateGold(-totalCost);
        targetStock.StockCurrentPrice = newPrice;
        return true;
    }

    // 판매 처리 — 성공 시 true, 보유 수량 부족으로 차단되면 false
    bool TrySell()
    {
        int owned = Portfolio.GetAmount(targetStock);
        if (StockNum > owned)
        {
            NoticeText.text = "You don't have enough Stocks";
            return false;
        }

        CalculateTrade(StockNum, false, out int newPrice, out int totalRevenue);

        GameManager.Instance.UpdateGold(totalRevenue);
        Portfolio.SetAmount(targetStock, owned - StockNum);
        targetStock.StockCurrentPrice = newPrice;
        return true;
    }

    // 취소 버튼 클릭 시 거래 패널 닫고 전체 주식 목록 패널로 복귀
    public void OnCancelClick()
    {
        UIManager.instance.ReturnToEntire(UIManager.instance.buyPanel.gameObject);
    }

    // 거래 수량에 따른 체결 후 가격(endPrice)과 총 거래대금(totalAmount)을 계산
    // 수량이 많을수록 가격이 밀리는 슬리피지를 반영 — 체결가는 시작가와 종료가의 평균
    public void CalculateTrade(int amount, bool isBuy, out int endPrice, out int totalAmount)
    {
        int startPrice = targetStock.StockCurrentPrice;
        float impactRatio = targetStock.TotalShare > 0 ? (float)amount / targetStock.TotalShare : 0f;

        endPrice = isBuy
            ? Mathf.Max(1, Mathf.RoundToInt(startPrice * (1 + impactRatio)))
            : Mathf.Max(1, Mathf.RoundToInt(startPrice * (1 - impactRatio)));

        float execPrice = (startPrice + endPrice) / 2f;
        totalAmount = Mathf.RoundToInt(execPrice * amount);
    }
}
