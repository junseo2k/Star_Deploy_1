using UnityEngine;

[CreateAssetMenu(fileName = "Stock", menuName = "Scriptable Objects/Stock")]
public class Stock : ScriptableObject //주식 정보 모음.
{
    public string StockName; //주식 이름
    public int StockBasePrice; //변동하기 전 최초 주식 가격
    public string StockDescription;//주식 설명
    public Sprite StockIcon;//주식 이미지
    public string FamilyTag;//주식 태그:종족
    public string IndustryTag;//주식 태그:산업
    public int StockCurrentPrice;//주식 현재 가격
}
