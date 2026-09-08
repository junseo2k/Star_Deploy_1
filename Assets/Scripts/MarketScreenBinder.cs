using System.Collections.Generic;
using Delusion.UI.MarketCurrent;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// GameManager·Portfolio의 실제 게임 데이터를 시장 탭 화면(CurrentMarketScreen)이 읽는 형식으로 바꿔주는 다리 역할
// 패키지 쪽 코드는 건드리지 않고 이 파일에서만 연결한다 — 패키지를 나중에 갈아끼워도 안 깨지게 하기 위함
// (뉴스 탭의 NewsScreenBinder, 자산 탭의 VaultScreenBinder와 같은 구조)
public class MarketScreenBinder : MonoBehaviour
{
    // 산업 태그(영어) → 화면에 표시할 한글 이름
    // CurrentMarketScreen의 필터 칩 이름과 반드시 똑같아야 필터가 동작한다
    // (전체 / 마법공학 / 광업 / 금융 / 방산 / 농업 / 물류 / 건설 / 제약 / 소비재 / 오락)
    [System.Serializable]
    public class IndustryLabel
    {
        public string industryTag;  // Stock.IndustryTag와 같은 값 (예: Mining)
        public string displayName;  // 화면에 나올 이름 (예: 광업)
    }

    // 종목별 로고 교체 — 비워두면 그 종목의 StockIcon을 그대로 쓴다
    [System.Serializable]
    public class LogoOverride
    {
        public string stockName;    // Stock.StockName과 같은 값
        public Sprite logo;
    }

    [Header("연결")]
    [Tooltip("시장 탭 화면 컴포넌트. 비워두면 씬에서 자동으로 찾는다")]
    public CurrentMarketScreen marketScreen;

    [Header("시대가 없는 기간(휴지기)에 표시할 이름")]
    public string noEraName = "평범한 시대";

    [Header("산업 이름 변환표")]
    public List<IndustryLabel> industryLabels = new List<IndustryLabel>
    {
        new IndustryLabel { industryTag = "Mining",    displayName = "광업" },
        new IndustryLabel { industryTag = "Bank",      displayName = "금융" },
        new IndustryLabel { industryTag = "Army",      displayName = "방산" },
        new IndustryLabel { industryTag = "Food",      displayName = "농업" },
        new IndustryLabel { industryTag = "Magic",     displayName = "마법공학" },
        new IndustryLabel { industryTag = "Clinic",    displayName = "제약" },
        new IndustryLabel { industryTag = "Coopang",   displayName = "물류" },
        new IndustryLabel { industryTag = "Construct", displayName = "건설" },
        new IndustryLabel { industryTag = "Luxury",    displayName = "소비재" },
        new IndustryLabel { industryTag = "Game",      displayName = "오락" },
    };

    [Header("로고 교체 (비워두면 종목의 StockIcon 사용)")]
    public List<LogoOverride> logoOverrides = new List<LogoOverride>();

    [Header("미니 차트에 넣을 최근 기록 개수")]
    [Tooltip("주가 히스토리 뒤에서부터 이 개수만큼 잘라 스파크라인에 넣는다")]
    public int sparklinePoints = 8;

    [Header("종목 목록 스크롤")]
    [Tooltip("마우스 휠 한 번에 스크롤되는 양. 패키지 기본값(42)은 뻑뻑해서 키운다")]
    public float scrollSensitivity = 120f;
    [Tooltip("스크롤바를 항상 보이게 한다 (패키지 기본은 안 쓸 때 숨김)")]
    public bool alwaysShowScrollbar = true;
    [Tooltip("스크롤바 폭(px). 패키지 기본은 7이라 잡기 어렵다")]
    public float scrollbarWidth = 14f;

    [Header("문제 확인용")]
    [Tooltip("시장 탭을 열 때마다 어떤 데이터가 들어갔는지 콘솔에 한 줄 찍는다. 확인이 끝나면 꺼도 된다")]
    public bool showRefreshLog = true;

    // 컴포넌트를 처음 붙였을 때 인스펙터에서 자동으로 채워준다
    void Reset()
    {
        FindMarketScreen();
    }

    void Awake()
    {
        FindMarketScreen();
    }

    // 인스펙터 연결이 비어 있거나, 연결해둔 오브젝트가 지워졌을 때 씬에서 다시 찾는다
    // (UI를 다시 생성하면 예전 오브젝트가 사라져서 연결이 "Missing" 상태로 남는 일이 흔하다)
    void FindMarketScreen()
    {
        if (marketScreen != null) return;

        marketScreen = GetComponentInParent<CurrentMarketScreen>(true);
        if (marketScreen != null) return;

        marketScreen = GetComponentInChildren<CurrentMarketScreen>(true);
        if (marketScreen != null) return;

        marketScreen = FindFirstObjectByType<CurrentMarketScreen>(FindObjectsInactive.Include);
    }

    // 시장 탭을 열 때마다 호출한다 (UIManager.OpenMarketPanel에서 부름)
    public void Refresh()
    {
        FindMarketScreen();

        if (marketScreen == null)
        {
            Debug.LogWarning(
                $"MarketScreenBinder({name}): 씬에서 CurrentMarketScreen을 찾지 못했습니다. " +
                "종목 패널 안에 CurrentMarketScreen 오브젝트가 있는지 확인하세요.", this);
            return;
        }

        GameManager game = GameManager.Instance;
        if (game == null)
        {
            // 여기서 그냥 돌아가면 화면에는 패키지 샘플 데이터(윈드시어 매직스 등)가 그대로 남는다
            Debug.LogWarning("MarketScreenBinder: GameManager를 찾지 못해 시장 탭을 갱신하지 못했습니다.", this);
            return;
        }

        // 상단 상태바 (시대 / 골드 / 날짜)
        string eraName = game.currentEra != null ? game.currentEra.eraName : noEraName;

        List<CurrentMarketCompanyInfo> companies = BuildCompanies(game);

        if (showRefreshLog)
        {
            string firstItem = companies.Count > 0
                ? $" / 첫 종목 {companies[0].companyName} {companies[0].price}G ({companies[0].changePercent:0.0}%)"
                : "";
            Debug.Log($"[시장 탭] 종목 {companies.Count}개 / 시대 {eraName} / 골드 {game.currentgold}G / {game.currentDay}일차{firstItem}", this);
        }

        // 상태만 먼저 넣고(rebuild: false), 종목 목록을 넣으면서 한 번만 화면을 다시 그린다
        marketScreen.SetStatus(eraName, game.currentgold, game.currentDay, false);
        marketScreen.SetCompanies(companies, true);

        // 한글 폰트는 줄 높이가 커서, 패키지가 잡아둔 일부 텍스트 칸(회사명·제목)보다 글자 한 줄이 더 크다.
        // 그러면 TMP가 그 줄을 통째로 안 그려서 글자가 안 보인다 → 칸이 모자란 텍스트만 찾아 세로 여유를 준다.
        FixClippedTexts();

        // 스크롤 감도·관성·스크롤바 표시를 손봐서 목록이 자연스럽게 스크롤되게 한다
        TuneScroll();
    }

    // 패키지가 만든 ScrollRect / Scrollbar를 게임에 맞게 조정한다.
    // 패키지 코드는 안 건드리고, 이미 만들어진 컴포넌트의 값만 바꾼다.
    void TuneScroll()
    {
        if (marketScreen == null) return;

        foreach (ScrollRect scroll in marketScreen.GetComponentsInChildren<ScrollRect>(true))
        {
            scroll.scrollSensitivity = scrollSensitivity;   // 휠 감도 (기본 42 → 120)
            scroll.movementType = ScrollRect.MovementType.Elastic; // 끝에서 살짝 튕기게
            scroll.elasticity = 0.1f;
            scroll.inertia = true;                          // 놓으면 부드럽게 감속
            scroll.decelerationRate = 0.135f;

            if (scroll.verticalScrollbar != null)
            {
                scroll.verticalScrollbarVisibility = alwaysShowScrollbar
                    ? ScrollRect.ScrollbarVisibility.Permanent
                    : ScrollRect.ScrollbarVisibility.AutoHide;

                // 스크롤바(트랙) 폭 넓히기 — 트랙 이미지 오브젝트가 Scrollbar 컴포넌트를 들고 있다
                // 오른쪽 끝 위치는 유지하고 왼쪽으로만 넓힌다 (앵커가 좌상단이라 폭을 키우면 오른쪽으로 삐져나옴)
                RectTransform barRect = scroll.verticalScrollbar.transform as RectTransform;
                if (barRect != null && scrollbarWidth > 0f)
                {
                    float delta = scrollbarWidth - barRect.sizeDelta.x;
                    barRect.sizeDelta = new Vector2(scrollbarWidth, barRect.sizeDelta.y);
                    barRect.anchoredPosition = new Vector2(barRect.anchoredPosition.x - delta, barRect.anchoredPosition.y);
                }
            }
        }
    }

    // 화면 안의 모든 TMP 텍스트를 훑어, 글자는 있는데 한 글자도 안 그려진(= 칸이 낮아 세로로 잘린) 것만 고친다.
    // 원인: Noto Sans KR은 줄 높이가 커서, 패키지가 잡아둔 일부 칸(회사명 35px·제목 55px)보다 글자 한 줄이 크다.
    //       그러면 TMP가 그 줄을 통째로 안 그린다. → 칸 높이만 실제 줄 높이보다 살짝 크게 늘려준다.
    //       줄바꿈·말줄임(…) 설정은 패키지가 잡은 그대로 둬서, 이름이 길면 여전히 "…"로 잘린다.
    void FixClippedTexts()
    {
        if (marketScreen == null) return;

        foreach (TMP_Text text in marketScreen.GetComponentsInChildren<TMP_Text>(true))
        {
            if (string.IsNullOrEmpty(text.text)) continue;

            text.ForceMeshUpdate();
            if (text.textInfo.characterCount != 0) continue; // 잘 보이는 건 건드리지 않는다

            float lineHeight = text.font.faceInfo.lineHeight / text.font.faceInfo.pointSize * text.fontSize;
            RectTransform rect = (RectTransform)text.transform;
            float needed = Mathf.Ceil(lineHeight) + 6f;
            if (rect.rect.height < needed)
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, needed);

            text.ForceMeshUpdate();
        }
    }

    // ── 종목 목록 ────────────────────────────────────────

    // allstock을 화면용 카드 데이터로 바꾼다. 상장폐지된 회사는 목록에서 뺀다
    List<CurrentMarketCompanyInfo> BuildCompanies(GameManager game)
    {
        List<CurrentMarketCompanyInfo> list = new List<CurrentMarketCompanyInfo>();

        if (game.tabmanager == null || game.tabmanager.allstock == null)
            return list;

        foreach (Stock stock in game.tabmanager.allstock)
        {
            if (stock == null || stock.isDelisted) continue;

            // 현재가는 런타임 값이라 아직 계산 전이면 0이다. 그때는 기준가로 대신 보여준다
            int price = stock.StockCurrentPrice > 0 ? stock.StockCurrentPrice : stock.StockBasePrice;

            list.Add(new CurrentMarketCompanyInfo
            {
                companyName = string.IsNullOrEmpty(stock.StockName) ? stock.name : stock.StockName,
                nation = string.IsNullOrEmpty(stock.nation) ? "" : stock.nation,
                industry = ToKoreanIndustry(stock.IndustryTag),
                logoResource = "",                 // 로고는 아래 logoOverride로 직접 넣는다
                logoOverride = FindLogo(stock),
                price = price,
                changePercent = stock.ChangePercent,
                status = BuildStatus(game, stock),
                priceHistory = BuildHistory(stock)
            });
        }
        return list;
    }

    // 카드 아래에 한 줄로 보여줄 상태 문구
    string BuildStatus(GameManager game, Stock stock)
    {
        if (stock.negativeQuarters >= 3)
            return $"부도 위기 ({stock.negativeQuarters}분기째 적자)";

        bool hasBusiness = game.activeBusinesses.Exists(b => b.stockName == stock.StockName);
        if (hasBusiness)
            return "사업 진행 중";

        return "정상 영업";
    }

    // 주가 히스토리(정수)를 스파크라인용 float 목록으로 바꾼다
    // 기록이 아직 없으면 빈 목록 — 스파크라인은 아무것도 그리지 않는다
    List<float> BuildHistory(Stock stock)
    {
        List<float> result = new List<float>();
        if (stock.priceHistory == null || stock.priceHistory.Count == 0)
            return result;

        int start = Mathf.Max(0, stock.priceHistory.Count - Mathf.Max(2, sparklinePoints));
        for (int i = start; i < stock.priceHistory.Count; i++)
            result.Add(stock.priceHistory[i]);
        return result;
    }

    // 산업 태그를 한글 이름으로 바꾼다. 변환표에 없으면 태그를 그대로 보여준다
    string ToKoreanIndustry(string industryTag)
    {
        foreach (IndustryLabel label in industryLabels)
        {
            if (label.industryTag == industryTag)
                return label.displayName;
        }
        return industryTag;
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

    // 카드를 눌렀을 때 종목 상세 창을 연다
    // CurrentMarketScreen의 On Company Clicked 이벤트에 이 메서드를 연결한다
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
