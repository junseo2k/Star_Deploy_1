using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 포트폴리오 패널을 관리하는 클래스
// 보유 주식 목록, 총 평가금액, 평균 매수가 대비 손익률을 표시
public class PortfolioPanel : MonoBehaviour
{
    public Transform portfolioPanel;    // 이 포트폴리오 패널 루트 오브젝트
    public Transform contentParent;     // 주식 항목 프리팹이 생성될 스크롤뷰 Content
    public GameObject rowPrefab;        // StockWithImagePrefab —TextMeshProUGUI 조합 프리팹
    public TextMeshProUGUI totalValueText;   // 보유 주식 총 평가금액 표시 텍스트
    public TextMeshProUGUI totalReturnText;  // 전체 손익률 표시 텍스트

    // 포트폴리오 패널을 열 때 호출 — 보유 주식 항목 생성 및 합계 계산
    public void Open()
    {
        // 이전 항목 제거
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        StockControl[] allStocks = FindObjectsByType<StockControl>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        float totalCurrentValue = 0f;  // 현재 보유 주식 총 평가금액
        float totalPurchaseCost = 0f;  // 평균 매수가 기준 총 매수 원금

        foreach (StockControl sc in allStocks)
        {
            if (sc.ownedAmount <= 0) continue;

            float currentValue = sc.stockinfo.StockCurrentPrice * sc.ownedAmount;
            float purchaseCost = sc.avgPurchasePrice * sc.ownedAmount;

            // 손익률 계산: avgPurchasePrice가 0이면 첫 매수 전이므로 0%로 처리
            float returnPct = sc.avgPurchasePrice > 0f
                ? (sc.stockinfo.StockCurrentPrice - sc.avgPurchasePrice) / sc.avgPurchasePrice * 100f
                : 0f;

            string sign = returnPct >= 0f ? "+" : "";
            string color = returnPct >= 0f ? "#FF4444" : "#4488FF"; // 상승 빨강, 하락 파랑

            // 행 프리팹 생성 후 이미지·텍스트 설정
            GameObject row = Instantiate(rowPrefab, contentParent);
            TextMeshProUGUI rowText = row.GetComponentInChildren<TextMeshProUGUI>();
            rowText.text = $"{sc.stockinfo.StockName}  |  {sc.ownedAmount}주  |  " +
                           $"{(int)currentValue} G  |  " +
                           $"<color={color}>{sign}{returnPct:F3}%</color>";

            totalCurrentValue += currentValue;
            totalPurchaseCost += purchaseCost;
        }

        // 총 평가금액
        totalValueText.text = $"총 평가금액: {(int)totalCurrentValue} G";

        // 전체 손익률
        if (totalPurchaseCost > 0f)
        {
            float totalReturn = (totalCurrentValue - totalPurchaseCost) / totalPurchaseCost * 100f;
            string sign = totalReturn >= 0f ? "+" : "";
            string color = totalReturn >= 0f ? "#FF4444" : "#4488FF";
            totalReturnText.text = $"전체 손익: <color={color}>{sign}{totalReturn:F3}%</color>";
        }
        else
        {
            totalReturnText.text = "전체 손익: -";
        }
    }

    // 닫기 버튼 클릭 시 포트폴리오 패널 닫고 전체 주식 목록 패널로 복귀
    public void OnCloseClick()
    {
        UIManager.instance.ReturnToEntire(portfolioPanel.gameObject);
    }
}
