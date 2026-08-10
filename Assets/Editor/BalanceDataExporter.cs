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
        Debug.Log("CSV 내보내기 완료! StockBalanceData.csv / CaseBalanceData.csv / TemplateBalanceData.csv 확인하세요.");
    }

    // 필드에 쉼표/따옴표/줄바꿈이 있으면 큰따옴표로 감싸서 CSV가 깨지지 않게 처리
    static string EscapeCsv(string field)
    {
        if (string.IsNullOrEmpty(field)) return "";
        if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        return field;
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
        sb.AppendLine("이름,종족,산업,기준가,변동성타입,국가,기본수입,기본금고,기준시가총액,발행주식수");

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
                $"{stock.baseIncome},{stock.BaselineAsset},{baseMarketCap},{stock.TotalShare}");
        }

        string savePath = Path.Combine(Application.dataPath, "../StockBalanceData.csv");
        File.WriteAllText(savePath, sb.ToString(), Encoding.UTF8);
    }

    // 모든 Case 에셋의 뉴스 정보 + 영향받는 주식별 변동률을 한 줄씩 CSV로 기록
    static void ExportCases()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("사건이름,뉴스타입,지속일,영향주식,최소변동률,최대변동률");

        string[] guids = AssetDatabase.FindAssets("t:Case");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Case c = AssetDatabase.LoadAssetAtPath<Case>(path);
            if (c == null) continue;

            // 영향받는 주식이 없는 경우 (대형 뉴스처럼 산업군 단위로만 영향받는 경우 등)
            if (c.affectedCase == null || c.affectedCase.Length == 0)
            {
                sb.AppendLine($"{c.CaseName},{c.newsType},{c.lastingDay},-,-,-");
                continue;
            }

            // 영향받는 주식마다 한 줄씩 기록 (같은 사건이 여러 줄로 나올 수 있음)
            for (int i = 0; i < c.affectedCase.Length; i++)
            {
                string stockName = c.affectedCase[i] != null ? c.affectedCase[i].StockName : "?";
                int min = (c.ChangePriceMin != null && i < c.ChangePriceMin.Length) ? c.ChangePriceMin[i] : 0;
                int max = (c.ChangePriceMax != null && i < c.ChangePriceMax.Length) ? c.ChangePriceMax[i] : 0;
                sb.AppendLine($"{c.CaseName},{c.newsType},{c.lastingDay},{stockName},{min},{max}");
            }
        }

        string savePath = Path.Combine(Application.dataPath, "../CaseBalanceData.csv");
        File.WriteAllText(savePath, sb.ToString(), Encoding.UTF8);
    }
}