using System;
using Delusion.UI.News;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Delusion.UI.NewsEditor
{
    [CustomEditor(typeof(DelusionNewsScreen))]
    public sealed class DelusionNewsScreenEditor : Editor
    {
        // 이 프로젝트가 이미 쓰고 있는 한글 TMP 폰트. 뉴스 탭도 같은 폰트를 쓴다.
        private const string ProjectKoreanFontPath = "Assets/Font/NotoSansKR-VariableFont_wght SDF.asset";
        private const string FontSearchFolder = "Assets/Font";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(12f);

            var screen = (DelusionNewsScreen)target;

            if (!screen.HasKoreanFont)
            {
                EditorGUILayout.HelpBox(
                    "한글 폰트가 연결되지 않았습니다. 이 상태로 UI를 만들면 글자가 모두 □ 로 보입니다.\n" +
                    "아래 버튼을 누르면 " + ProjectKoreanFontPath + " 를 자동으로 연결합니다.",
                    MessageType.Warning);

                if (GUILayout.Button("프로젝트 한글 폰트 자동 연결", GUILayout.Height(30f)))
                    EnsureKoreanFont(screen, true);

                EditorGUILayout.Space(8f);
            }

            EditorGUILayout.HelpBox(
                "이 컴포넌트는 뉴스 '탭 내용'만 그립니다. 사이드바·상단 상태바·배경은 기존 화면 것을 그대로 씁니다.\n" +
                "시대 배경만 이미지이고 나머지 문구와 수치는 전부 런타임 데이터입니다.",
                MessageType.Info);

            if (GUILayout.Button("뉴스 탭 UI 생성 / 갱신", GUILayout.Height(42f)))
            {
                EnsureKoreanFont(screen, false);
                Undo.RegisterFullObjectHierarchyUndo(screen.gameObject, "Build Delusion News UI");
                screen.Rebuild();
                EditorUtility.SetDirty(screen);
            }

            if (GUILayout.Button("뉴스 없는 날 미리보기", GUILayout.Height(30f)))
            {
                EnsureKoreanFont(screen, false);
                Undo.RegisterFullObjectHierarchyUndo(screen.gameObject, "Preview Quiet News Day");
                screen.ShowQuietDay();
                EditorUtility.SetDirty(screen);
            }
        }

        // 기존 Canvas 안에 뉴스 탭 화면을 자식으로 붙인다.
        // 예전 버전처럼 Canvas를 새로 만들지 않으므로 기존 UI를 덮지 않는다.
        [MenuItem("Tools/Delusion/뉴스 탭 화면 만들기 (선택한 오브젝트 아래)")]
        private static void CreateNewsScreenUnderSelection()
        {
            var parent = Selection.activeGameObject;

            if (parent == null || parent.GetComponentInParent<Canvas>() == null)
            {
                EditorUtility.DisplayDialog(
                    "뉴스 탭 화면 만들기",
                    "먼저 Hierarchy에서 뉴스 탭이 들어갈 부모 오브젝트를 선택하세요.\n\n" +
                    "기존 Canvas 안에 있는 뉴스 패널(예: NewsPanel)을 고르면 그 아래에 만들어집니다.",
                    "확인");
                return;
            }

            if (parent.GetComponent<RectTransform>() == null)
            {
                EditorUtility.DisplayDialog(
                    "뉴스 탭 화면 만들기",
                    "선택한 오브젝트에 RectTransform이 없습니다. UI 오브젝트를 선택하세요.",
                    "확인");
                return;
            }

            var screenObject = new GameObject("DelusionNewsScreen", typeof(RectTransform), typeof(DelusionNewsScreen));
            Undo.RegisterCreatedObjectUndo(screenObject, "Create Delusion News Screen");

            // 부모 영역을 꽉 채우도록 늘린다. 좌표가 1920×1080 기준이라 부모도 전체 화면 크기여야 한다.
            var rect = screenObject.GetComponent<RectTransform>();
            rect.SetParent(parent.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;

            var screen = screenObject.GetComponent<DelusionNewsScreen>();

            if (!EnsureKoreanFont(screen, false))
            {
                Debug.LogWarning(
                    "한글 폰트를 찾지 못했습니다. " + ProjectKoreanFontPath + " 가 있는지 확인한 뒤 " +
                    "DelusionNewsScreen의 Korean Regular에 직접 연결하고 '뉴스 탭 UI 생성 / 갱신'을 누르세요.");
            }

            screen.Rebuild();
            Selection.activeGameObject = screenObject;
            Debug.Log("뉴스 탭 화면을 " + parent.name + " 아래에 만들었습니다.");
        }

        // 폰트가 비어 있으면 프로젝트의 한글 폰트를 찾아 연결한다.
        // force가 true면 이미 연결돼 있어도 다시 연결한다.
        private static bool EnsureKoreanFont(DelusionNewsScreen screen, bool force)
        {
            if (screen.HasKoreanFont && !force)
                return true;

            var font = FindProjectKoreanFont();
            if (font == null)
                return false;

            Undo.RecordObject(screen, "Assign Korean Font");
            screen.SetFonts(font);
            EditorUtility.SetDirty(screen);
            return true;
        }

        private static TMP_FontAsset FindProjectKoreanFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ProjectKoreanFontPath);
            if (font != null)
                return font;

            // 파일 이름이 바뀐 경우를 대비해 Assets/Font 안에서 이름에 Noto가 들어간 TMP 폰트를 찾는다.
            if (!AssetDatabase.IsValidFolder(FontSearchFolder))
                return null;

            foreach (var guid in AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { FontSearchFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.IndexOf("Noto", StringComparison.OrdinalIgnoreCase) >= 0)
                    return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            }

            return null;
        }
    }
}
