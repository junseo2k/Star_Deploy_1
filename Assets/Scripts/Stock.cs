using System.Collections.Generic;
using UnityEngine;

// 주식 정보를 담는 ScriptableObject — 인스펙터에서 각 주식 데이터를 에셋으로 생성·관리
[CreateAssetMenu(fileName = "Stock", menuName = "Scriptable Objects/Stock")]
public class Stock : ScriptableObject
{
    // 기업이 제공하는 서비스/상품 정보를 담는 구조체 (상세 패널 서비스 탭에 표시)
    [System.Serializable]
    public struct ProductInfo
    {
        public string productName; // 서비스/상품 이름
        public Sprite productIcon; // 서비스/상품 아이콘 이미지
    }
    public string StockName;        // 주식 이름
    public int StockBasePrice;      // 변동 전 최초 기준 가격
    public string StockDescription; // 주식 설명
    public string StockDetailDescription; // 상세 패널에 표시되는 기업 상세 설명
    public Sprite StockIcon;        // 주식 아이콘 이미지
    public string FamilyTag;        // 주식 분류 태그 — 종족 (예: Dwarf, Elf, Dragon, Human)
    public string IndustryTag;      // 주식 분류 태그 — 산업 (예: Mining, Army)
    public int revenue;             // 기업 매출액
    public int OperatingProfit;     // 기업 영업이익
    public int baseRevenue;         // 기본 매출 (시대 보정 전, 인스펙터 고정)
    public int baseCost;            // 기본 비용 (인스펙터 고정)

    public int marketCap => StockCurrentPrice * TotalShare;
    public int TotalShare;          // 발행 주식 총 수량
    public string[] RecentIssue;    // 최근 주요 이슈 목록 (상세 패널 이슈 탭에 표시)
    public ProductInfo[] products;          // 기업 서비스/상품 목록 (상세 패널 서비스 탭에 표시)
    public bool isFavorite;   // 즐겨찾기 상태 (SaveGame에서 저장)
    [System.NonSerialized] // 저장 안 되는 런타임 전용
    public List<Case> runtimeIssues = new List<Case>();
    public enum VolatilityType
    {
        Stable,   // 안정형: -1.5% ~ +1.5%
        Normal,   // 보통형: -2.5% ~ +2.5%
        Volatile  // 고변동형: -4% ~ +4%
    }

    public VolatilityType volatilityType; // 각 주식마다 설정
    public string nation;             // 이 주식이 속한 국가 이름 (대형 뉴스 당사국 판정에 사용)
    [System.NonSerialized]
    public int StockCurrentPrice;     // 현재 주식 가격 (런타임에만 존재, 저장 안 됨)
    [System.NonSerialized]
    public int StockPreviousPrice;    // 하루 전 주식 가격 (변동률 계산 기준)
    [System.NonSerialized]
    public float StockChangePrice;    // 이번 턴 가격 변동률 (NPC 학습 데이터 입력에 사용)

    // 전일 대비 가격 변동률(%) — 전일 가격이 0 이하면 0 반환 (0으로 나누기 방지)
    public float ChangePercent => StockPreviousPrice > 0
        ? (float)(StockCurrentPrice - StockPreviousPrice) / StockPreviousPrice * 100f
        : 0f;
    public bool hasDividend;   // 배당주 여부 (true면 매턴 배당금 지급)
    [Range(0f, 0.05f)]
    public float dividendRate; // 배당률 (예: 0.02 = 현재가의 2%)

    // 그래프·NPC 예측에 쓰이는 최근 기록 (GameManager가 매턴 갱신, 저장 대상 아님)
    [System.NonSerialized]
    public List<float> changePctHistory = new List<float>(); // 변동률 히스토리
    [System.NonSerialized]
    public List<int> priceHistory = new List<int>();         // 실제 주가 히스토리
    [System.NonSerialized]
    public List<float> volumeHistory = new List<float>();    // 가짜 거래량 히스토리
}
