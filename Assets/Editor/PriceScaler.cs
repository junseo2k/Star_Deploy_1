using UnityEditor;
using UnityEngine;

// 주가·회사 재무 숫자를 한 번에 배율만큼 곱해주는 일회성 에디터 도구
// 40개 Stock 에셋과 40개 BusinessProject 에셋을 손으로 고치지 않아도 되게 만든 것
//
// 쓰는 법: Unity 상단 메뉴 Tools > 밸런스 > 주가·재무 4배 스케일 클릭 → 확인
// 한 번만 눌러야 한다. 두 번 누르면 16배가 된다 (되돌리기는 배율 0.25로 다시 실행하거나 버전 관리로 복구)
public static class PriceScaler
{
    // 곱할 배율 — 주가를 4배로 뻥튀기하면서 회사 경제(매출·운영비·금고·사업 투자비)도 같은 배율로 맞춘다
    // 전부 같은 배율이라 순수입 비율·목표 주가 비율·배당 수익률이 스케일 전과 똑같이 유지된다
    private const float SCALE = 4f;

    // Stock 에셋에서 배율을 곱할 정수 필드들 (주가 + 회사 재무)
    private static readonly string[] StockFields =
    {
        "StockBasePrice",   // 변동 전 기준 주가 — StockCurrentPrice가 게임 시작 시 이 값에서 나온다
        "baseRevenue",      // 평소 매출
        "operatingCost",    // 분기 고정 운영비
        "baseAsset",        // 기본 금고 (0이면 자동 산정이라 곱해도 그대로 0)
    };

    // BusinessProject 에셋에서 배율을 곱할 정수 필드들 (사업 규모)
    private static readonly string[] BusinessFields =
    {
        "investmentCost",   // 착수 시 금고에서 빠지는 투자비
        "revenueBoost",     // 성공 시 매출 영구 증가량
        "revenueDrop",      // 실패 시 매출 영구 감소량
        "assetBoost",       // 성공 시 금고 증가량
    };

    [MenuItem("Tools/밸런스/주가·재무 4배 스케일")]
    public static void ScaleAll()
    {
        bool ok = EditorUtility.DisplayDialog(
            "주가·재무 스케일",
            $"모든 Stock / BusinessProject 에셋의 금액 필드를 {SCALE}배로 곱합니다.\n" +
            "한 번만 실행하세요. 두 번 누르면 배로 더 곱해집니다.\n\n계속할까요?",
            "실행", "취소");
        if (!ok) return;

        int stockCount = ScaleAssets<Stock>(StockFields);
        int businessCount = ScaleAssets<BusinessProject>(BusinessFields);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[PriceScaler] 완료 — Stock {stockCount}개, BusinessProject {businessCount}개 에셋을 {SCALE}배로 스케일했습니다.");
        EditorUtility.DisplayDialog("완료", $"Stock {stockCount}개, BusinessProject {businessCount}개를 {SCALE}배로 스케일했습니다.", "확인");
    }

    // 지정한 타입의 모든 에셋을 찾아, 넘겨받은 정수 필드들에 배율을 곱한다
    private static int ScaleAssets<T>(string[] fields) where T : ScriptableObject
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        int changed = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) continue;

            SerializedObject so = new SerializedObject(asset);
            bool touched = false;

            foreach (string fieldName in fields)
            {
                SerializedProperty prop = so.FindProperty(fieldName);
                if (prop == null || prop.propertyType != SerializedPropertyType.Integer) continue;

                // 반올림해서 정수로 다시 넣는다 (4배라 딱 떨어지지만, 다른 배율을 쓸 때를 대비)
                prop.intValue = Mathf.RoundToInt(prop.intValue * SCALE);
                touched = true;
            }

            if (touched)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(asset);
                changed++;
            }
        }

        return changed;
    }
}
