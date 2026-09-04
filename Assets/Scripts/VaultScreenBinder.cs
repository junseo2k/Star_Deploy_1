using System.Collections.Generic;
using Delusion.UI.Vault;
using UnityEngine;

// GameManager·Portfolio의 실제 게임 데이터를 자산 탭 화면(DelusionVaultScreen)이 읽는 형식으로 바꿔주는 다리 역할
// 패키지 쪽 코드는 건드리지 않고 이 파일에서만 연결한다 — 패키지를 나중에 갈아끼워도 안 깨지게 하기 위함
// (뉴스 탭의 NewsScreenBinder와 같은 구조)
public class VaultScreenBinder : MonoBehaviour
{
    // 종목별 로고 교체 — 비워두면 그 종목의 StockIcon을 그대로 쓴다
    [System.Serializable]
    public class LogoOverride
    {
        public string stockName;    // Stock.StockName과 같은 값
        public Sprite logo;
    }

    [Header("연결")]
    [Tooltip("자산 탭 화면 컴포넌트. 비워두면 씬에서 자동으로 찾는다")]
    public DelusionVaultScreen vaultScreen;

    [Header("시대가 없는 기간(휴지기)에 표시할 이름")]
    public string noEraName = "평범한 시대";

    [Header("총 손익 기준")]
    [Tooltip("예전 방식(시작 자산 대비) 계산용 값. 지금은 화면에 쓰이지 않는다 — 손익은 매수 원금 대비로 계산한다")]
    public int startingAssetsOverride = 0;

    [Header("대주주 판정 기준 (발행 주식 수 대비 보유 비율 %)")]
    [Tooltip("이 비율 이상 가지고 있으면 대주주 목록에 올라간다")]
    public float majorShareholderPercent = 5f;
    [Tooltip("이 비율 이상이면 '최대주주' 배지를 단다")]
    public float largestShareholderPercent = 10f;

    [Header("로고 교체 (비워두면 종목의 StockIcon 사용)")]
    public List<LogoOverride> logoOverrides = new List<LogoOverride>();

    [Header("문제 확인용")]
    [Tooltip("자산 탭을 열 때마다 어떤 데이터가 들어갔는지 콘솔에 한 줄 찍는다. 확인이 끝나면 꺼도 된다")]
    public bool showRefreshLog = true;

    // 컴포넌트를 처음 붙였을 때 인스펙터에서 자동으로 채워준다
    void Reset()
    {
        FindVaultScreen();
    }

    void Awake()
    {
        FindVaultScreen();
    }

    // 인스펙터 연결이 비어 있거나, 연결해둔 오브젝트가 지워졌을 때 씬에서 다시 찾는다
    // (UI를 다시 생성하면 예전 오브젝트가 사라져서 연결이 "Missing" 상태로 남는 일이 흔하다)
    void FindVaultScreen()
    {
        if (vaultScreen != null) return;

        // 1) 나와 부모 쪽에서 먼저 찾는다 (바인더를 화면 오브젝트의 자식으로 두는 경우가 많다)
        vaultScreen = GetComponentInParent<DelusionVaultScreen>(true);
        if (vaultScreen != null) return;

        // 2) 내 자식 중에서 찾는다 (꺼져 있는 오브젝트도 포함)
        vaultScreen = GetComponentInChildren<DelusionVaultScreen>(true);
        if (vaultScreen != null) return;

        // 3) 그래도 없으면 씬 전체에서 찾는다
        vaultScreen = FindFirstObjectByType<DelusionVaultScreen>(FindObjectsInactive.Include);
    }

    // 자산 탭을 열 때마다 호출한다 (UIManager.OpenPortfolioPanel에서 부름)
    public void Refresh()
    {
        FindVaultScreen();

        if (vaultScreen == null)
        {
            // 두 번째 인자로 this를 넘기면 콘솔에서 이 경고를 눌렀을 때 Hierarchy에서 해당 오브젝트가 반짝인다
            // → 어느 VaultScreenBinder가 문제인지 바로 확인할 수 있다
            Debug.LogWarning(
                $"VaultScreenBinder({name}): 씬에서 DelusionVaultScreen을 찾지 못했습니다. " +
                "자산 패널 안에 DelusionVaultScreen 오브젝트가 있는지 확인하세요.", this);
            return;
        }

        GameManager game = GameManager.Instance;
        if (game == null)
        {
            // 여기서 그냥 돌아가면 화면에는 패키지 샘플 데이터(윈드시어 매직스 등)가 그대로 남는다
            Debug.LogWarning("VaultScreenBinder: GameManager를 찾지 못해 자산 탭을 갱신하지 못했습니다.", this);
            return;
        }

        DelusionVaultScreenData data = new DelusionVaultScreenData();

        // 상단 상태 (기본 설정에서는 화면에 안 그리지만, 상태바를 켜면 이 값이 쓰인다)
        data.currentEra = game.currentEra != null ? game.currentEra.eraName : noEraName;
        data.currentGold = game.currentgold;
        data.currentDay = game.currentDay;

        // 자산 요약 — 총 자산과 손익은 아래 RecalculateSummary가 직접 계산한다
        data.cash = game.currentgold;
        data.debt = Mathf.Max(0, game.loanAmount);                      // 갚아야 할 대출금 (총 자산에서 빠진다)
        data.purchaseCost = Mathf.RoundToInt(Portfolio.TotalPurchaseCost()); // 주식을 살 때 쓴 돈 (손익률의 기준)
        data.startingAssets = startingAssetsOverride > 0 ? startingAssetsOverride : GameBalance.START_GOLD;

        data.holdings = BuildHoldings();
        data.majorShareholders = BuildMajorShareholders();

        if (showRefreshLog)
        {
            // 무엇이 들어갔는지 한 줄로 남긴다 — 목록이 비었는지, 가격이 0인지 여기서 바로 보인다
            string firstItem = data.holdings.Count > 0
                ? $" / 첫 종목 {data.holdings[0].companyName} {data.holdings[0].quantity}주, 평단 {data.holdings[0].averagePrice}G, 현재가 {data.holdings[0].currentPrice}G"
                : "";
            Debug.Log(
                $"[자산 탭] 보유 {data.holdings.Count}종목 / 현금 {data.cash}G / 대출 {data.debt}G / 매수원금 {data.purchaseCost}G{firstItem}",
                this);
        }

        // 데이터만 먼저 넣고(rebuild: false), 합계를 계산하면서 한 번만 다시 그린다
        vaultScreen.SetData(data, false);
        vaultScreen.RecalculateSummary(true);
    }

    // ── 보유 종목 ────────────────────────────────────────

    // Portfolio에 들어 있는 보유 주식을 화면용 목록으로 바꾼다
    // 평가액 순 정렬은 DelusionVaultScreen이 알아서 하므로 여기서는 순서를 신경 쓰지 않는다
    List<VaultHoldingItem> BuildHoldings()
    {
        List<VaultHoldingItem> list = new List<VaultHoldingItem>();

        foreach (KeyValuePair<Stock, Portfolio.Holding> pair in Portfolio.All)
        {
            Stock stock = pair.Key;
            Portfolio.Holding holding = pair.Value;
            if (stock == null || holding.amount <= 0) continue;

            // 현재가는 런타임 값이라 아직 계산 전이면 0이다. 그때는 기준가로 대신 보여준다
            // (0이면 평가액도 0이 되어 "평가액 많은 순" 정렬이 의미 없어진다)
            int currentPrice = stock.StockCurrentPrice > 0 ? stock.StockCurrentPrice : stock.StockBasePrice;

            list.Add(new VaultHoldingItem
            {
                companyName = string.IsNullOrEmpty(stock.StockName) ? stock.name : stock.StockName,
                quantity = holding.amount,
                // 평단은 소수점까지 관리되지만 화면에는 정수 골드로만 보여준다
                averagePrice = Mathf.RoundToInt(holding.avgPurchasePrice),
                currentPrice = currentPrice,
                logoOverride = FindLogo(stock)
            });
        }
        return list;
    }

    // ── 대주주 현황 ──────────────────────────────────────

    // 지분율 = 내 보유 수량 ÷ 발행 주식 총 수량 × 100
    // 아직 NPC 지분 데이터가 없으므로 "누가 1등인가"는 판정할 수 없다.
    // 대신 지분율이 largestShareholderPercent를 넘으면 최대주주 배지를 다는 방식으로 처리한다
    List<VaultMajorShareholderItem> BuildMajorShareholders()
    {
        List<VaultMajorShareholderItem> list = new List<VaultMajorShareholderItem>();

        foreach (KeyValuePair<Stock, Portfolio.Holding> pair in Portfolio.All)
        {
            Stock stock = pair.Key;
            Portfolio.Holding holding = pair.Value;
            if (stock == null || holding.amount <= 0) continue;
            if (stock.TotalShare <= 0) continue;   // 발행 주식 수가 없으면 지분율을 못 구한다

            float ownershipPercent = holding.amount / (float)stock.TotalShare * 100f;
            if (ownershipPercent < majorShareholderPercent) continue;

            list.Add(new VaultMajorShareholderItem
            {
                companyName = stock.StockName,
                ownershipPercent = Mathf.Clamp(ownershipPercent, 0f, 100f),
                isLargestShareholder = ownershipPercent >= largestShareholderPercent,
                logoOverride = FindLogo(stock)
            });
        }

        // 지분이 큰 회사부터 보여준다
        list.Sort((a, b) => b.ownershipPercent.CompareTo(a.ownershipPercent));
        return list;
    }

    // ── 로고 ────────────────────────────────────────────

    // 교체 목록에 등록된 로고가 있으면 그걸 쓰고, 없으면 종목의 StockIcon을 쓴다
    Sprite FindLogo(Stock stock)
    {
        if (stock == null) return null;

        foreach (LogoOverride entry in logoOverrides)
        {
            if (entry.logo != null && entry.stockName == stock.StockName)
                return entry.logo;
        }
        return stock.StockIcon;
    }

    // ── 종목 클릭 ────────────────────────────────────────

    // 보유 종목 줄이나 대주주 카드를 눌렀을 때 종목 상세 창을 연다
    // DelusionVaultScreen의 On Company Clicked 이벤트에 이 메서드를 연결한다
    public void OpenStockDetail(string stockName)
    {
        GameManager game = GameManager.Instance;
        if (game == null || game.tabmanager == null || game.tabmanager.allstock == null) return;

        foreach (Stock stock in game.tabmanager.allstock)
        {
            if (stock != null && stock.StockName == stockName)
            {
                UIManager.instance.ShowDetail(stock);
                return;
            }
        }
    }
}
