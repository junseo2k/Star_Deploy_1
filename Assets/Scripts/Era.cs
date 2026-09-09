using UnityEngine;

// 시대(유행 산업) 데이터를 담는 ScriptableObject
// 특정 산업은 가격이 오르고, 특정 산업은 내려가는 효과를 줌
[CreateAssetMenu(fileName = "Era", menuName = "Scriptable Objects/Era")]
public class Era : SaveIdentity
{
    public string eraName;              // 시대 이름 (예: "마법공학의 시대")
    public string eraDescription;       // 시대 시작 시 공지될 설명 텍스트

    public string[] boostedIndustries;  // 유망 산업 태그 목록 (가격 상승)
    public string[] weakenedIndustries; // 비선호 산업 태그 목록 (가격 하락)

    public float boostedRate;           // 유망 산업 하루 보정 배율 (예: 0.5 = +0.5%)
    public float weakenedRate;          // 비선호 산업 하루 보정 배율 (예: 0.5 = -0.5%)

    public Case hintNews;               // 시대 전환 전에 보여줄 힌트 뉴스 (Case 재사용)
}
