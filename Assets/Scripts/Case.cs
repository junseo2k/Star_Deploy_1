using UnityEngine;

// 사건 정보를 담는 ScriptableObject — 인스펙터에서 각 사건 데이터를 에셋으로 생성·관리
[CreateAssetMenu(fileName = "Case", menuName = "Scriptable Objects/Case")]
public class Case : ScriptableObject
{
    public string CaseName;        // 사건 이름
    public string CaseDescription; // 사건 발생 시 공지될 설명 텍스트
    public Stock[] affectedCase;   // 이 사건으로 가격이 변동되는 주식 목록
    public int[] ChangePriceMin;      // affectedCase의 각 주식에 적용할 가격 변동률(%) 최솟값 (인덱스 1:1 대응)
    public int[] ChangePriceMax;
    [System.Serializable]
    public struct ChainEffect
    {
        public Stock stock;   // 연쇄 효과를 받는 주식
        public int priceMin;  // 연쇄 효과 최소 변동률 (%)
        public int priceMax;  // 연쇄 효과 최대 변동률 (%)
    }
    public ChainEffect[] chainEffects; // 연쇄 효과 목록 — 뉴스 2일차에 발동
    public Sprite CaseImage;
    public int lastingDay;          // 사건이 지속되는 날 수 (ActiveCase.Rday 초기값으로 사용)
    public float[] lowPercent;      // 날짜별 가격 변동 감쇠율 배열 (0일차: lowPercent[0], 1일차: lowPercent[1] …)
    public enum NewsType
    {
        SmallIndustryNews,  // 소형 산업 뉴스 — 특정 산업 주식에 영향
        SmallCompanyNews,   // 소형 기업 뉴스 — 특정 기업 주식에 영향
        BigTriggerNews,     // 대형 뉴스 — 30~50턴 주기로 무작위 예약
        ForeshadowNews      // 전조 뉴스 — 대형 뉴스 발생 2~3턴 전 자동 표시
    }
    public NewsType newsType;
    public Case[] foreshadowCases; // BigTriggerNews 전용 — 발생 전 전조 뉴스 후보 목록 (랜덤 1개 선택)
    public string[] nationPairs; // 접경국 쌍 ("마노사,알브레이" 형태)
    [System.Serializable]
    public struct IndustryRate
    {
        public string industryTag;      // 산업군 태그 (Army, Clinic 등)
        public int changePriceMin;      // 당사국 최소 변동률
        public int changePriceMax;      // 당사국 최대 변동률
        public int changePriceMinNon;   // 비당사국 최소 변동률
        public int changePriceMaxNon;   // 비당사국 최대 변동률
    }
    public IndustryRate[] industryRates; // 대형 뉴스 전용 — 산업별 당사국/비당사국 변동률 설정 목록
}
