using UnityEngine;

[CreateAssetMenu(fileName = "Stock", menuName = "Scriptable Objects/Stock")]
public class Stock : ScriptableObject
{
    public string StockName;
    public int StockBasePrice;
    public string StockDescription;
    public Sprite StockIcon;
    public string FamilyTag;
    public string IndustryTag;
    public int StockCurrentPrice;
}
