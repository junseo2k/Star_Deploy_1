using System.Collections.Generic;
using Delusion.UI.News;
using UnityEngine;

// GameManager의 실제 게임 데이터를 뉴스 탭 화면(DelusionNewsScreen)이 읽는 형식으로 바꿔주는 다리 역할
// 패키지 쪽 코드는 건드리지 않고 이 파일에서만 연결한다 — 패키지를 나중에 갈아끼워도 안 깨지게 하기 위함
public class NewsScreenBinder : MonoBehaviour
{
    // 산업 태그(영어) → 화면에 표시할 한글 이름
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
    public DelusionNewsScreen newsScreen;   // 뉴스 탭 화면 컴포넌트

    [Header("시대가 없는 기간(휴지기)에 표시할 문구")]
    public string noEraName = "평범한 시대";
    [TextArea(1, 2)] public string noEraSummary = "특별히 유행하는 산업이 없는 조용한 시기입니다";

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

    [Header("표시 개수")]
    public int moverCount = 3;   // 급상승/급하락에 각각 몇 개까지 표시할지

    // 뉴스 탭을 열 때마다 호출한다 (UIManager.OpenNewsPanel에서 부름)
    public void Refresh()
    {
        if (newsScreen == null)
        {
            Debug.LogWarning("NewsScreenBinder: News Screen이 연결되지 않았습니다.");
            return;
        }

        GameManager game = GameManager.Instance;
        if (game == null) return;

        DelusionNewsScreenData data = new DelusionNewsScreenData();

        FillStatus(data, game);
        FillEra(data, game);
        FillTodayNews(data, game);
        FillMovers(data, game);
        FillQuarter(data, game);

        newsScreen.SetData(data);   // 데이터를 넣으면 화면이 다시 그려진다
    }

    // ── 상단 상태 (시대 이름 / 골드 / 날짜) ──────────────

    void FillStatus(DelusionNewsScreenData data, GameManager game)
    {
        data.currentEra = game.currentEra != null ? game.currentEra.eraName : noEraName;
        data.currentGold = game.currentgold;
        data.currentDay = game.currentDay;
    }

    // ── 시대 카드 ────────────────────────────────────────

    void FillEra(DelusionNewsScreenData data, GameManager game)
    {
        // 시대별 배경 그림이 아직 한 장뿐이라 배경 없이 단색 카드로 그린다
        data.eraBackgroundResource = "";
        data.industryTrends = new List<NewsIndustryTrend>();

        // 시대와 시대 사이의 휴지기 — 진행도 게이지가 0이 되도록 날짜를 0으로 둔다
        if (game.currentEra == null)
        {
            data.eraName = noEraName;
            data.eraSummary = noEraSummary;
            data.eraDay = 0;
            data.eraEstimatedDays = 0;
            return;
        }

        data.eraName = game.currentEra.eraName;
        data.eraSummary = game.currentEra.eraDescription;
        data.eraEstimatedDays = GameBalance.ERA_DURATION;

        // 시대 시작일 = 종료일 − 전체 길이. 여기서 며칠이 지났는지 구한다
        int eraStartDay = game.eraEndDay - GameBalance.ERA_DURATION;
        data.eraDay = Mathf.Clamp(game.currentDay - eraStartDay, 0, GameBalance.ERA_DURATION);

        AddIndustryTrends(data.industryTrends,
            game.currentEra.boostedIndustries,
            game.currentEra.weakenedIndustries);
    }

    // 상승 산업과 하락 산업을 번갈아 담는다
    // 화면에는 앞에서 4개까지만 나오므로, 한쪽만 잔뜩 채워져 반대쪽이 안 보이는 걸 막는다
    void AddIndustryTrends(List<NewsIndustryTrend> list, string[] boosted, string[] weakened)
    {
        int boostedCount = boosted != null ? boosted.Length : 0;
        int weakenedCount = weakened != null ? weakened.Length : 0;
        int maxCount = Mathf.Max(boostedCount, weakenedCount);

        for (int i = 0; i < maxCount; i++)
        {
            if (i < boostedCount)
                list.Add(new NewsIndustryTrend { industryName = ToKoreanIndustry(boosted[i]), isRising = true });
            if (i < weakenedCount)
                list.Add(new NewsIndustryTrend { industryName = ToKoreanIndustry(weakened[i]), isRising = false });
        }
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

    // ── 오늘의 뉴스 ──────────────────────────────────────

    void FillTodayNews(DelusionNewsScreenData data, GameManager game)
    {
        data.todayNews = new List<NewsCompanyItem>();

        // 1) 오늘 터진 뉴스를 먼저 담는다
        //    ActiveCase.currentDay는 하루 지날 때마다 1씩 늘어나므로 0이면 오늘 터진 것이다
        foreach (ActiveCase active in game.activeNewsList)
        {
            if (active.currentDay != 0) continue;
            data.todayNews.Add(ToNewsItem(game, active, active.displayDescription));
        }

        // 2) 어제 이전에 터져서 아직 진행 중인 이슈를 그 뒤에 이어 붙인다
        //    문구 형식은 예전 IssueScroll이 쓰던 것을 그대로 따른다
        foreach (ActiveCase active in game.activeNewsList)
        {
            if (active.currentDay == 0) continue;
            data.todayNews.Add(ToNewsItem(game, active, $"{active.displayDescription} 남은 시간 : {active.Rday}"));
        }
    }

    // ActiveCase 하나를 화면에 표시할 뉴스 항목으로 바꾼다
    NewsCompanyItem ToNewsItem(GameManager game, ActiveCase active, string description)
    {
        Stock target = FindMainStock(active.caseData);
        string caseName = active.caseData != null ? active.caseData.CaseName : "";

        return new NewsCompanyItem
        {
            companyName = target != null ? target.StockName : caseName,
            headline = caseName,
            description = description,
            changePercent = GetNewsChangePercent(game, active.caseData, target),
            logoOverride = FindLogo(target)
        };
    }

    // 뉴스 옆에 띄울 등락률을 정한다
    // 소형 뉴스는 대표 종목의 등락률을 그대로 쓰면 되지만, 대형 뉴스는 대표 종목이 없어서
    // 예전에는 무조건 0.0%로 나왔다 — 시장이 크게 흔들린 날인데도 화면만 조용해 보였다
    // 대형 뉴스는 산업 단위로 터지므로, 이 뉴스가 실제로 건드린 산업들의 평균 등락률을 대신 보여준다
    float GetNewsChangePercent(GameManager game, Case caseData, Stock target)
    {
        if (target != null) return target.ChangePercent;

        if (caseData == null || caseData.industryRates == null || caseData.industryRates.Length == 0) return 0f;
        if (game == null || game.tabmanager == null || game.tabmanager.allstock == null) return 0f;

        float changeSum = 0f;
        int affectedCount = 0;

        foreach (Stock stock in game.tabmanager.allstock)
        {
            if (stock == null || stock.isDelisted) continue;
            if (!IsAffectedIndustry(caseData, stock.IndustryTag)) continue;

            changeSum += stock.ChangePercent;
            affectedCount++;
        }

        return affectedCount > 0 ? changeSum / affectedCount : 0f;
    }

    // 이 대형 뉴스가 해당 산업을 실제로 건드리는지 — 변동률 네 칸이 전부 0이면 무관한 산업으로 본다
    // 뉴스 에셋들이 산업 10개를 전부 적어두고 관계없는 산업만 0으로 비워두는 방식이라,
    // 이걸 안 거르면 평균이 묽어져서 오락주가 20% 오른 날에도 +3% 정도로 표시된다
    static bool IsAffectedIndustry(Case caseData, string industryTag)
    {
        foreach (Case.IndustryRate rate in caseData.industryRates)
        {
            if (rate.industryTag != industryTag) continue;

            return rate.changePriceMin != 0 || rate.changePriceMax != 0
                || rate.changePriceMinNon != 0 || rate.changePriceMaxNon != 0;
        }
        return false; // 목록에 아예 없는 산업
    }

    // 이 사건에서 대표로 보여줄 종목 하나를 고른다 (영향받는 첫 번째 종목)
    // 대형 뉴스처럼 산업 전체에 걸친 사건은 affectedCase가 비어 있을 수 있어 null을 돌려준다
    Stock FindMainStock(Case caseData)
    {
        if (caseData == null || caseData.affectedCase == null) return null;

        foreach (Stock stock in caseData.affectedCase)
        {
            if (stock != null) return stock;
        }
        return null;
    }

    // ── 급상승 / 급하락 ──────────────────────────────────

    void FillMovers(DelusionNewsScreenData data, GameManager game)
    {
        data.gainers = new List<NewsMoverItem>();
        data.losers = new List<NewsMoverItem>();

        if (game.tabmanager == null || game.tabmanager.allstock == null) return;

        // 상장폐지된 종목은 빼고 변동률이 큰 순서로 줄 세운다
        List<Stock> aliveStocks = new List<Stock>();
        foreach (Stock stock in game.tabmanager.allstock)
        {
            if (stock != null && !stock.isDelisted) aliveStocks.Add(stock);
        }
        aliveStocks.Sort((a, b) => b.ChangePercent.CompareTo(a.ChangePercent));

        // 앞에서부터 오른 종목 — 실제로 오르지 않았으면 넣지 않는다
        for (int i = 0; i < moverCount && i < aliveStocks.Count; i++)
        {
            Stock stock = aliveStocks[i];
            if (stock.ChangePercent <= 0f) break;
            data.gainers.Add(ToMoverItem(stock));
        }

        // 뒤에서부터 내린 종목
        for (int i = 0; i < moverCount && i < aliveStocks.Count; i++)
        {
            Stock stock = aliveStocks[aliveStocks.Count - 1 - i];
            if (stock.ChangePercent >= 0f) break;
            data.losers.Add(ToMoverItem(stock));
        }
    }

    NewsMoverItem ToMoverItem(Stock stock)
    {
        return new NewsMoverItem
        {
            companyName = stock.StockName,
            changePercent = stock.ChangePercent,
            logoOverride = FindLogo(stock)
        };
    }

    // ── 분기 게이지 ──────────────────────────────────────

    void FillQuarter(DelusionNewsScreenData data, GameManager game)
    {
        data.quarterLength = GameBalance.QUARTER_LENGTH;

        // 분기는 QUARTER_LENGTH턴마다 끝난다 (GameManager.IsQuarterEnd와 같은 기준)
        int passedDays = game.currentDay % GameBalance.QUARTER_LENGTH;
        data.daysToQuarter = passedDays == 0 ? 0 : GameBalance.QUARTER_LENGTH - passedDays;
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

    // 뉴스 행이나 급상승/급하락 행을 눌렀을 때 종목 상세 창을 연다
    // DelusionNewsScreen의 On Company Clicked 이벤트에 이 메서드를 연결한다
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
