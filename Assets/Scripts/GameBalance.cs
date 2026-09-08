// 게임 전체의 밸런스 숫자를 한곳에 모아둔 파일 — 코드 로직과 "숫자 손잡이"를 분리하기 위한 것
// 값 하나를 조절하고 싶을 때는 이 파일만 열면 된다. 이 숫자가 실제로 어떻게 쓰이는지 보고 싶으면
// GameManager.Business.cs / GameManager.Finance.cs / GameManager.Era.cs 등 해당 로직 파일을 열면 된다
// (회사마다 다른 값 — dividendRate 같은 것 — 은 여기 없다. 그건 코드가 아니라 각 Stock 에셋의
//  Inspector 값이라, Assets/Editor/BalanceDataExporter.cs가 만드는 CSV로 훑어보는 쪽이 낫다)
public static class GameBalance
{
    // ── 골드 / 대출 / 게임 오버 ────────────────────────
    public const int START_GOLD = 60000;             // 새 게임 시작 골드 (주가 4배 스케일에 맞춰 상향)
    public const int LOW_GOLD_THRESHOLD = 1000;      // 이 골드 이하가 연속되면 게임 오버 카운트 증가
    public const int LOW_GOLD_GAMEOVER_STREAK = 5;   // 게임 오버까지의 연속 턴 수
    public const float LOAN_INTEREST_RATE = 0.01f;   // 턴당 대출 이자율 (1%)
    public const int LOAN_MAX_RATIO_DIVISOR = 2;     // 대출 한도 = 보유 골드 / 이 값 (50%)

    // ── 뉴스 시스템 ────────────────────────────────────
    public const int SMALL_NEWS_MIN_INTERVAL = 3;    // 소형 뉴스 최소 발생 주기 (턴)
    public const int SMALL_NEWS_MAX_INTERVAL = 6;    // 소형 뉴스 최대 발생 주기 (상한 제외)
    public const int UNCERTAIN_NEWS_DELAY = 2;       // 불확정 뉴스 효과가 나타나기까지 걸리는 턴 수
    public const int BIG_NEWS_MIN_INTERVAL = 30;     // 대형 뉴스 최소 예약 간격 (턴)
    public const int BIG_NEWS_MAX_INTERVAL = 51;     // 대형 뉴스 최대 예약 간격 (상한 제외)
    public const int FORESHADOW_DEFAULT_LEAD = 2;    // 전조 뉴스 지속일이 없을 때 사용할 기본 선행 턴 수

    // ── 시대 시스템 ────────────────────────────────────
    public const int ERA_FIRST_START_DAY = 20;       // 시대 시스템 최초 발생 가능 날짜
    // 시대 지속 턴 수 — 분기(QUARTER_LENGTH=20턴)보다 길게 잡아야 시대 중에 분기 마감이 최소 한 번은 지나간다
    // (사업 착수 판정은 분기 마감에만 일어나므로, 시대가 분기보다 짧으면 시대 사업 보정이 걸릴 틈이 없다)
    public const int ERA_DURATION = 24;              // 시대 지속 턴 수
    public const int ERA_COOLDOWN = 8;               // 시대 종료 후 휴지기 턴 수
    public const int ERA_HINT_DAYS_BEFORE = 2;       // 힌트 뉴스를 며칠 전에 보여줄지
    public const float ERA_REVENUE_BOOST = 1.2f;     // 유망 산업 매출 배율
    public const float ERA_REVENUE_PENALTY = 0.8f;   // 비선호 산업 매출 배율
    // 시대는 매출뿐 아니라 그 산업 회사들의 "새 사업 착수 의욕"에도 영향을 준다
    // 예: 제약의 시대 → 제약 회사들이 평소보다 적극적으로 신사업을 벌인다 (반대 산업은 위축)
    public const float ERA_BUSINESS_CHANCE_BOOST = 1.5f;   // 유망 산업 사업 착수 확률 배율
    public const float ERA_BUSINESS_CHANCE_PENALTY = 0.6f; // 비선호 산업 사업 착수 확률 배율

    // ── 사업 시스템 ────────────────────────────────────
    // 분기 마감마다 회사 하나하나가 새 사업을 벌일 확률 — 성격에 따라 다르게 준다
    // 사업 미치광이는 번 돈을 다음 사업에 넣는 게 정체성이라 가장 자주 벌이고,
    // 배당 성애자는 배당을 우선하느라 신중하고, 금고 지기는 아예 쌓아두는 게 목적이라 제일 소극적이다
    public const float BUSINESS_START_CHANCE_MANIAC = 0.35f;
    public const float BUSINESS_START_CHANCE_DIVIDEND = 0.18f;
    public const float BUSINESS_START_CHANCE_VAULT = 0.10f;
    // 사업 착수 후에도 금고에 운영비 이만큼의 분기치는 남겨야 한다 — 안전 마진 없이 다 지르면 전부 부도난다
    public const int BUSINESS_SAFETY_MARGIN_QUARTERS = 2;

    // ── 배당 시스템 ────────────────────────────────────
    // 배당 여부는 성격(Personality.DividendLover) 하나로 정해진다. dividendRate를 종목마다
    // 일일이 채우지 않아도 되도록, 비워둔(0) 종목은 이 기본값을 대신 쓴다
    public const float DIVIDEND_RATE_DEFAULT = 0.25f;

    // ── 뜬금 이벤트 ────────────────────────────────────
    // 성격이 만드는 뻔한 평소 패턴 위에 가끔 얹히는 예상 밖의 한 방. 게임 한 판(20분기)에
    // 회사당 1~2번 정도 터지도록, 분기당 확률을 낮게 잡았다 (20분기 × 0.07 ≈ 1.4회)
    public const float SURPRISE_EVENT_CHANCE = 0.07f;
    public const float DIVIDEND_BOMB_MULTIPLIER = 4f;    // 배당 성애자: 특별 배당이 평소 배당의 몇 배인가
    public const float DIVIDEND_BOMB_PRICE_BUMP = 0.08f; // 특별 배당 발표 시 주가가 즉시 뛰는 비율
    public const float MEGA_BUSINESS_SCALE = 2.5f;       // 사업 미치광이: 초대형 사업의 규모 배율
    public const float VAULT_SURPRISE_SCALE = 2f;        // 금고 지기: 깜짝 대형 투자의 규모 배율

    // ── 분기 / 회사 금고 / 재무 기반 주가 ──────────────
    public const int QUARTER_LENGTH = 20;             // 분기 길이 (턴) — 400턴이면 20분기

    // 분기 마감일에 그 분기 수입이 금고로 들어간다. 배당과 사업 투자비는 여기서 빠져나간다
    public const float QUARTER_SHOCK = 0.30f;         // 실적 발표일에 목표 주가 쪽으로 한 번에 좁히는 비율 (30%)
    public const float QUARTER_REPORT_THRESHOLD = 3f; // 이 수치(%) 미만의 수입 변화는 공시하지 않는다 (알림 도배 방지)
    // 주가를 움직이는 엔진은 수입 하나다. 금고는 회사의 체력이지 가치 평가 요소가 아니므로 주가에 넣지 않는다
    // 금고를 주가에 반영하면 배당을 줄수록·사업을 시작할수록 주가가 떨어지는 역설이 생긴다
    // 실적이 좋아진 것을 목표 주가에 얼마나 세게 반영할지 정하는 손잡이
    // "매출의 몇 %만큼 수입이 늘었나"에 이 값을 곱해 주가 배수를 만든다. 클수록 실적에 예민해진다
    // 40개 매출값을 채운 지금(Step 6), 안정형(운영비 30%)과 자원형(60%) 사이에서 체감이 맞게 2.0으로 올린다
    public const float PROFIT_SENSITIVITY = 2.0f;
    public const float FAIR_PRICE_MIN_RATIO = 0.3f;   // 목표 주가 하한 (기준가의 30% — 적자가 심해도 0으로 떨어지지 않게)
    public const float FAIR_PRICE_MAX_RATIO = 3.0f;   // 목표 주가 상한 (기준가의 300% — 사업 연속 성공으로 무한히 오르지 않게)

    // 금고가 마이너스라고 바로 부도나지 않는다 — 버티는 기간을 준다
    public const int SOLVENCY_CRISIS_QUARTERS = 3;    // 이 분기 이상 연속 마이너스면 "부도 위기" 경고
    public const int SOLVENCY_DELIST_QUARTERS = 5;    // 이 분기 이상 연속 마이너스면 상장폐지

    // ── 매일 가격 변동 ─────────────────────────────────
    public const float DAILY_BIAS = 0.15f;            // 매일 가격에 더해지는 상승 편향 (%)
    public const float RANGE_STABLE = 0.5f;           // 안정형 일일 변동폭 (±%)
    public const float RANGE_NORMAL = 1f;             // 보통형 일일 변동폭 (±%)
    public const float RANGE_VOLATILE = 1.5f;         // 고변동형 일일 변동폭 (±%)
    public const float RANGE_DEFAULT = 2f;            // 변동 유형이 없을 때의 기본값
    public const int MIN_STOCK_PRICE = 4;            // 주가 하한 (0 이하로 내려가지 않도록 — 주가 4배 스케일에 맞춰 상향)

    // ── 그래프 / 학습 데이터 ───────────────────────────
    // 그래프는 이만큼 쌓여야 선이 그려진다 — 그래프 탭 잠금 기준일로도 쓰이므로 DetailPanel에서도 참조한다
    public const int HISTORY_LENGTH = 10;             // 그래프·NPC 예측에 쓰는 최근 기록 보관 일수
    public const float FAKE_VOLUME_SCALE = 50f;       // 가짜 거래량 계수 (변동폭 절댓값 × 이 값)
}
