using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

// CSV 파일에서 Case ScriptableObject 에셋을 일괄 생성하는 에디터 유틸리티 (Tools > Import Cases from CSV)
public class CaseImporter : EditorWindow
{
    [MenuItem("Tools/Import Cases from CSV")]
    // CSV를 파싱하여 각 행을 Case 에셋으로 변환하고 Assets/Case/ 폴더에 저장
    public static void ImportCases()
    {
        string csvPath = EditorUtility.OpenFilePanel("CSV 파일 선택", "", "csv");
        if (string.IsNullOrEmpty(csvPath)) return;

        string[] lines = File.ReadAllLines(csvPath, System.Text.Encoding.UTF8);

        for (int i = 1; i < lines.Length; i++) // 1행부터 시작 (0행은 헤더)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] cols = SplitCSVLine(lines[i]);
            if (cols.Length < 13) continue; // 필수 컬럼 수 미달 시 스킵

            string caseName = cols[0].Trim();
            if (string.IsNullOrEmpty(caseName)) continue;
            if (caseName.StartsWith("---")) continue; // 구분자 행 스킵

            Case newCase = ScriptableObject.CreateInstance<Case>();
            newCase.CaseName = caseName;
            newCase.CaseDescription = cols[1].Trim();

            // affectedCase — 소형 뉴스용
            string[] affectedNames = SplitArray(cols[2]);
            List<Stock> affectedStocks = new List<Stock>();
            foreach (string stockName in affectedNames)
            {
                string trimmed = stockName.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                Stock stock = AssetDatabase.LoadAssetAtPath<Stock>($"Assets/Stock/{trimmed}.asset");
                if (stock != null)
                    affectedStocks.Add(stock);
                else
                    Debug.LogWarning($"[{caseName}] Stock 에셋을 찾을 수 없음: {trimmed}");
            }
            newCase.affectedCase = affectedStocks.ToArray();

            newCase.ChangePriceMin = ParseIntArray(cols[3]);
            newCase.ChangePriceMax = ParseIntArray(cols[4]);
            newCase.lastingDay = int.TryParse(cols[7].Trim(), out int lasting) ? lasting : 1;
            newCase.lowPercent = ParseFloatArray(cols[8]);

            if (System.Enum.TryParse(cols[9].Trim(), out Case.NewsType parsedType))
                newCase.newsType = parsedType;
            else
                Debug.LogWarning($"[{caseName}] newsType 파싱 실패: {cols[9]}");


            // nationPairs — '|' 구분자로 분리된 국가 쌍 목록
            string nationPairsRaw = cols[12].Trim();
            if (!string.IsNullOrEmpty(nationPairsRaw))
            {
                string[] pairs = nationPairsRaw.Split('|');
                List<string> pairList = new List<string>();
                foreach (string pair in pairs)
                    if (!string.IsNullOrEmpty(pair.Trim()))
                        pairList.Add(pair.Trim());
                newCase.nationPairs = pairList.ToArray();
            }
            else
            {
                newCase.nationPairs = new string[0];
            }

            // industryRates — 대형 뉴스용 산업군별 변동률
            if (cols.Length > 13 && !string.IsNullOrWhiteSpace(cols[13]))
            {
                string[] rateParts = cols[13].Trim().Split('|');
                List<Case.IndustryRate> rateList = new List<Case.IndustryRate>();
                foreach (string ratePart in rateParts)
                {
                    // 형식: "산업태그:최소:최대:최소(비당사):최대(비당사)"
                    string[] rateVals = ratePart.Trim().Split(':');
                    if (rateVals.Length < 5) continue;
                    Case.IndustryRate rate = new Case.IndustryRate();
                    rate.industryTag = rateVals[0].Trim();
                    rate.changePriceMin = int.TryParse(rateVals[1], out int rMin) ? rMin : 0;
                    rate.changePriceMax = int.TryParse(rateVals[2], out int rMax) ? rMax : 0;
                    rate.changePriceMinNon = int.TryParse(rateVals[3], out int rMinN) ? rMinN : 0;
                    rate.changePriceMaxNon = int.TryParse(rateVals[4], out int rMaxN) ? rMaxN : 0;
                    rateList.Add(rate);
                }
                newCase.industryRates = rateList.ToArray();
            }
            else
            {
                newCase.industryRates = new Case.IndustryRate[0];
            }

            string savePath = $"Assets/Case/{caseName}.asset";
            AssetDatabase.CreateAsset(newCase, savePath);
            Debug.Log($"Case 생성 완료: {savePath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("CSV 임포트 완료!");
    }

    // CSV 한 줄을 쉼표로 분리 — 큰따옴표 안의 쉼표는 무시
    static string[] SplitCSVLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        string current = "";
        foreach (char c in line)
        {
            if (c == '"') inQuotes = !inQuotes;
            else if (c == ',' && !inQuotes) { result.Add(current); current = ""; }
            else current += c;
        }
        result.Add(current);
        return result.ToArray();
    }

    // 쉼표로 구분된 정수 문자열을 int 배열로 변환
    static int[] ParseIntArray(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new int[0];
        string[] parts = raw.Trim().Split(',');
        List<int> result = new List<int>();
        foreach (string part in parts)
            if (int.TryParse(part.Trim(), out int val))
                result.Add(val);
        return result.ToArray();
    }

    // 쉼표로 구분된 실수 문자열을 float 배열로 변환
    static float[] ParseFloatArray(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new float[0];
        string[] parts = raw.Trim().Split(',');
        List<float> result = new List<float>();
        foreach (string part in parts)
            if (float.TryParse(part.Trim(), out float val))
                result.Add(val);
        return result.ToArray();
    }

    // 쉼표로 구분된 문자열을 string 배열로 분리
    static string[] SplitArray(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new string[0];
        return raw.Trim().Split(',');
    }
}
