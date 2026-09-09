using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 실제 슬롯을 건드리지 않는 저장/금액 회귀 검사. Tools 메뉴에서 다시 실행할 수 있다.
public static class BuildReadinessChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("Tools/Validation/Run Save and Economy Checks")]
    public static void Run()
    {
        var results = new List<string>();
        string folder = Path.Combine(Path.GetTempPath(), "StarDeployChecks_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var objects = new List<Object>();
        GameManager oldGame = GameManager.Instance;
        UIManager oldUI = UIManager.instance;
        try
        {
            if (Application.isPlaying) throw new InvalidOperationException("플레이를 종료한 상태에서 검사를 실행하세요.");
            Check("legacy negative/zero/missing revenue", () =>
            {
                foreach (int revenue in new[] { -500, 0, 500 })
                {
                    string json = "{\"currentday\":15,\"currentgold\":60000,\"OwnStocks\":[{\"stockname\":\"A\",\"currentPrice\":100,\"revenue\":" + revenue + ",\"baseIncome\":777}]}";
                    Require(GameSaveStorage.Parse(json).OwnStocks[0].revenue == revenue, "revenue changed");
                }
                Require(GameSaveStorage.Parse("{\"currentday\":1,\"OwnStocks\":[{\"stockname\":\"A\",\"currentPrice\":100,\"baseIncome\":777}]}").OwnStocks[0].revenue == 777, "legacy migration");
            }, results);

            var data = new GameManager.GameSaveData
            {
                version = GameSaveStorage.CurrentVersion, currentday = 15, currentgold = 5000000000L,
                OwnStocks = new List<GameManager.StockSaveData>
                {
                    new() { stockId = "stable-a", stockname = "A", currentPrice = 100, previousPrice = 90,
                        revenue = -500, lastQuarterIncome = -10, priceHistory = new List<int> { 90, 100 } },
                },
            };
            string path = Path.Combine(folder, "save.json");
            Check("atomic write, backup, 64-bit amount and history", () =>
            {
                GameSaveStorage.Write(path, data);
                data.currentday = 16;
                GameSaveStorage.Write(path, data);
                Require(GameSaveStorage.TryRead(path, out var loaded, out _) && loaded.currentgold == 5000000000L, "64-bit roundtrip");
                Require(loaded.OwnStocks[0].priceHistory.Count == 2 && loaded.OwnStocks[0].revenue == -500, "state roundtrip");
                Require(GameSaveStorage.Parse(File.ReadAllText(path + ".bak")).currentday == 15, "backup missing");
                File.WriteAllText(path, "{broken");
                Require(GameSaveStorage.TryRead(path, out loaded, out var message) && loaded.currentday == 15 && message.Length > 0, "backup recovery");
                GameSaveStorage.Write(path, loaded);
                Require(GameSaveStorage.Parse(File.ReadAllText(path + ".bak")).currentday == 15, "valid backup overwritten by corruption");
            }, results);
            Check("invalid data and write failure preserve prior save", () =>
            {
                string before = File.ReadAllText(path);
                data.currentday = 0;
                bool rejected = false;
                try { GameSaveStorage.Write(path, data); } catch (InvalidDataException) { rejected = true; }
                Require(rejected && File.ReadAllText(path) == before, "invalid save replaced good file");
                data.currentday = 16;
                Directory.CreateDirectory(path + ".tmp");
                rejected = false;
                try { GameSaveStorage.Write(path, data); } catch (Exception e) when (GameSaveStorage.IsFileError(e)) { rejected = true; }
                Require(rejected && File.ReadAllText(path) == before, "write failure replaced good file");
                Directory.Delete(path + ".tmp");
                File.WriteAllText(path, "{\"version\":999}");
                Require(!GameSaveStorage.TryRead(path, out _, out _), "future save rolled back silently");
            }, results);

            Stock stock = ScriptableObject.CreateInstance<Stock>(); objects.Add(stock);
            typeof(SaveIdentity).GetField("saveId", Private).SetValue(stock, "stable-a");
            stock.StockName = "Renamed A"; stock.StockCurrentPrice = 2000000000; stock.TotalShare = 10000;
            var root = new GameObject("SaveChecks"); root.SetActive(false); objects.Add(root);
            var game = root.AddComponent<GameManager>();
            var tab = root.AddComponent<TabManager>(); game.tabmanager = tab; tab.allstock = new[] { stock };
            game.AllCases = Array.Empty<Case>(); game.allEras = Array.Empty<Era>();

            Check("stock lookup survives rename", () => Require(game.FindSavedStock("stable-a", "A") == stock, "name used instead of ID"), results);
            Check("large trade, saturation and screen mode", () =>
            {
                var order = root.AddComponent<OrderPanel>(); order.targetStock = stock;
                order.CalculateTrade(10000, true, out int end, out long cost);
                Require(end == int.MaxValue && cost > int.MaxValue, "trade overflow");
                Require(MoneyMath.Add(long.MaxValue, 1) == long.MaxValue && MoneyMath.Add(long.MinValue, -1) == long.MinValue, "sum overflow");
                Require(Setting.GetScreenMode(0) == FullScreenMode.ExclusiveFullScreen &&
                    Setting.GetScreenMode(1) == FullScreenMode.FullScreenWindow && Setting.GetScreenMode(2) == FullScreenMode.Windowed, "screen mode mismatch");
            }, results);

            Check("generated and pending news roundtrip", () =>
            {
                var news = new GameManager.NewsSaveData
                {
                    title = "Generated", description = "Rumor", displayDescription = "A/B resolved",
                    type = Case.NewsType.SmallCompanyNews, remaining = 2, elapsed = 1, lastingDay = 3,
                    stockIds = new[] { stock.SaveId }, min = new[] { -5 }, max = new[] { -1 }, decay = new[] { 1f, .4f },
                    nations = new List<string> { "A", "B" }, chains = new List<GameManager.ChainSaveData>
                    { new() { stockId = stock.SaveId, min = -2, max = 3 } },
                };
                var copy = JsonConvert.DeserializeObject<GameManager.NewsSaveData>(JsonConvert.SerializeObject(news));
                Invoke(game, "RestoreNews", copy);
                var active = game.activeNewsList[0];
                Require(active.Rday == 2 && active.currentDay == 1 && active.caseData.affectedCase[0] == stock &&
                    active.caseData.chainEffects[0].stock == stock && active.involvedNations.Count == 2, "news state lost");
                copy.pending = true; copy.delay = 2;
                Invoke(game, "RestoreNews", copy);
                Require(game.pendingNewsList[0].daysLeft == 2, "pending news lost");
            }, results);
            Check("scheduled news survives reorder", () =>
            {
                Case a = ScriptableObject.CreateInstance<Case>(); Case b = ScriptableObject.CreateInstance<Case>();
                objects.Add(a); objects.Add(b);
                typeof(SaveIdentity).GetField("saveId", Private).SetValue(a, "event-a");
                typeof(SaveIdentity).GetField("saveId", Private).SetValue(b, "event-b");
                a.foreshadowCases = new[] { b };
                game.BigTriggerCases = new List<Case> { b, a };
                Invoke(game, "RestoreSchedule", new GameManager.GameSaveData
                    { version = 1, scheduledCaseId = "event-a", scheduledHintId = "event-b", scheduledBigNewsCaseIndex = 0 });
                Require(game.scheduledBigNewsCaseIndex == 1 && game.scheduledForeshadowIndex == 0, "schedule index not remapped");
            }, results);
            Check("NPC holdings/history and delisting", () =>
            {
                var npc = root.AddComponent<NPCInvestor>();
                var saved = new NPCInvestor.SaveData
                {
                    id = "test-npc", gold = 5000000000L,
                    holdings = new List<NPCInvestor.HoldingSave> { new() { stockId = stock.SaveId, amount = 10000 } },
                    histories = new List<NPCInvestor.HistorySave> { new() { stockId = stock.SaveId,
                        days = new List<NPCInvestor.HistoryDay> { new() { price = 100, features = new StockDailyFeatures { fairPriceRatio = 1.2f } } } } },
                };
                npc.RestoreSave(saved, game);
                var roundtrip = npc.CaptureSave();
                Require(roundtrip.gold == saved.gold && roundtrip.histories[0].days.Count == 1 && npc.totalAssets > int.MaxValue, "NPC state lost");
                stock.isDelisted = true; npc.OnStockDelisted(stock);
                Require(npc.holdings.Count == 0 && npc.totalAssets == saved.gold && npc.CaptureSave().histories.Count == 0, "delisted value retained");
                stock.isDelisted = false;
            }, results);
            Check("all content assets have stable unique IDs", () =>
            {
                var ids = new HashSet<string>();
                foreach (string type in new[] { "Stock", "Case", "Era" })
                    foreach (string guid in AssetDatabase.FindAssets("t:" + type))
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<SaveIdentity>(AssetDatabase.GUIDToAssetPath(guid));
                        Require(asset != null && asset.SaveId == guid && ids.Add(guid), "asset ID missing/duplicate: " + guid);
                    }
            }, results);
            Check("full load restores portfolio, history, finances, business and NPC", () =>
            {
                UIManager.instance = null;
                var npc = root.GetComponent<NPCInvestor>();
                typeof(NPCInvestor).GetField("saveId", Private).SetValue(npc, "test-npc");
                data.OwnStocks[0].ownamount = 12;
                data.OwnStocks[0].avgPurchasePrice = 80;
                data.OwnStocks[0].lastRevenue = -600;
                data.OwnStocks[0].lastEraModifier = .8f;
                data.activeBusinesses = new List<GameManager.ActiveBusiness>
                {
                    new() { stockId = stock.SaveId, stockName = "Old Name", projectName = "Project", remainingQuarters = 2 },
                };
                data.npcs.Add(new NPCInvestor.SaveData { id = "test-npc", gold = 6000000000L,
                    holdings = new List<NPCInvestor.HoldingSave> { new() { stockId = stock.SaveId, amount = 20 } } });
                string fullPath = Path.Combine(folder, "full.json");
                GameSaveStorage.Write(fullPath, data);
                var previousHoldings = new List<(Stock stock, int amount, float average)>();
                foreach (var h in Portfolio.All) previousHoldings.Add((h.Key, h.Value.amount, h.Value.avgPurchasePrice));
                try
                {
                    Invoke(game, "LoadGame", fullPath);
                    Require(game.currentDay == 16 && game.currentgold == 5000000000L, "player state");
                    Require(stock.runtimeRevenue == -500 && stock.priceHistory.Count == 2 && stock.lastQuarterIncome == -10, "stock state");
                    Require(Portfolio.GetAmount(stock) == 12 && Portfolio.GetAvgPurchasePrice(stock) == 80, "portfolio state");
                    Require(game.activeBusinesses[0].stockName == stock.StockName && game.activeBusinesses[0].remainingQuarters == 2, "business rename");
                    Require(npc.gold == 6000000000L && npc.holdings[0].amount == 20, "NPC restore");
                }
                finally
                {
                    Portfolio.Clear();
                    foreach (var h in previousHoldings) Portfolio.SetHolding(h.stock, h.amount, h.average);
                    UIManager.instance = oldUI;
                }
            }, results);
            Check("missing NPC model fails gracefully", () =>
            {
                var npc = root.GetComponent<NPCInvestor>();
                Invoke(npc, "Start");
                Require((bool)typeof(NPCInvestor).GetField("predictionDisabled", Private).GetValue(npc), "NPC failure not isolated");
            }, results);
        }
        catch (Exception e) { results.Add("FAIL setup: " + e); }
        finally
        {
            // 테스트 뉴스만 정리한다. 실제 게임 에셋이나 슬롯은 수정하지 않는다.
            foreach (var obj in objects)
                if (obj is GameObject go)
                {
                    var gm = go.GetComponent<GameManager>();
                    if (gm != null)
                    {
                        var cases = (List<Case>)typeof(GameManager).GetField("restoredCases", Private).GetValue(gm);
                        foreach (var c in cases) if (c != null) Object.DestroyImmediate(c);
                        cases.Clear();
                    }
                }
            for (int i = objects.Count - 1; i >= 0; i--) if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            GameManager.Instance = oldGame; UIManager.instance = oldUI;
            Directory.Delete(folder, true);
            File.WriteAllText("Library/BuildReadinessChecks.json", JsonConvert.SerializeObject(results, Formatting.Indented));
        }
        bool failed = results.Exists(r => r.StartsWith("FAIL"));
        if (failed) Debug.LogError(string.Join("\n", results));
        else Debug.Log("Build readiness checks passed: " + results.Count + "\n" + string.Join("\n", results));
    }

    static object Invoke(object target, string method, params object[] args) => target.GetType().GetMethod(method, Private).Invoke(target, args);
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Check(string name, Action action, List<string> results)
    {
        try { action(); results.Add("PASS " + name); }
        catch (Exception e) { results.Add("FAIL " + name + ": " + e); }
    }
}
