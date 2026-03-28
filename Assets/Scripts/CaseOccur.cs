using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

// 사건 발생 처리를 담당하는 클래스 — 사건에 따른 주식 가격 변동과 UI 공지를 처리
public class CaseOccur : MonoBehaviour
{
    public Case[] AllCases; // 게임에 등록된 모든 사건 목록 (GameManager에서 랜덤 선택에 사용)
    public DetailPanel detailpanel; // 상세 패널 참조 (사건 발생 시 이슈 탭 갱신에 활용)
      
    void Start()
    {

    }

    void Update()
    {

    }

    // 사건 발생 시 호출 — 해당 사건의 영향을 받는 주식 가격을 변동하고 UI를 갱신
    public void CaseOccurMethod(Case caseData)
    {
        // 씬 전체에서 StockControl 컴포넌트가 붙은 모든 오브젝트를 검색 (비활성화 포함)
        StockControl[] allstocks = FindObjectsByType<StockControl>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < caseData.affectedCase.Length; i++) // 사건마다 영향 주식 반복 처리
        {
            // 주식 가격에 변동량 적용
            
            caseData.affectedCase[i].StockCurrentPrice = Mathf.RoundToInt(caseData.affectedCase[i].StockCurrentPrice * (1 + caseData.ChangePrice[i]/100f));
            caseData.affectedCase[i].runtimeIssues.Add(caseData);
            caseData.affectedCase[i].StockChangePrice = caseData.ChangePrice[i];
            // 주식 가격이 1 미만으로 내려가지 않도록 하한선 설정
            if (caseData.affectedCase[i].StockCurrentPrice < 1)
            {
                caseData.affectedCase[i].StockCurrentPrice = 1;
            }

            // 변경된 주식에 해당하는 StockControl UI 텍스트를 갱신
            foreach (StockControl stock in allstocks)
            {
                Debug.Log($"비교중: {stock.stockinfo?.StockName} / {caseData.affectedCase[i]?.StockName}");
                if (stock.stockinfo.StockName == caseData.affectedCase[i].StockName)
                {
                    stock.UpdatePriceText();
                    
                }
            }
            
        }
        
        
        

        // 사건 설명을 공지 텍스트에 표시
        UIManager.instance.NoticeText.text = caseData.CaseDescription;
    }
}
