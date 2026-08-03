using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 포트폴리오 패널을 관리하는 클래스
// 보유 주식 목록, 총 평가금액, 평균 매수가 대비 손익률을 표시
public class PortfolioPanel : MonoBehaviour
{
    public Transform portfolioPanel;         // 이 포트폴리오 패널 루트 오브젝트
    public Transform contentParent;          // 주식 항목 프리팹이 생성될 스크롤뷰 Content
    public GameObject rowPrefab;             // 항목 한 줄 프리팹 (Image + TextMeshProUGUI 조합)
    public TextMeshProUGUI totalValueText;   // 보유 주식 총 평가금액 표시 텍스트
    public TextMeshProUGUI totalReturnText;  // 전체 손익률 표시 텍스트

    // 포트폴리오 패널을 열 때 호출 — 보유 주식 항목 생성 및 합계 계산
    public void Open()
    {
        // 이전 항목 제거
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        float totalCurrentValue = 0f;  // 현재 보유 주식 총 평가금액
        float totalPurchaseCost = 0f;  // 평균 매수가 기준 총 매수 원금

        foreach (KeyValuePair<Stock, Portfolio.Holding> pair in Portfolio.All)
        {
            Stock stock = pair.Key;
            Portfolio.Holding holding = pair.Value;

            float currentValue = stock.StockCurrentPrice * holding.amount;
            totalCurrentValue += currentValue;
            totalPurchaseCost += holding.avgPurchasePrice * holding.amount;

            // 손익률 계산: avgPurchasePrice가 0이면 첫 매수 전이므로 0%로 처리
            float returnPct = holding.avgPurchasePrice > 0f
                ? (stock.StockCurrentPrice - holding.avgPurchasePrice) / holding.avgPurchasePrice * 100f
                : 0f;

            GameObject row = Instantiate(rowPrefab, contentParent);
            row.GetComponentInChildren<TextMeshProUGUI>().text =
                $"{stock.StockName}  |  {holding.amount}주  |  " +
                $"{(int)currentValue} G  |  " +
                $"{UIFormat.SignedPercent(returnPct, 3)}";
        }

        totalValueText.text = $"총 평가금액: {(int)totalCurrentValue} G";

        if (totalPurchaseCost > 0f)
        {
            float totalReturn = (totalCurrentValue - totalPurchaseCost) / totalPurchaseCost * 100f;
            totalReturnText.text = $"전체 손익: {UIFormat.SignedPercent(totalReturn, 3)}";
        }
        else
        {
            totalReturnText.text = "전체 손익: -";
        }
    }
}
