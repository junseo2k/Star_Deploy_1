using System;
using System.Collections.Generic;
using Delusion.UI.Vault;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Delusion.UI.VaultEditor
{
    [CustomEditor(typeof(DelusionVaultScreen))]
    public sealed class DelusionVaultScreenEditor : Editor
    {
        // 이 프로젝트가 이미 쓰고 있는 한글 TMP 폰트. 자산 탭도 뉴스 탭과 같은 폰트를 쓴다.
        private const string PrefabFolder = "Assets/Delusion/UI/Vault/Prefabs";
        private const string HoldingRowPrefabPath = PrefabFolder + "/VaultHoldingRow.prefab";

        // 폰트 에셋이 들어 있을 만한 폴더들. 위에 있는 것부터 찾는다
        private static readonly string[] FontSearchFolders =
        {
            "Assets/Font",
            "Assets/Delusion/UI/News/Fonts",
            "Assets/Delusion/UI/Vault/Fonts",
        };

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(12f);

            var screen = (DelusionVaultScreen)target;

            if (!screen.HasKoreanFont)
            {
                EditorGUILayout.HelpBox(
                    "한글 폰트가 연결되지 않았습니다. 이 상태로 UI를 만들면 글자가 모두 □ 로 보입니다.\n" +
                    "아래 버튼을 누르면 " + "프로젝트의 한글 TMP 폰트를 찾아 자동으로 연결합니다.",
                    MessageType.Warning);

                if (GUILayout.Button("프로젝트 한글 폰트 자동 연결", GUILayout.Height(30f)))
                {
                    EnsureKoreanFont(screen, true);
                    screen.Rebuild();
                    EditorUtility.SetDirty(screen);
                }

                EditorGUILayout.Space(8f);
            }

            EditorGUILayout.HelpBox(
                "이 컴포넌트는 자산 '탭 내용'만 그립니다. 사이드바·상단 상태바·배경은 기존 화면 것을 그대로 씁니다.\n" +
                "보유 종목은 평가액 순으로 자동 정렬되고 한 화면에 4개가 보이며, 5번째부터 ScrollRect로 내려갑니다.",
                MessageType.Info);

            if (GUILayout.Button("확정 자산 탭 UI 생성 / 갱신", GUILayout.Height(42f)))
            {
                EnsureKoreanFont(screen, false);
                Undo.RegisterFullObjectHierarchyUndo(screen.gameObject, "Build Delusion Vault UI");
                screen.Rebuild();
                EditorUtility.SetDirty(screen);
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                "아래 버튼을 누르면 지금 코드가 그리는 '보유 종목 한 줄'을 그대로 프리팹으로 떠서 연결합니다.\n" +
                "그 뒤로는 줄의 생김새를 프리팹에서 눈으로 편집하면 되고, 갱신해도 편집한 내용이 유지됩니다.",
                MessageType.Info);

            if (GUILayout.Button("보유 종목 줄 프리팹 만들기 (지금 디자인 그대로)", GUILayout.Height(30f)))
                CreateHoldingRowPrefab(screen);
        }

        // 코드가 그리던 줄 하나를 그대로 프리팹으로 저장하고 화면에 연결한다
        private static void CreateHoldingRowPrefab(DelusionVaultScreen screen)
        {
            if (System.IO.File.Exists(HoldingRowPrefabPath) &&
                !EditorUtility.DisplayDialog(
                    "보유 종목 줄 프리팹",
                    HoldingRowPrefabPath + " 가 이미 있습니다.\n\n덮어쓰면 프리팹에 직접 해둔 편집이 사라집니다.",
                    "덮어쓰기", "취소"))
                return;

            if (!EnsurePrefabFolder())
                return;

            // 화면 아래에 임시 부모를 만들고 거기에 한 줄만 그린다
            var holder = new GameObject("__TempHoldingRow", typeof(RectTransform));
            var holderRect = holder.GetComponent<RectTransform>();
            holderRect.SetParent(screen.transform, false);

            var sample = new VaultHoldingItem
            {
                companyName = "블레이즈마인 광산",
                logoResource = "01_blazemine-mining",
                quantity = 2,
                averagePrice = 400,
                currentPrice = 380
            };

            var rowObject = screen.CreateHoldingRowTemplate(holderRect, sample);
            if (rowObject == null)
            {
                DestroyImmediate(holder);
                Debug.LogError("줄을 만들지 못했습니다. 폰트가 연결돼 있는지 확인하세요.");
                return;
            }

            rowObject.name = "VaultHoldingRow";

            // 저장되지 않는 임시 재질(굵게용 복사본)이 프리팹에 딸려가지 않도록 원래 재질로 되돌린다
            foreach (var text in rowObject.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.font != null && text.fontSharedMaterial != null &&
                    text.fontSharedMaterial.hideFlags == HideFlags.DontSave)
                    text.fontSharedMaterial = text.font.material;
            }

            var row = rowObject.GetComponent<VaultHoldingRow>();
            if (row == null)
                row = rowObject.AddComponent<VaultHoldingRow>();
            row.AutoWire();   // 자식 이름(Logo, CompanyName, Prices ...)을 보고 칸을 자동으로 채운다

            var prefab = PrefabUtility.SaveAsPrefabAsset(rowObject, HoldingRowPrefabPath);
            DestroyImmediate(holder);

            if (prefab == null)
            {
                Debug.LogError("프리팹 저장에 실패했습니다: " + HoldingRowPrefabPath);
                return;
            }

            Undo.RecordObject(screen, "Assign Holding Row Prefab");
            screen.SetHoldingRowPrefab(prefab.GetComponent<VaultHoldingRow>());
            EditorUtility.SetDirty(screen);
            screen.Rebuild();

            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            Debug.Log("보유 종목 줄 프리팹을 만들어 연결했습니다: " + HoldingRowPrefabPath);
        }

        private static bool EnsurePrefabFolder()
        {
            if (AssetDatabase.IsValidFolder(PrefabFolder))
                return true;

            if (!AssetDatabase.IsValidFolder("Assets/Delusion/UI/Vault"))
            {
                Debug.LogError("Assets/Delusion/UI/Vault 폴더를 찾지 못했습니다.");
                return false;
            }

            AssetDatabase.CreateFolder("Assets/Delusion/UI/Vault", "Prefabs");
            return true;
        }

        // 기존 Canvas 안에 자산 탭 화면을 자식으로 붙인다.
        // 예전 버전처럼 Canvas를 새로 만들지 않으므로 기존 UI를 덮지 않는다.
        [MenuItem("Tools/Delusion/자산 탭 화면 만들기 (선택한 오브젝트 아래)")]
        private static void CreateVaultScreenUnderSelection()
        {
            var parent = Selection.activeGameObject;

            if (parent == null || parent.GetComponentInParent<Canvas>() == null)
            {
                EditorUtility.DisplayDialog(
                    "자산 탭 화면 만들기",
                    "먼저 Hierarchy에서 자산 탭이 들어갈 부모 오브젝트를 선택하세요.\n\n" +
                    "기존 Canvas 안에 있는 자산 패널(예: PortfolioPanel)을 고르면 그 아래에 만들어집니다.",
                    "확인");
                return;
            }

            if (parent.GetComponent<RectTransform>() == null)
            {
                EditorUtility.DisplayDialog(
                    "자산 탭 화면 만들기",
                    "선택한 오브젝트에 RectTransform이 없습니다. UI 오브젝트를 선택하세요.",
                    "확인");
                return;
            }

            var screenObject = new GameObject("DelusionVaultScreen", typeof(RectTransform), typeof(DelusionVaultScreen));
            Undo.RegisterCreatedObjectUndo(screenObject, "Create Delusion Vault Screen");

            // 부모 영역을 꽉 채우도록 늘린다. 좌표가 1920×1080 기준이라 부모도 전체 화면 크기여야 한다.
            var rect = screenObject.GetComponent<RectTransform>();
            rect.SetParent(parent.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;

            var screen = screenObject.GetComponent<DelusionVaultScreen>();

            if (!EnsureKoreanFont(screen, false))
            {
                Debug.LogWarning(
                    "쓸 만한 한글 폰트를 찾지 못했습니다. 한글·영문·숫자가 모두 들어간 TMP 폰트가 있는지 확인한 뒤 " +
                    "DelusionVaultScreen의 Korean Regular에 직접 연결하고 '확정 자산 탭 UI 생성 / 갱신'을 누르세요.");
            }

            screen.Rebuild();
            Selection.activeGameObject = screenObject;
            Debug.Log("자산 탭 화면을 " + parent.name + " 아래에 만들었습니다.");
        }

        // 폰트가 비어 있으면 프로젝트의 한글 폰트를 찾아 연결한다.
        // force가 true면 이미 연결돼 있어도 다시 연결한다.
        private static bool EnsureKoreanFont(DelusionVaultScreen screen, bool force)
        {
            var bold = FindProjectKoreanBoldFont();

            // 본문 폰트가 이미 연결돼 있는 경우 — 나중에 만든 굵은 폰트만 따로 채워 넣는다
            if (screen.HasKoreanFont && !force)
            {
                if (bold == null || screen.HasRealBoldFont)
                    return true;

                Undo.RecordObject(screen, "Assign Korean Bold Font");
                screen.SetFonts(screen.KoreanRegular, bold, false);
                EditorUtility.SetDirty(screen);
                return true;
            }

            var font = FindProjectKoreanFont();
            if (font == null)
                return false;

            Undo.RecordObject(screen, "Assign Korean Font");
            // 굵은 폰트가 있으면 같이 연결한다 (없으면 null → 화면이 Bold Thickness로 대신 두껍게 그린다)
            screen.SetFonts(font, bold, false);   // 연결만 하고, 다시 그리는 건 호출한 쪽이 한다
            EditorUtility.SetDirty(screen);
            return true;
        }

        // 굵은 한글 폰트 찾기 — 이름에 Noto가 들어가고 Bold나 700이 붙은 TMP 폰트를 고른다
        private static TMP_FontAsset FindProjectKoreanBoldFont()
        {
            foreach (var font in FindKoreanFontAssets())
            {
                var path = AssetDatabase.GetAssetPath(font);
                if (path.IndexOf("Bold", StringComparison.OrdinalIgnoreCase) >= 0 || path.Contains("700"))
                    return font;
            }
            return null;
        }

        // 본문용 한글 폰트 찾기 — 쓸 만한 것 중 첫 번째
        private static TMP_FontAsset FindProjectKoreanFont()
        {
            foreach (var font in FindKoreanFontAssets())
                return font;
            return null;
        }

        // 지정한 폴더들에서 "쓸 만한" 한글 TMP 폰트를 순서대로 돌려준다
        private static IEnumerable<TMP_FontAsset> FindKoreanFontAssets()
        {
            foreach (var folder in FontSearchFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                    continue;

                foreach (var guid in AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { folder }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.IndexOf("Noto", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                    if (IsUsableKoreanFont(font))
                        yield return font;
                }
            }
        }

        // 한글·영문·숫자를 모두 그릴 수 있는 폰트인지 확인한다
        // 아틀라스 크기가 모자라 한글이 110자만 구워진 폰트 같은 것을 걸러내기 위한 안전장치다
        // (그런 폰트를 물리면 나머지 글자가 대체 폰트로 넘어가면서 화면에서 사라진다)
        private static bool IsUsableKoreanFont(TMP_FontAsset font)
        {
            if (font == null)
                return false;

            // tryAddCharacter: true — Dynamic 폰트는 원본 ttf에서 글자를 가져올 수 있는지까지 확인한다
            return font.HasCharacter('가', false, true)
                && font.HasCharacter('힣', false, true)
                && font.HasCharacter('A', false, true)
                && font.HasCharacter('0', false, true);
        }
    }
}
