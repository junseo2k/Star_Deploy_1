using System.Collections.Generic;
using System.IO;
using UnityEngine;

// GameManager의 저장/불러오기 담당 partial — JSON 파일로 게임 전체 상태를 저장하고 복원한다
public partial class GameManager
{
    // ── 저장 / 불러오기 ───────────────────────────────

    // 선택된 슬롯 번호 기반으로 저장 경로 반환
    string GetSavePath()
    {
        int slot = PlayerPrefs.GetInt("SelectedSlot", 1);
        return Path.Combine(Application.persistentDataPath, $"save{slot}.json");
    }

    // 현재 게임 상태를 JSON 파일로 저장
    public void SaveGame()
    {
        GameSaveData data = new()
        {
            currentgold = currentgold,
            currentday = currentDay,
            smallNewsTimer = smallNewsTimer,
            loanAmount = loanAmount,
            eraEndDay = eraEndDay,
            nextEraStartDay = nextEraStartDay,
            eraHintShownDay = eraHintShownDay,
            currentEraName = currentEra != null ? currentEra.eraName : "",
            pendingEraName = pendingEra != null ? pendingEra.eraName : "",
            activeBusinesses = activeBusinesses,
            lastBigNewsDay = lastBigNewsDay,
            scheduledBigNewsDay = scheduledBigNewsDay,
            scheduledBigNewsVibeDay = scheduledBigNewsVibeDay,
            scheduledBigNewsCaseIndex = scheduledBigNewsCaseIndex,
            scheduledForeshadowIndex = scheduledForeshadowIndex,
            scheduledNationPair = scheduledNationPair,
            OwnStocks = new List<StockSaveData>(),
        };

        // 보유 여부와 무관하게 모든 주식의 현재 상태를 저장 (가격·실적이 매턴 바뀌므로)
        foreach (Stock stock in tabmanager.allstock)
        {
            data.OwnStocks.Add(new StockSaveData
            {
                stockname = stock.StockName,
                currentPrice = stock.StockCurrentPrice,
                isFavorite = stock.isFavorite,
                income = stock.income,
                revenue = stock.runtimeRevenue, // 사업 성패가 누적된 런타임 매출을 저장
                asset = stock.runtimeAsset,
                negativeQuarters = stock.negativeQuarters,
                isDelisted = stock.isDelisted,
                ownamount = Portfolio.GetAmount(stock),
                avgPurchasePrice = Portfolio.GetAvgPurchasePrice(stock),
            });
        }

        File.WriteAllText(GetSavePath(), JsonUtility.ToJson(data));
    }

    // JSON 파일에서 게임 데이터를 불러와 복원
    void LoadGame()
    {
        // Portfolio는 static이라 씬을 다시 로드해도 값이 남으므로, 불러오기 전에 항상 비운다
        Portfolio.Clear();

        // 먼저 모든 주식을 기본값으로 세팅 — 저장 이후에 새로 추가된 주식도 0G로 시작하지 않도록
        ResetStocksToBase();

        string path = GetSavePath();
        if (!File.Exists(path))
        {
            currentgold = GameBalance.START_GOLD;
            currentDay = 1;
            return;
        }

        GameSaveData data = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(path));
        currentgold = data.currentgold;
        currentDay = data.currentday;
        lastBigNewsDay = data.lastBigNewsDay;
        scheduledBigNewsDay = data.scheduledBigNewsDay;
        scheduledBigNewsVibeDay = data.scheduledBigNewsVibeDay;
        scheduledBigNewsCaseIndex = data.scheduledBigNewsCaseIndex;
        scheduledForeshadowIndex = data.scheduledForeshadowIndex;
        scheduledNationPair = data.scheduledNationPair ?? "";
        smallNewsTimer = data.smallNewsTimer;
        loanAmount = data.loanAmount;
        eraEndDay = data.eraEndDay;
        nextEraStartDay = data.nextEraStartDay > 0 ? data.nextEraStartDay : GameBalance.ERA_FIRST_START_DAY;
        eraHintShownDay = data.eraHintShownDay;
        currentEra = System.Array.Find(allEras, e => e.eraName == data.currentEraName);
        pendingEra = System.Array.Find(allEras, e => e.eraName == data.pendingEraName);
        activeBusinesses = data.activeBusinesses ?? new List<ActiveBusiness>();

        if (data.OwnStocks == null) return;

        // 이름으로 주식을 찾기 위한 색인 — 저장 목록과 이중 루프를 도는 것을 피함
        Dictionary<string, Stock> stocksByName = new();
        foreach (Stock stock in tabmanager.allstock)
            stocksByName[stock.StockName] = stock;

        foreach (StockSaveData saved in data.OwnStocks)
        {
            if (!stocksByName.TryGetValue(saved.stockname, out Stock stock)) continue;

            stock.StockCurrentPrice = saved.currentPrice;
            stock.StockPreviousPrice = saved.currentPrice;
            stock.isFavorite = saved.isFavorite;
            stock.income = saved.income;
            // 옛 세이브에는 revenue 칸이 없다. 그때는 baseIncome을 매출로 본다 (운영비가 0이던 시절의 값)
            stock.runtimeRevenue = saved.revenue > 0 ? saved.revenue : saved.baseIncome;
            stock.runtimeAsset = saved.asset;
            stock.negativeQuarters = saved.negativeQuarters;
            stock.isDelisted = saved.isDelisted;

            // 상폐된 회사는 보유 주식이 있어도 복원하지 않는다 — 상폐 시점에 이미 전액 손실 처리됐다
            if (!stock.isDelisted)
                Portfolio.SetHolding(stock, saved.ownamount, saved.avgPurchasePrice);
        }
    }

    // 모든 주식의 런타임 값을 인스펙터 기준값으로 되돌린다
    // Stock은 ScriptableObject라 에디터에서 값이 남으므로 불러오기 전에 반드시 초기화해야 한다
    void ResetStocksToBase()
    {
        foreach (Stock stock in tabmanager.allstock)
        {
            stock.StockCurrentPrice = stock.StockBasePrice;
            stock.StockPreviousPrice = stock.StockBasePrice;
            // 사업 보너스가 쌓인 런타임 매출을 에셋 원본으로 되돌린다 (도메인 리로드 여부와 무관하게 항상 실행)
            stock.runtimeRevenue = stock.BaselineRevenue;
            stock.runtimeAsset = stock.BaselineAsset;
            stock.income = stock.runtimeRevenue - stock.operatingCost;
            // 즐겨찾기는 세이브 대상이지만 에셋에도 남으므로 먼저 끈다
            // 세이브가 있으면 바로 아래 LoadGame이 저장된 값으로 다시 켜준다
            stock.isFavorite = false;

            // 상폐 상태도 NonSerialized라 에디터에서 값이 남지 않지만, 도메인 리로드 없이 이어서
            // 새 게임을 시작하는 경우를 대비해 명시적으로 초기화한다. 세이브가 있으면 LoadGame이 다시 채운다
            stock.negativeQuarters = 0;
            stock.isDelisted = false;

            stock.changePctHistory.Clear();
            stock.priceHistory.Clear();
            stock.volumeHistory.Clear();
        }
    }
}
