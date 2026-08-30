using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

// 밸런스 작업용 데이터 내보내기 스크립트
// Assets/Editor 폴더에 넣어야 합니다 (Editor 전용 스크립트라 빌드에는 포함되지 않음)
public class BalanceDataExporter
{
    [MenuItem("Tools/밸런스 데이터 내보내기 (CSV)")]
    public static void ExportAll()
    {
        ExportStocks();
        ExportCases();
        ExportTemplates();
        ExportIndustryRates();
        ExportBusinesses();
        ExportEras();
        Debug.Log("CSV 내보내기 완료! StockBalanceData.csv / CaseBalanceData.csv / TemplateBalanceData.csv / " +
            "CaseIndustryRates.csv / BusinessBalanceData.csv / EraBalanceData.csv 확인하세요.");
    }

    // 필드에 쉼표/따옴표/줄바꿈이 있으면 큰따옴표로 감싸서 CSV가 깨지지 않게 처리
    static string EscapeCsv(string field)
    {
        if (string.IsNullOrEmpty(field)) return "";
        if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        return field;
    }

    // 문자열 배열을 ";"로 이어붙인 뒤, 안에 쉼표가 섞여 있을 수 있으니 EscapeCsv까지 통과시킨다
    static string JoinStrings(string[] arr)
    {
        if (arr == null || arr.Length == 0) return "";
        StringBuilder s = new StringBuilder();
        for (int i = 0; i < arr.Length; i++)
        {
            if (i > 0) s.Append(";");
            s.Append(arr[i]);
        }
        return EscapeCsv(s.ToString());
    }

    // float 배열(뉴스 감쇠 곡선 lowPercent 등)을 ";"로 이어붙인다
    static string JoinFloats(float[] arr)
    {
        if (arr == null || arr.Length == 0) return "";
        StringBuilder s = new StringBuilder();
        for (int i = 0; i < arr.Length; i++)
        {
            if (i > 0) s.Append(";");
            s.Append(arr[i].ToString("0.###"));
        }
        return s.ToString();
    }

    // 모든 NewsTemplate 에셋의 이벤트 목록을 한 줄씩 CSV로 기록
    static void ExportTemplates()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("템플릿이름,대상주식,사건문구,상승여부,변동최소,변동최대,지속일");

        string[] guids = AssetDatabase.FindAssets("t:NewsTemplate");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            NewsTemplate t = AssetDatabase.LoadAssetAtPath<NewsTemplate>(path);
            if (t == null) continue;

            // 이벤트마다 한 줄씩 기록 (이벤트가 이미 자기 회사·범위를 가지고 있음)
            foreach (var e in t.events)
            {
                string stockName = e.targetStock != null ? e.targetStock.StockName : "?";
                bool isPositive = (e.changeMin + e.changeMax) >= 0; // isPositive 필드가 삭제되어 부호로 direct 계산
                sb.AppendLine($"{t.name},{stockName},{EscapeCsv(e.eventText)},{isPositive}," +
                    $"{e.changeMin},{e.changeMax},{t.lastingDay}");
            }
        }

        string savePath = Path.Combine(Application.dataPath, "../TemplateBalanceData.csv");
        File.WriteAllText(savePath, sb.ToString(), Encoding.UTF8);
    }

    // 모든 Stock 에셋의 기본 스탯을 한 줄씩 CSV로 기록
    static void ExportStocks()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("이름,종족,산업,기준가,변동성타입,국가,체질,성향,소유주,평소매출,운영비,기본수입,기본금고,기준시가총액,발행주식수");

        string[] guids = AssetDatabase.FindAssets("t:Stock");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Stock stock = AssetDatabase.LoadAssetAtPath<Stock>(path);
            if (stock == null) continue;

            // income / runtimeAsset / StockCurrentPrice는 런타임 전용(NonSerialized)이라
            // 플레이 모드가 아닌 지금은 전부 0이다. 밸런스 확인용이므로 에셋에 저장된 기준값으로 계산한다
            long baseMarketCap = (long)stock.StockBasePrice * stock.TotalShare;

            sb.AppendLine($"{stock.StockName},{stock.FamilyTag},{stock.IndustryTag}," +
                $"{stock.StockBasePrice},{stock.volatilityType},{stock.nation}," +
                $"{stock.companyType},{stock.personality},{stock.owner}," +
                $"{stock.BaselineRevenue},{stock.operatingCost}," +
                $"{stock.BaselineIncome},{stock.BaselineAsset},{baseMarketCap},{stock.TotalShare}");
        }

        string savePath = Path.Combine(Application.dataPath, "../StockBalanceData.csv");
        File.WriteAllText(savePath, sb.ToString(), Encoding.UTF8);
    }

    // 모든 Case 에셋의 뉴스 정보 + 영향받는 주식별 변동률 + 감쇠 곡선을 한 줄씩 CSV로 기록
    static void ExportCases()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("사건이름,뉴스타입,지속일,영향주식,최소변동률,최대변동률,감쇠곡선");

        string[] guids = AssetDatabase.FindAssets("t:Case");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Case c = AssetDatabase.LoadAssetAtPath<Case>(path);
            if (c == null) continue;

            string lowPercentStr = JoinFloats(c.lowPercent);

            // 영향받는 주식이 없는 경우 (대형 뉴스처럼 산업군 단위로만 영향받는 경우 등)
            if (c.affectedCase == null || c.affectedCase.Length == 0)
            {
                sb.AppendLine($"{c.CaseName},{c.newsType},{c.lastingDay},-,-,-,{lowPercentStr}");
                continue;
            }

            // 영향받는 주식마다 한 줄씩 기록 (같은 사건이 여러 줄로 나올 수 있음)
            for (int i = 0; i < c.affectedCase.Length; i++)
            {
                string stockName = c.affectedCase[i] != null ? c.affectedCase[i].StockName : "?";
                int min = (c.ChangePriceMin != null && i < c.ChangePriceMin.Length) ? c.ChangePriceMin[i] : 0;
                int max = (c.ChangePriceMax != null && i < c.ChangePriceMax.Length) ? c.ChangePriceMax[i] : 0;
                sb.AppendLine($"{c.CaseName},{c.newsType},{c.lastingDay},{stockName},{min},{max},{lowPercentStr}");
            }
        }

        string savePath = Path.Combine(Application.dataPath, "../CaseBalanceData.csv");
        File.WriteAllText(savePath, sb.ToString(), Encoding.UTF8);
    }

    // 대형 뉴스(BigTriggerNews) 전용 — 산업군별 당사국/비당사국 변동률(industryRates)을 한 줄씩 CSV로 기록
    // ExportCases()의 영향주식 컬럼은 "-"로만 나오는 대형 뉴스를, 여기서 산업군 단위로 풀어서 보여준다
    static void ExportIndustryRates()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("사건이름,접경국쌍,산업태그,당사국최소,당사국최대,비당사국최소,비당사국최대");

        string[] guids = AssetDatabase.FindAssets("t:Case");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Case c = AssetDatabase.LoadAssetAtPath<Case>(path);
            if (c == null || c.industryRates == null || c.industryRates.Length == 0) continue;

            string nationPairsStr = JoinStrings(c.nationPairs);

            foreach (var ir in c.industryRates)
            {
                sb.AppendLine($"{c.CaseName},{nationPairsStr},{ir.industryTag}," +
                    $"{ir.changePriceMin},{ir.changePriceMax},{ir.changePriceMinNon},{ir.changePriceMaxNon}");
            }
        }

        string savePath = Path.Combine(Application.dataPath, "../CaseIndustryRates.csv");
        File.WriteAllText(savePath, sb.ToString(), Encoding.UTF8);
    }

    // 모든 Stock의 사업 프로젝트(businesses[])를 한 줄씩 CSV로 기록
    static void ExportBusinesses()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("종목,사업이름,투자비용,기간분기,성공확률,매출증가량,매출감소량,금고증가량,착수문구,성공문구,실패문구");

        string[] guids = AssetDatabase.FindAssets("t:Stock");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Stock stock = AssetDatabase.LoadAssetAtPath<Stock>(path);
            if (stock == null || stock.businesses == null) continue;

            foreach (BusinessProject p in stock.businesses)
            {
                if (p == null) continue;

                sb.AppendLine($"{stock.StockName},{p.projectName},{p.investmentCost},{p.durationQuarters}," +
                    $"{p.successRate},{p.revenueBoost},{p.revenueDrop},{p.assetBoost}," +
                    $"{EscapeCsv(p.startNews)},{EscapeCsv(p.successNews)},{EscapeCsv(p.failNews)}");
            }
        }

        string savePath = Path.Combine(Application.dataPath, "../BusinessBalanceData.csv");
        File.WriteAllText(savePath, sb.ToString(), Encoding.UTF8);
    }

    // 모든 Era 에셋을 한 줄씩 CSV로 기록
    static void ExportEras()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("시대이름,설명,유망산업,비선호산업,유망배율,비선호배율,힌트뉴스");

        string[] guids = AssetDatabase.FindAssets("t:Era");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Era era = AssetDatabase.LoadAssetAtPath<Era>(path);
            if (era == null) continue;

            string boosted = JoinStrings(era.boostedIndustries);
            string weakened = JoinStrings(era.weakenedIndustries);
            string hintNewsName = era.hintNews != null ? era.hintNews.CaseName : "";

            sb.AppendLine($"{era.eraName},{EscapeCsv(era.eraDescription)},{boosted},{weakened}," +
                $"{era.boostedRate},{era.weakenedRate},{EscapeCsv(hintNewsName)}");
        }

        string savePath = Path.Combine(Application.dataPath, "../EraBalanceData.csv");
        File.WriteAllText(savePath, sb.ToString(), Encoding.UTF8);
    }
}