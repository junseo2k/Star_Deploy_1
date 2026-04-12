using UnityEngine;

// 사건 정보를 담는 ScriptableObject — 인스펙터에서 각 사건 데이터를 에셋으로 생성·관리
[CreateAssetMenu(fileName = "Case", menuName = "Scriptable Objects/Case")]
public class Case : ScriptableObject
{
    public string CaseName;        // 사건 이름
    public string CaseDescription; // 사건 발생 시 공지될 설명 텍스트
    public Stock[] affectedCase;   // 이 사건으로 가격이 변동되는 주식 목록
    public int[] ChangePriceMin;      // affectedCase의 각 주식에 적용할 가격 변동량 (인덱스 1:1 대응)
    public int[] ChangePriceMax;
    public Sprite CaseImage;
    public bool isBigNews;          // 대형 뉴스 여부 (UI 표시 방식 분기에 사용)
    public int lastingDay;          // 사건이 지속되는 날 수 (ActiveCase.Rday 초기값으로 사용)
    public float[] lowPercent;      // 날짜별 가격 변동 감쇠율 배열 (0일차: lowPercent[0], 1일차: lowPercent[1] …)
    public enum NewsType
    {
        SmallIndustryNews,  // 소형 산업 뉴스 — 특정 산업 주식에 영향
        SmallCompanyNews,   // 소형 기업 뉴스 — 특정 기업 주식에 영향
        SmallVibeNews,      // 소형 분위기 뉴스 — 트리거 수치 누적용
        BigTriggerNews,     // 대형 트리거 뉴스 — triggerKey 누적치가 100 이상일 때 발동
        BigRandomNews       // 대형 랜덤 뉴스 — 일정 확률로 무작위 발생
    }
    public NewsType newsType;       // 이 사건의 뉴스 분류 (처리 방식·목록 분류에 사용)
    public string triggerKey;       // 트리거 키워드 (예: "전쟁", "평화") — 누적 수치 증감 및 BigTriggerNews 발동 조건
    public int triggervalue;        // 이 사건 발생 시 해당 triggerKey 수치에 더해지는 값
    public string[] nationPairs; // 접경국 쌍 ("마노사,알브레이" 형태)
    public int[] ChangePriceMinNon;  // 비당사국 최소 변동률
    public int[] ChangePriceMaxNon;
    [System.Serializable]
    public struct IndustryRate
    {
        public string industryTag;      // 산업군 태그 (Army, Clinic 등)
        public int changePriceMin;      // 당사국 최소 변동률
        public int changePriceMax;      // 당사국 최대 변동률
        public int changePriceMinNon;   // 비당사국 최소 변동률
        public int changePriceMaxNon;   // 비당사국 최대 변동률
    }
    public IndustryRate[] industryRates;
}
