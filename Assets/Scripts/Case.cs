using UnityEngine;

[CreateAssetMenu(fileName = "Case", menuName = "Scriptable Objects/Case")]
public class Case : ScriptableObject
{
    public string CaseName;
    public string CaseDescription;
    public Stock[] affectedCase;
    public int[] ChangePrice;
}
