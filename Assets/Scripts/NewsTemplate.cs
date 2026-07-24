using UnityEngine;

// 뉴스 템플릿 - 사건 문구와 대상 기업, 변동률 범위를 미리 저장해두는 데이터
[CreateAssetMenu(fileName = "NewsTemplate", menuName = "Scriptable Objects/NewsTemplate")]
public class NewsTemplate : ScriptableObject
{
    // 사건 문구 하나 = 텍스트 + 대상 회사 + 방향 + 이 사건만의 변동률 범위
    // targetStock을 이벤트 안에 직접 넣어서, 문구와 회사가 항상 같이 붙어 다니게 함
    [System.Serializable]
    public struct EventEntry
    {
        public Stock targetStock; // 이 사건이 실제로 일어나는 회사 (문구가 회사 전용이라 직접 지정)
        public string eventText;  // 예: "블레이즈마인 광산에서 대규모 붕괴 사고가 발생했다"
        public int changeMin;     // 이 사건의 변동률(%) 최솟값 — 음수면 하락, 양수면 상승
        public int changeMax;     // 이 사건의 변동률(%) 최댓값 — 음수면 하락, 양수면 상승
        public bool isUncertain;  // false=확정 뉴스(즉시 적용), true=불확정 뉴스(몇 턴 뒤 효과 발동)
    }

    public EventEntry[] events;   // 이 산업군에 속한 모든 회사의 사건을 한 풀에 모아둠

    public int lastingDay = 2;    // 생성된 뉴스의 기본 지속일
    public float[] lowPercent = { 1f, 0.4f };  // 날짜별 감쇠율
}