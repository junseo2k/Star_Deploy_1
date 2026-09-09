using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public partial class GameManager
{
    private bool savingBlocked;
    private bool isSimulating;
    private readonly List<Case> restoredCases = new();
    public static string GetSavePath(int slot) => Path.Combine(Application.persistentDataPath, $"save{slot}.json");

    public void SaveGame()
    {
        if (!savingBlocked && !isSimulating) TrySaveToSlot(PlayerPrefs.GetInt("SelectedSlot", 1));
    }
    public void AutoSave() => SaveGame();

    public bool TrySaveToSlot(int slot)
    {
        if (slot < 1 || isSimulating) return false;
        try
        {
            GameSaveStorage.Write(GetSavePath(slot), CaptureSave());
            savingBlocked = false;
            return true;
        }
        catch (Exception e) when (GameSaveStorage.IsFileError(e) || e is InvalidOperationException)
        {
            ShowPersistenceNotice("저장하지 못했습니다. 기존 파일은 유지됩니다. " + e.Message);
            return false;
        }
    }

    void ShowPersistenceNotice(string message)
    {
        Debug.LogWarning(message);
        if (UIManager.instance != null && UIManager.instance.NoticeText != null)
            UIManager.instance.NoticeText.text = message;
    }

    GameSaveData CaptureSave()
    {
        var data = new GameSaveData
        {
            version = GameSaveStorage.CurrentVersion,
            currentgold = currentgold, currentday = currentDay, loanAmount = loanAmount,
            smallNewsTimer = smallNewsTimer, eraEndDay = eraEndDay,
            nextEraStartDay = nextEraStartDay, eraHintShownDay = eraHintShownDay,
            currentEraName = currentEra != null ? currentEra.eraName : "",
            pendingEraName = pendingEra != null ? pendingEra.eraName : "",
            currentEraId = currentEra != null ? currentEra.SaveId : "",
            pendingEraId = pendingEra != null ? pendingEra.SaveId : "",
            activeBusinesses = activeBusinesses,
            lastBigNewsDay = lastBigNewsDay, scheduledBigNewsDay = scheduledBigNewsDay,
            scheduledBigNewsVibeDay = scheduledBigNewsVibeDay,
            scheduledBigNewsCaseIndex = scheduledBigNewsCaseIndex,
            scheduledForeshadowIndex = scheduledForeshadowIndex,
            scheduledNationPair = scheduledNationPair, OwnStocks = new List<StockSaveData>(),
            notices = new List<string>(dailyNotices), quarterReport = new List<string>(quarterReportLines),
            quarterReportOpen = UIManager.instance != null && UIManager.instance.quarterReportPanel != null
                && UIManager.instance.quarterReportPanel.gameObject.activeSelf,
        };
        if (scheduledBigNewsCaseIndex >= 0 && scheduledBigNewsCaseIndex < BigTriggerCases.Count)
        {
            Case scheduled = BigTriggerCases[scheduledBigNewsCaseIndex];
            data.scheduledCaseId = scheduled.SaveId;
            if (scheduled.foreshadowCases != null && scheduledForeshadowIndex >= 0 && scheduledForeshadowIndex < scheduled.foreshadowCases.Length)
                data.scheduledHintId = scheduled.foreshadowCases[scheduledForeshadowIndex]?.SaveId;
        }
        foreach (Stock stock in tabmanager.allstock)
        {
            if (stock == null) continue;
            if (string.IsNullOrEmpty(stock.SaveId)) throw new InvalidOperationException("종목 저장 ID 누락: " + stock.name);
            data.OwnStocks.Add(new StockSaveData
            {
                stockId = stock.SaveId, stockname = stock.StockName,
                currentPrice = stock.StockCurrentPrice, previousPrice = stock.StockPreviousPrice,
                isFavorite = stock.isFavorite, income = stock.income, revenue = stock.runtimeRevenue,
                asset = stock.runtimeAsset, negativeQuarters = stock.negativeQuarters,
                isDelisted = stock.isDelisted, ownamount = Portfolio.GetAmount(stock),
                avgPurchasePrice = Portfolio.GetAvgPurchasePrice(stock), lastQuarterIncome = stock.lastQuarterIncome,
                lastEraModifier = lastQuarterEraModifier.TryGetValue(stock.StockName, out float m) ? m : 1f,
                lastRevenue = lastQuarterRevenue.TryGetValue(stock.StockName, out int r) ? r : stock.runtimeRevenue,
                priceHistory = new List<int>(stock.priceHistory), changeHistory = new List<float>(stock.changePctHistory),
                volumeHistory = new List<float>(stock.volumeHistory),
            });
        }
        foreach (var active in activeNewsList) data.news.Add(CaptureNews(active.caseData, active, 0));
        foreach (var pending in pendingNewsList) data.news.Add(CaptureNews(pending.caseData, null, pending.daysLeft));
        foreach (var npc in FindObjectsByType<NPCInvestor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            data.npcs.Add(npc.CaptureSave());
        return data;
    }

    public Stock FindSavedStock(string id, string oldName = null) => Array.Find(tabmanager.allstock, s => s != null &&
        (!string.IsNullOrEmpty(id) ? s.SaveId == id : !string.IsNullOrEmpty(oldName) && s.StockName == oldName));

    void LoadGame(string path = null)
    {
        Portfolio.Clear();
        ResetStocksToBase();
        activeNewsList.Clear(); pendingNewsList.Clear(); activeBusinesses.Clear();
        dailyNotices.Clear(); quarterReportLines.Clear(); lastQuarterEraModifier.Clear(); lastQuarterRevenue.Clear();
        currentgold = GameBalance.START_GOLD; currentDay = 1; loanAmount = 0;
        currentEra = pendingEra = null; eraEndDay = -1;
        nextEraStartDay = GameBalance.ERA_FIRST_START_DAY; eraHintShownDay = -1;
        lastBigNewsDay = scheduledBigNewsDay = scheduledBigNewsVibeDay = -1;
        scheduledBigNewsCaseIndex = scheduledForeshadowIndex = -1; smallNewsTimer = 0;
        path ??= GetSavePath(PlayerPrefs.GetInt("SelectedSlot", 1));
        if (!File.Exists(path) && !File.Exists(path + ".bak")) return;
        if (!GameSaveStorage.TryRead(path, out GameSaveData data, out string message))
        {
            savingBlocked = true; // 손상 파일을 새 게임의 자동 저장으로 덮지 않는다.
            ShowPersistenceNotice(message);
            return;
        }
        currentgold = data.currentgold; currentDay = data.currentday; loanAmount = data.loanAmount;
        lastBigNewsDay = data.lastBigNewsDay; scheduledBigNewsDay = data.scheduledBigNewsDay;
        scheduledBigNewsVibeDay = data.scheduledBigNewsVibeDay; scheduledNationPair = data.scheduledNationPair ?? "";
        smallNewsTimer = data.smallNewsTimer; eraEndDay = data.eraEndDay;
        nextEraStartDay = data.nextEraStartDay > 0 ? data.nextEraStartDay : GameBalance.ERA_FIRST_START_DAY;
        eraHintShownDay = data.eraHintShownDay;
        currentEra = FindSavedEra(data.currentEraId, data.currentEraName);
        pendingEra = FindSavedEra(data.pendingEraId, data.pendingEraName);
        RestoreSchedule(data);
        foreach (StockSaveData saved in data.OwnStocks)
        {
            Stock stock = FindSavedStock(saved.stockId, saved.stockname);
            if (stock == null) continue;
            stock.StockCurrentPrice = saved.currentPrice;
            stock.StockPreviousPrice = data.version > 0 && saved.previousPrice > 0 ? saved.previousPrice : saved.currentPrice;
            stock.isFavorite = saved.isFavorite && !saved.isDelisted;
            stock.income = saved.income; stock.runtimeRevenue = saved.revenue;
            stock.runtimeAsset = saved.asset; stock.negativeQuarters = saved.negativeQuarters;
            stock.isDelisted = saved.isDelisted; stock.lastQuarterIncome = saved.lastQuarterIncome;
            RestoreHistory(stock.priceHistory, saved.priceHistory); RestoreHistory(stock.changePctHistory, saved.changeHistory);
            RestoreHistory(stock.volumeHistory, saved.volumeHistory);
            lastQuarterEraModifier[stock.StockName] = saved.lastEraModifier;
            lastQuarterRevenue[stock.StockName] = data.version > 0 ? saved.lastRevenue : saved.revenue;
            if (!stock.isDelisted) Portfolio.SetHolding(stock, saved.ownamount, saved.avgPurchasePrice);
        }
        foreach (var business in data.activeBusinesses ?? new List<ActiveBusiness>())
        {
            Stock stock = FindSavedStock(business.stockId, business.stockName);
            if (stock == null || stock.isDelisted) continue;
            business.stockId = stock.SaveId; business.stockName = stock.StockName;
            activeBusinesses.Add(business);
        }
        foreach (var news in data.news ?? new List<NewsSaveData>()) RestoreNews(news);
        foreach (var npc in FindObjectsByType<NPCInvestor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var saved = data.npcs?.Find(n => n.id == npc.SaveId);
            if (saved != null) npc.RestoreSave(saved, this);
        }
        dailyNotices.AddRange(data.notices ?? new List<string>());
        quarterReportLines.AddRange(data.quarterReport ?? new List<string>());
        if (UIManager.instance != null)
        {
            UIManager.instance.NoticeText.text = string.Join("\n", dailyNotices);
            if (data.quarterReportOpen && UIManager.instance.quarterReportPanel != null)
                UIManager.instance.quarterReportPanel.Show(CurrentQuarter, quarterReportLines);
            UIManager.instance.favoritePanel?.RefreshHome();
        }
        if (!string.IsNullOrEmpty(message)) ShowPersistenceNotice(message);
    }

    Era FindSavedEra(string id, string oldName) => Array.Find(allEras, e => e != null &&
        (!string.IsNullOrEmpty(id) ? e.SaveId == id : !string.IsNullOrEmpty(oldName) && e.eraName == oldName));

    void RestoreSchedule(GameSaveData data)
    {
        scheduledBigNewsCaseIndex = data.version == 0 ? data.scheduledBigNewsCaseIndex
            : BigTriggerCases.FindIndex(c => !string.IsNullOrEmpty(data.scheduledCaseId) && c.SaveId == data.scheduledCaseId);
        scheduledForeshadowIndex = -1;
        if (scheduledBigNewsCaseIndex < 0 || scheduledBigNewsCaseIndex >= BigTriggerCases.Count)
        {
            scheduledBigNewsCaseIndex = -1; scheduledBigNewsDay = scheduledBigNewsVibeDay = -1;
            return;
        }
        var hints = BigTriggerCases[scheduledBigNewsCaseIndex].foreshadowCases;
        if (hints != null) scheduledForeshadowIndex = data.version == 0 ? data.scheduledForeshadowIndex
            : Array.FindIndex(hints, h => h != null && !string.IsNullOrEmpty(data.scheduledHintId) && h.SaveId == data.scheduledHintId);
    }

    static void RestoreHistory<T>(List<T> target, List<T> saved)
    {
        target.Clear();
        if (saved == null) return;
        for (int i = Math.Max(0, saved.Count - GameBalance.HISTORY_LENGTH); i < saved.Count; i++) target.Add(saved[i]);
    }

    [Serializable]
    public class NewsSaveData
    {
        public string caseId, title, description, displayDescription;
        public Case.NewsType type;
        public string[] stockIds = Array.Empty<string>();
        public int[] min = Array.Empty<int>(), max = Array.Empty<int>();
        public float[] decay = Array.Empty<float>();
        public Case.IndustryRate[] industryRates;
        public List<ChainSaveData> chains = new();
        public List<string> nations = new();
        public int lastingDay, remaining, elapsed, delay;
        public bool pending;
    }
    [Serializable]
    public class ChainSaveData { public string stockId; public int min, max; }

    NewsSaveData CaptureNews(Case c, ActiveCase active, int delay)
    {
        var data = new NewsSaveData
        {
            caseId = c.SaveId, title = c.CaseName, description = c.CaseDescription,
            type = c.newsType, lastingDay = c.lastingDay,
            min = c.ChangePriceMin ?? Array.Empty<int>(), max = c.ChangePriceMax ?? Array.Empty<int>(),
            decay = c.lowPercent ?? Array.Empty<float>(), industryRates = c.industryRates,
            stockIds = Array.ConvertAll(c.affectedCase ?? Array.Empty<Stock>(), s => s != null ? s.SaveId : ""),
            pending = active == null, delay = delay, remaining = active?.Rday ?? 0, elapsed = active?.currentDay ?? 0,
            displayDescription = active?.displayDescription,
            nations = active != null ? new List<string>(active.involvedNations) : new List<string>(),
        };
        foreach (var chain in c.chainEffects ?? Array.Empty<Case.ChainEffect>())
            if (chain.stock != null) data.chains.Add(new ChainSaveData { stockId = chain.stock.SaveId, min = chain.priceMin, max = chain.priceMax });
        return data;
    }

    void RestoreNews(NewsSaveData saved)
    {
        Case original = Array.Find(AllCases, c => c != null && !string.IsNullOrEmpty(saved.caseId) && c.SaveId == saved.caseId);
        // 생성형 뉴스도 복원하도록 효과와 참조 ID를 저장한다.
        Case c = original != null ? Instantiate(original) : ScriptableObject.CreateInstance<Case>();
        restoredCases.Add(c);
        c.CaseName = saved.title; c.CaseDescription = saved.description; c.newsType = saved.type;
        c.lastingDay = saved.lastingDay; c.lowPercent = saved.decay; c.industryRates = saved.industryRates;
        var stocks = new List<Stock>(); var mins = new List<int>(); var maxs = new List<int>();
        for (int i = 0; i < saved.stockIds.Length; i++)
        {
            Stock stock = FindSavedStock(saved.stockIds[i]);
            if (stock == null || stock.isDelisted) continue;
            stocks.Add(stock); mins.Add(saved.min[i]); maxs.Add(saved.max[i]);
        }
        c.affectedCase = stocks.ToArray(); c.ChangePriceMin = mins.ToArray(); c.ChangePriceMax = maxs.ToArray();
        var chains = new List<Case.ChainEffect>();
        foreach (var chain in saved.chains ?? new List<ChainSaveData>())
        {
            if (chain == null) continue;
            Stock stock = FindSavedStock(chain.stockId);
            if (stock != null && !stock.isDelisted) chains.Add(new Case.ChainEffect { stock = stock, priceMin = chain.min, priceMax = chain.max });
        }
        c.chainEffects = chains.ToArray();
        if (saved.pending) pendingNewsList.Add(new PendingNews { caseData = c, daysLeft = saved.delay });
        else if (saved.remaining > 0) activeNewsList.Add(new ActiveCase
        {
            caseData = c, Rday = saved.remaining, currentDay = saved.elapsed,
            displayDescription = saved.displayDescription ?? saved.description, involvedNations = saved.nations ?? new List<string>(),
        });
    }

    void OnDestroy()
    {
        foreach (var c in restoredCases) if (c != null) Destroy(c);
        if (Instance == this) Instance = null;
    }

    void ResetStocksToBase()
    {
        foreach (Stock stock in tabmanager.allstock)
        {
            if (stock == null) continue;
            stock.StockCurrentPrice = stock.StockPreviousPrice = stock.StockBasePrice;
            stock.runtimeRevenue = stock.BaselineRevenue; stock.runtimeAsset = stock.BaselineAsset;
            stock.income = stock.runtimeRevenue - stock.operatingCost;
            stock.isFavorite = stock.isDelisted = false; stock.negativeQuarters = stock.lastQuarterIncome = 0;
            stock.changePctHistory.Clear(); stock.priceHistory.Clear(); stock.volumeHistory.Clear(); stock.runtimeIssues.Clear();
        }
    }
}
