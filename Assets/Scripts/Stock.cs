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
    // ── 회사가 굴리는 두 가지 돈: 수입과 금고 ──
    // 매출·비용을 따로 두지 않고 "실제로 남는 돈" 하나만 다룬다 (회계 용어 최소화)

    // 기본 수입 (분기당, 시대 보정 전 — 인스펙터 고정, 런타임에 절대 변경하지 말 것)
    public int baseIncome;

    // 기본 금고 (인스펙터 고정) — 0으로 두면 게임 시작 시 수입 기준으로 자동 산정된다
    // 종목 40개의 금고를 일일이 채우지 않아도 되도록 만든 장치로, 특색을 주고 싶은 회사만 값을 넣으면 된다
    public int baseAsset;

    // baseAsset이 비어 있을 때 금고를 수입의 몇 배로 잡을지
    public const int AUTO_ASSET_MULTIPLIER = 10;

    // 시대 보정까지 적용된 이번 분기 수입 — RecalculateFinancials()가 다시 계산하는 파생값이라 저장하지 않는다
    // 직렬화하면 플레이 중 바뀐 값이 에셋 파일에 눌어붙어 매번 파일이 수정 상태가 된다
    [System.NonSerialized]
    public int income;

    // 사업 성패가 누적되는 런타임 기준값 — 분기 수입 계산은 이 값을 쓴다
    // 인스펙터 원본(baseIncome)을 직접 건드리면 에셋에 값이 눌어붙어 다음 플레이로 새어나간다
    [System.NonSerialized]
    public int runtimeBaseIncome;

    // 회사 금고 — 분기 수입이 쌓이고, 사업 투자비와 배당이 여기서 빠져나간다
    [System.NonSerialized]
    public int runtimeAsset;

    // 지난 분기 수입 — 실적 공시에서 "이번 분기에 얼마나 늘었나"를 계산하는 기준
    [System.NonSerialized]
    public int lastQuarterIncome;

    // ── 목표 주가 산정의 기준선 ──
    // 인스펙터 고정값에서 바로 계산되므로 따로 저장할 필요가 없다
    // 이 기준 대비 지금 수입이 몇 배인지로 목표가를 구하기 때문에, 종목별 주가 배수가 제각각이어도 밸런스가 깨지지 않는다
    public int BaselineIncome => baseIncome;

    // 기준 금고 — baseAsset이 비어 있으면 수입 기준으로 자동 산정 (0으로 나누는 것을 막기 위해 최소 1 보장)
    public int BaselineAsset => baseAsset > 0
        ? baseAsset
        : Mathf.Max(BaselineIncome, 1) * AUTO_ASSET_MULTIPLIER;

    // 시가총액 = 현재가 × 발행주식수
    // int끼리 곱하면 주가가 214,748을 넘는 순간 21억을 넘겨 음수가 되므로, long으로 올린 뒤 곱한다
    // (long)을 곱셈 앞에 붙여야 한다 — 곱한 뒤에 붙이면 이미 넘친 값을 옮기는 것이라 소용없다
    public long marketCap => (long)StockCurrentPrice * TotalShare;
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
    public bool hasDividend;   // 배당주 여부 (true면 분기마다 배당 지급)

    // 배당성향 — 이번 분기 수입 중 주주에게 나눠주는 비율 (예: 0.3 = 수입의 30%)
    // 주가 기준이 아니라 수입 기준이다. 주가가 올랐다고 배당이 따라 오르면 안 되기 때문
    [Range(0f, 0.6f)]
    public float dividendRate;

    // 그래프·NPC 예측에 쓰이는 최근 기록 (GameManager가 매턴 갱신, 저장 대상 아님)
    [System.NonSerialized]
    public List<float> changePctHistory = new List<float>(); // 변동률 히스토리
    [System.NonSerialized]
    public List<int> priceHistory = new List<int>();         // 실제 주가 히스토리
    [System.NonSerialized]
    public List<float> volumeHistory = new List<float>();    // 가짜 거래량 히스토리
}
