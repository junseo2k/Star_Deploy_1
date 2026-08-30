using Delusion.UI.MarketCurrent;
using UnityEditor;
using UnityEngine;

namespace Delusion.UI.MarketCurrentEditor
{
    [CustomEditor(typeof(CurrentMarketScreen))]
    public sealed class CurrentMarketScreenEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(12f);
            EditorGUILayout.HelpBox(
                "2026-08-23 첨부 화면 전체를 새로 생성합니다. 기존 홈 UI나 예전 시장 UI를 재사용하지 않습니다. " +
                "화면에 보이는 로고 슬롯은 118×124px이며, 브루트스틸 철강을 포함한 최신 8개 로고가 연결되어 있습니다.",
                MessageType.Info);

            if (GUILayout.Button("현재 첨부본 시장 UI 생성 / 갱신", GUILayout.Height(40f)))
            {
                var screen = (CurrentMarketScreen)target;
                Undo.RegisterFullObjectHierarchyUndo(screen.gameObject, "Build Current Delusion Market UI");
                screen.Rebuild();
                EditorUtility.SetDirty(screen);
            }
        }
    }
}
