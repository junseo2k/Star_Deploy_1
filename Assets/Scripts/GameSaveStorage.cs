using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Unity 씬과 독립적인 저장 파일 처리. 임시 파일 완성 후 교체하고 이전 파일은 보관한다.
public static class GameSaveStorage
{
    public const int CurrentVersion = 1;

    public static bool TryRead(string path, out GameManager.GameSaveData data, out string message)
    {
        data = null;
        message = "";
        try
        {
            data = Parse(File.ReadAllText(path));
            return true;
        }
        catch (NotSupportedException e) { message = e.Message; return false; }
        catch (Exception e) when (IsFileError(e)) { message = e.Message; }

        try
        {
            data = Parse(File.ReadAllText(path + ".bak"));
            message = "저장 파일을 읽지 못해 이전 백업을 복원했습니다.";
            return true;
        }
        catch (Exception e) when (IsFileError(e) || e is NotSupportedException)
        {
            message = "저장 파일을 불러올 수 없습니다: " + message;
            return false;
        }
    }

    public static GameManager.GameSaveData Parse(string json)
    {
        JObject root = JObject.Parse(json);
        int version = root.Value<int?>("version") ?? 0;
        if (version < 0 || version > CurrentVersion)
            throw new NotSupportedException("현재 게임에서 지원하지 않는 저장 버전입니다.");

        // 값의 부호가 아니라 필드 유무로 옛 매출 형식을 구분한다.
        if (version == 0 && root["OwnStocks"] is JArray stocks)
            foreach (JObject stock in stocks.Children<JObject>())
                if (stock["revenue"] == null)
                    stock["revenue"] = stock["baseIncome"] ?? new JValue(0);

        var data = root.ToObject<GameManager.GameSaveData>();
        Validate(data);
        return data;
    }

    public static void Validate(GameManager.GameSaveData data)
    {
        if (data == null || data.currentday < 1 || data.currentday == int.MaxValue ||
            data.loanAmount < 0 || data.OwnStocks == null)
            throw new InvalidDataException("게임 기본 상태가 올바르지 않습니다.");

        var ids = new System.Collections.Generic.HashSet<string>();
        foreach (var stock in data.OwnStocks)
        {
            if (stock == null || stock.currentPrice <= 0 || stock.ownamount < 0 ||
                float.IsNaN(stock.avgPurchasePrice) || float.IsInfinity(stock.avgPurchasePrice) ||
                stock.avgPurchasePrice < 0 || (string.IsNullOrEmpty(stock.stockId) && string.IsNullOrEmpty(stock.stockname)))
                throw new InvalidDataException("종목 저장 데이터가 올바르지 않습니다.");
            string key = string.IsNullOrEmpty(stock.stockId) ? stock.stockname : stock.stockId;
            if (!ids.Add(key)) throw new InvalidDataException("종목 저장 데이터가 중복되었습니다.");
            if (stock.priceHistory != null && stock.priceHistory.Exists(p => p <= 0))
                throw new InvalidDataException("가격 이력이 올바르지 않습니다.");
            if (!Finite(stock.lastEraModifier) ||
                stock.changeHistory != null && stock.changeHistory.Exists(v => !Finite(v)) ||
                stock.volumeHistory != null && stock.volumeHistory.Exists(v => !Finite(v) || v < 0))
                throw new InvalidDataException("가격 이력에 올바르지 않은 수치가 있습니다.");
        }
        if (data.news != null)
            foreach (var news in data.news)
                if (news == null || news.stockIds == null || news.min == null || news.max == null ||
                    news.stockIds.Length != news.min.Length || news.stockIds.Length != news.max.Length ||
                    news.decay == null || Array.Exists(news.decay, v => !Finite(v)) ||
                    news.elapsed < 0 || news.remaining < 0 || news.delay < 0)
                    throw new InvalidDataException("뉴스 저장 데이터가 올바르지 않습니다.");
        if (data.activeBusinesses != null && data.activeBusinesses.Exists(b => b == null || b.remainingQuarters < 0))
            throw new InvalidDataException("사업 저장 데이터가 올바르지 않습니다.");
        var npcIds = new System.Collections.Generic.HashSet<string>();
        if (data.npcs != null)
            foreach (var npc in data.npcs)
                if (npc == null || string.IsNullOrEmpty(npc.id) || !npcIds.Add(npc.id) ||
                    npc.gold < 0 || npc.holdings == null || npc.histories == null ||
                    npc.holdings.Exists(h => h == null || h.amount < 0) ||
                    npc.histories.Exists(h => h == null || h.days == null ||
                        h.days.Exists(d => d == null || !Finite(d.price) || d.price <= 0 ||
                            !Finite(d.features.priceChangePct) || !Finite(d.features.eraRatePct) || !Finite(d.features.fairPriceRatio))))
                    throw new InvalidDataException("NPC 저장 데이터가 올바르지 않습니다.");
    }

    static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    public static void Write(string path, GameManager.GameSaveData data)
    {
        Validate(data);
        string json = JsonConvert.SerializeObject(data);
        Parse(json); // 복원 가능한 파일만 교체한다.
        string temp = path + ".tmp";
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        try
        {
            using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] bytes = new System.Text.UTF8Encoding(false).GetBytes(json);
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            if (File.Exists(path))
            {
                bool validPrevious = false;
                try { Parse(File.ReadAllText(path)); validPrevious = true; }
                catch (Exception e) when (IsFileError(e) || e is NotSupportedException) { }
                File.Replace(temp, path, validPrevious ? path + ".bak" : null);
            }
            else File.Move(temp, path);
        }
        finally
        {
            try { if (File.Exists(temp)) File.Delete(temp); }
            catch (Exception e) when (IsFileError(e)) { }
        }
    }

    public static bool IsFileError(Exception e) => e is IOException || e is UnauthorizedAccessException ||
        e is JsonException || e is ArgumentException || e is OverflowException || e is System.Security.SecurityException;
}
