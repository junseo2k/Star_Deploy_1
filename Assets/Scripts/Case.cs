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
    public bool isBigNews;
    public int lastingDay;
    public float[] lowPercent;
    public enum NewsType
    {
        SmallIndustryNews,
        SmallCompanyNews,
        SmallVibeNews,
        BigTriggerNews,
        BigRandomNews
    }
    public NewsType newsType;
    public string triggerKey;
    public int triggervalue;
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
