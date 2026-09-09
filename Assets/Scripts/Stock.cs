using System.Collections.Generic;
using UnityEngine;

// 주식 정보를 담는 ScriptableObject — 인스펙터에서 각 주식 데이터를 에셋으로 생성·관리
[CreateAssetMenu(fileName = "Stock", menuName = "Scriptable Objects/Stock")]
public class Stock : SaveIdentity
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
    // ── 정체성 — 누가 만든 회사인가 ──
    // 읽을거리이자 뉴스 분위기의 재료다. 수입·주가 계산에는 일절 쓰지 않는다
    public enum OwnerType
    {
        Family,   // 가문 소유 — 대를 이어온 이야기
        Guild,    // 길드 소유 — 조합원들의 공동 소유
        Commoner, // 평민 소유 — 맨손으로 일군 회사
        Royal     // 왕실 소유 — 나라가 뒤를 봐준다
    }
    public OwnerType owner;

    // ── 체질 — 평소에 어떻게 버는가 ──
    // 매출과 운영비의 차이가 회사 성격을 만든다. 운영비가 무거울수록 시대에 크게 휘둘린다
    public enum CompanyType
    {
        Stable,   // 안정형: 운영비가 가볍다. 꾸준히 흑자지만 크게 뛰지도 않는다
        Resource, // 자원형: 매출이 크다. 흑자지만 시대에 따라 오르내린다
        HighRisk, // 고위험형: 매출도 운영비도 크다. 흑자와 적자를 오간다
        Growth    // 성장형: 평소엔 적자. 사업 성공과 좋은 시대가 겹쳐야 크게 번다
    }
    public CompanyType companyType;

    // 성격 — 번 돈을 어디에 쓰는가. 평소 행동의 90%를 여기서 정한다 (기획 문서 "체질 — 성격" 참고)
    // 예전 이름은 PayoutStyle(Dividend/Reinvest) 2종이었다. FormerlySerializedAs로 필드 이름을 바꿔도
    // 기존 40개 종목에 이미 입력된 값(0=Dividend, 1=Reinvest)이 그대로 DividendLover/BusinessManiac으로 이어진다
    public enum Personality
    {
        DividendLover,  // 배당 성애자: 배당을 꾸준히 챙긴다. 사업엔 소극적
        BusinessManiac, // 사업 미치광이: 번 돈을 계속 새 사업에 쏟는다. 금고가 항상 얇다
        VaultKeeper     // 금고 지기: 배당도 사업도 최소화하고 금고에 현금을 쌓아둔다
    }
    [UnityEngine.Serialization.FormerlySerializedAs("payoutStyle")]
    public Personality personality;

    // ── 활동 — 이 회사가 벌일 수 있는 사업들 ──
    // 예전엔 사업(BusinessProject) 쪽이 회사 하나를 가리켰다. 그러면 회사마다 사업이 몇 개든
    // 착수 확률이 사업 개수만큼 배가돼서 사업 많은 회사가 유리해진다. 그래서 방향을 뒤집는다:
    // 회사가 사업 목록을 들고, 착수 판정도 회사당 한 번만 한다 (GameManager.TryStartBusiness)
    public BusinessProject[] businesses;

    // ── 회사가 굴리는 돈: 매출 − 운영비 = 수입, 그리고 금고 ──

    // 평소 매출 (분기당, 시대 보정 전 — 인스펙터 고정, 런타임에 절대 변경하지 말 것)
    // 사업 안 해도 장사로 들어오는 돈이라 항상 양수다. 시대 배율은 이 값에만 곱한다
    public int baseRevenue;

    // 운영비 (분기당 고정 지출 — 월급, 유지비)
    // 시대가 좋든 나쁘든 똑같이 나간다. 그래서 이게 무거운 회사일수록 수입이 크게 흔들린다
    public int operatingCost;

    // 기본 금고 (인스펙터 고정) — 0으로 두면 게임 시작 시 운영비 기준으로 자동 산정된다
    // 종목 40개의 금고를 일일이 채우지 않아도 되도록 만든 장치로, 특색을 주고 싶은 회사만 값을 넣으면 된다
    public int baseAsset;

    // 시대 보정까지 적용된 이번 분기 수입 — RecalculateFinancials()가 다시 계산하는 파생값이라 저장하지 않는다
    // 직렬화하면 플레이 중 바뀐 값이 에셋 파일에 눌어붙어 매번 파일이 수정 상태가 된다
    [System.NonSerialized]
    public int income;

    // 사업 성패가 누적되는 런타임 매출 — 분기 수입 계산은 이 값을 쓴다
    // 사업에 성공하면 여기가 영구히 올라간다. 즉 "회사가 지금까지 키운 실력"에 해당한다
    // 인스펙터 원본(baseRevenue)을 직접 건드리면 에셋에 값이 눌어붙어 다음 플레이로 새어나간다
    [System.NonSerialized]
    public int runtimeRevenue;

    // 회사 금고 — 분기 수입이 쌓이고, 사업 투자비와 배당이 여기서 빠져나간다
    [System.NonSerialized]
    public int runtimeAsset;

    // 금고가 연속으로 마이너스인 분기 수 — 플러스로 돌아오면 0으로 초기화된다
    // "위기에서 극적으로 살아나는" 이야기가 여기서 나온다
    [System.NonSerialized]
    public int negativeQuarters;

    // 상장폐지 여부 — 한 번 켜지면 이 회사는 게임에서 더 이상 살아있는 종목으로 취급되지 않는다
    [System.NonSerialized]
    public bool isDelisted;

    // 지난 분기 수입 — 실적 공시에서 "이번 분기에 얼마나 늘었나"를 계산하는 기준
    [System.NonSerialized]
    public int lastQuarterIncome;

    // ── 목표 주가 산정의 기준선 ──
    // 인스펙터 고정값에서 바로 계산되므로 따로 저장할 필요가 없다
    // 게임 시작 시점 대비 지금 실적이 얼마나 나아졌는지로 목표가를 구하기 때문에,
    // 종목별 주가 배수가 제각각이어도 밸런스가 깨지지 않는다

    // 기준 매출 — 목표 주가 계산에서 나누는 쪽으로 쓰인다. 항상 양수여야 부호가 뒤집히지 않는다
    public int BaselineRevenue => baseRevenue;

    // 기준 수입 — 게임 시작 시점의 분기 수입. 성장형은 이 값이 음수일 수 있다
    public int BaselineIncome => BaselineRevenue - operatingCost;

    // 체질별로 "금고가 몇 분기치 운영비를 담고 시작하는가"
    // 나쁜 체질일수록 두껍게 준다 — 평소 적자여도 좋은 시대가 올 때까지 버텨야 하기 때문
    public int SurvivalQuarters
    {
        get
        {
            switch (companyType)
            {
                case CompanyType.Stable: return 4;
                case CompanyType.Resource: return 5;
                case CompanyType.HighRisk: return 8;
                case CompanyType.Growth: return 10;
                default: return 4;
            }
        }
    }

    // 기준 금고 — baseAsset이 비어 있으면 "몇 분기를 버티는가"로 자동 산정
    // 수입이 아니라 운영비를 기준으로 삼는 이유: 적자 회사도 음수가 끼어들지 않아 계산이 안전하다
    public int BaselineAsset => baseAsset > 0 ? baseAsset : MoneyMath.Int((long)operatingCost * SurvivalQuarters);

    // 시가총액 = 현재가 × 발행주식수
    // int끼리 곱하면 주가가 214,748을 넘는 순간 21억을 넘겨 음수가 되므로, long으로 올린 뒤 곱한다
    // (long)을 곱셈 앞에 붙여야 한다 — 곱한 뒤에 붙이면 이미 넘친 값을 옮기는 것이라 소용없다
    public long marketCap => (long)StockCurrentPrice * TotalShare;
    public int TotalShare;          // 발행 주식 총 수량
    public ProductInfo[] products;          // 기업 서비스/상품 목록 (상세 패널 서비스 탭에 표시)
    public bool isFavorite;   // 즐겨찾기 상태 (SaveGame에서 저장)
    [System.NonSerialized] // 저장 안 되는 런타임 전용
    public List<Case> runtimeIssues = new();
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
    // 배당 여부는 이제 hasDividend 같은 별도 플래그가 아니라 personality == DividendLover 하나로 정해진다
    // (예전엔 종목 40개 중 이 플래그를 실제로 켠 게 1개뿐이라 배당 시스템이 사실상 죽어 있었다)

    // 배당성향 — 이번 분기 수입 중 주주에게 나눠주는 비율 (예: 0.3 = 수입의 30%)
    // 주가 기준이 아니라 수입 기준이다. 주가가 올랐다고 배당이 따라 오르면 안 되기 때문
    // 0으로 비워두면 GameBalance.DIVIDEND_RATE_DEFAULT를 대신 쓴다 — 배당 성애자 40개를 일일이 채우지 않아도 되게 하는 장치
    [Range(0f, 0.6f)]
    public float dividendRate;

    // 그래프·NPC 예측에 쓰이는 최근 기록 (GameManager가 매턴 갱신, 저장 대상 아님)
    [System.NonSerialized]
    public List<float> changePctHistory = new(); // 변동률 히스토리
    [System.NonSerialized]
    public List<int> priceHistory = new();         // 실제 주가 히스토리
    [System.NonSerialized]
    public List<float> volumeHistory = new();    // 가짜 거래량 히스토리
}
