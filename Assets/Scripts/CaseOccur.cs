using UnityEngine;

public class CaseOccur : MonoBehaviour
{
    public Case DwarfEarthQuakeData;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void CaseOccurMethod(Case caseData)
    {
        StockControl[] allstocks = FindObjectsByType<StockControl>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < caseData.affectedCase.Length; i++)
        {
            caseData.affectedCase[i].StockCurrentPrice += caseData.ChangePrice[i];
            if (caseData.affectedCase[i].StockCurrentPrice < 1)
        {
            caseData.affectedCase[i].StockCurrentPrice = 1;
        }
            foreach (StockControl stock in allstocks)
            {
                Debug.Log($"비교중: {stock.stockinfo?.StockName} / {caseData.affectedCase[i]?.StockName}");
                if (stock.stockinfo.StockName == caseData.affectedCase[i].StockName)
                {
                    stock.UpdatePriceText();
                  
                }
            }
        }
        UIManager.instance.NoticeText.text =  caseData.CaseDescription;
    }
    public void OnDwarfEarthQuakeButton()
    {
        CaseOccurMethod(DwarfEarthQuakeData);
    }
}
