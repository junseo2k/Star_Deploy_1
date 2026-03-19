using UnityEngine;

public class CaseOccur : MonoBehaviour
{
    public Case[] AllCases;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void CaseOccurMethod(Case caseData)//사건 발생 시 
    {
        StockControl[] allstocks = FindObjectsByType<StockControl>(FindObjectsInactive.Include, FindObjectsSortMode.None);//씬 전체에서 검색해 이 스크립트가 붙은 컴포넌트들을 모두 찾음
        for (int i = 0; i < caseData.affectedCase.Length; i++) //케이스마다 반복
        {
            caseData.affectedCase[i].StockCurrentPrice += caseData.ChangePrice[i];//가격 변동 적용
            if (caseData.affectedCase[i].StockCurrentPrice < 1) //주식 가격이 1 아래로 안가게!
        {
            caseData.affectedCase[i].StockCurrentPrice = 1;
        }
            foreach (StockControl stock in allstocks)
            {
                Debug.Log($"비교중: {stock.stockinfo?.StockName} / {caseData.affectedCase[i]?.StockName}");
                if (stock.stockinfo.StockName == caseData.affectedCase[i].StockName) //가격 변동을 UI에 업데이트
                {
                    stock.UpdatePriceText();
                  
                }
            }
        }
        UIManager.instance.NoticeText.text =  caseData.CaseDescription; //어떤 사건이 발생했는지 공지 띄움
    }
    
}
