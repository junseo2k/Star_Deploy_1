using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// 프로젝트에 이미 있는 Case 에셋들을 스캔해서 산업군 단위 NewsTemplate으로 변환
// CSV를 전혀 쓰지 않고, Case.affectedCase에 이미 연결된 실제 Stock 참조를 그대로 사용
// (이름 문자열 매칭이 없어서 인코딩/오탈자 문제가 원천적으로 없음)
public class CaseToTemplateConverter : EditorWindow
{
    [MenuItem("Tools/Convert Cases to Templates")]
    public static void Convert()
    {
        string[] caseGuids = AssetDatabase.FindAssets("t:Case");
        Debug.Log($"프로젝트에서 찾은 Case 에셋 수: {caseGuids.Length}");

        // 타입별 정확한 개수 확인 (추측 대신 직접 카운트)
        Dictionary<Case.NewsType, int> typeCounts = new Dictionary<Case.NewsType, int>();
        foreach (string g in caseGuids)
        {
            Case cc = AssetDatabase.LoadAssetAtPath<Case>(AssetDatabase.GUIDToAssetPath(g));
            if (cc == null) continue;
            if (!typeCounts.ContainsKey(cc.newsType)) typeCounts[cc.newsType] = 0;
            typeCounts[cc.newsType]++;
        }
        foreach (var kv in typeCounts)
            Debug.Log($"타입별 개수 - {kv.Key} : {kv.Value}개");

        // 산업군 이름 -> 그 산업군에 속한 이벤트 목록
        Dictionary<string, List<NewsTemplate.EventEntry>> grouped = new Dictionary<string, List<NewsTemplate.EventEntry>>();
        int convertedCount = 0;

        foreach (string guid in caseGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Case c = AssetDatabase.LoadAssetAtPath<Case>(path);
            if (c == null) continue;

            // 소형 뉴스만 변환 (대형 뉴스는 industryRates를 쓰는 별개 구조라 대상 아님)
            if (c.newsType != Case.NewsType.SmallCompanyNews && c.newsType != Case.NewsType.SmallIndustryNews)
                continue;

            if (c.affectedCase == null || c.affectedCase.Length == 0)
            {
                Debug.LogWarning($"[{c.CaseName}] affectedCase가 비어있어서 변환 불가 (영향받는 Stock 연결 필요)");
                continue;
            }

            // affectedCase 배열의 각 Stock마다 이벤트 하나씩 생성 (ChangePriceMin/Max와 인덱스 1:1 대응)
            for (int i = 0; i < c.affectedCase.Length; i++)
            {
                Stock stock = c.affectedCase[i];
                if (stock == null) continue;

                int min = (c.ChangePriceMin != null && i < c.ChangePriceMin.Length) ? c.ChangePriceMin[i] : 0;
                int max = (c.ChangePriceMax != null && i < c.ChangePriceMax.Length) ? c.ChangePriceMax[i] : 0;

                NewsTemplate.EventEntry entry = new NewsTemplate.EventEntry
                {
                    targetStock = stock,
                    eventText = c.CaseDescription,
                    isPositive = (min + max) >= 0,
                    changeMin = min,
                    changeMax = max
                };

                string industry = stock.IndustryTag;
                if (!grouped.ContainsKey(industry))
                    grouped[industry] = new List<NewsTemplate.EventEntry>();
                grouped[industry].Add(entry);
                convertedCount++;
            }
        }

        // 산업군별로 NewsTemplate 에셋 생성 또는 기존 에셋에 이어붙이기
        if (!AssetDatabase.IsValidFolder("Assets/NewsTemplates"))
            AssetDatabase.CreateFolder("Assets", "NewsTemplates");

        foreach (var pair in grouped)
        {
            string assetPath = $"Assets/NewsTemplates/Template_{pair.Key}.asset";
            NewsTemplate template = AssetDatabase.LoadAssetAtPath<NewsTemplate>(assetPath);
            bool isNew = template == null;
            if (isNew) template = ScriptableObject.CreateInstance<NewsTemplate>();

            // 재실행 시 중복 추가되니, 값 수정 후 다시 돌릴 거면 Assets/NewsTemplates 폴더를 지우고 실행할 것
            List<NewsTemplate.EventEntry> combined = new List<NewsTemplate.EventEntry>();
            if (!isNew) combined.AddRange(template.events);
            combined.AddRange(pair.Value);
            template.events = combined.ToArray();

            if (isNew) AssetDatabase.CreateAsset(template, assetPath);
            else EditorUtility.SetDirty(template);

            Debug.Log($"{assetPath} : 이벤트 {pair.Value.Count}개 추가 (총 {template.events.Length}개)");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"변환 완료! 총 {convertedCount}개 이벤트를 {grouped.Count}개 산업군 템플릿으로 옮겼습니다.");
    }
}