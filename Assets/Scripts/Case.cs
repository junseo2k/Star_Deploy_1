using UnityEngine;

[CreateAssetMenu(fileName = "Case", menuName = "Scriptable Objects/Case")]
public class Case : ScriptableObject//사건 정보 모음
{
    public string CaseName; //사건 이름
    public string CaseDescription;//사건 묘사
    public Stock[] affectedCase;//변동 주식 모음.
    public int[] ChangePrice;// 변동 가격 모음
}
