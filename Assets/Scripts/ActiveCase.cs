using UnityEngine;
using System.Collections.Generic;

// 현재 진행 중인 뉴스 사건의 런타임 상태를 나타내는 클래스
// Case ScriptableObject는 고정 데이터이고, 이 클래스는 해당 사건이 활성화된 동안의 상태를 추적
public class ActiveCase
{
    public Case caseData;           // 이 활성 사건의 원본 데이터 (ScriptableObject)
    public int Rday;                // 사건 종료까지 남은 날 수 (0 이하가 되면 activeNewsList에서 제거)
    public int currentDay;          // 사건 발생 후 경과 날 수 (lowPercent 감쇠율 인덱스로 사용)
    public string displayDescription; // 국가 이름이 치환된 최종 뉴스 텍스트 (예: "[국가]" → "마노사")
    public List<string> involvedNations = new List<string>(); // 이 사건에 관련된 국가 목록 (당사국/비당사국 구분에 사용)
}
