using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.IO;

public class PrefabToPNG : Editor
{
    [MenuItem("Tools/Prefab UI to PNG")]
    static void CapturePrefabsToPNG()
    {
        string selectedFolder = EditorUtility.OpenFolderPanel(
            "Prefab 폴더 선택",
            Application.dataPath,
            ""
        );
        if (string.IsNullOrEmpty(selectedFolder)) return;

        string relativePath = "Assets" + selectedFolder.Replace(Application.dataPath, "");
        string outputPath = Path.Combine(Application.dataPath, "..", "ExtractedPrefabs");
        Directory.CreateDirectory(outputPath);

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { relativePath });
        int count = 0;

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null) continue;

            try
            {
                // Camera 먼저 생성
                GameObject camGO = new GameObject("TempCam");
                Camera cam = camGO.AddComponent<Camera>();
                cam.orthographic = true;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.clear;
                cam.transform.position = new Vector3(0, 0, -10);

                // Canvas 생성
                GameObject canvasGO = new GameObject("TempCanvas");
                Canvas canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 5f;
                canvasGO.AddComponent<GraphicRaycaster>();

                // Prefab 인스턴스화
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvasGO.transform);

                // RectTransform 크기 및 위치 가져오기
                RectTransform rect = instance.GetComponent<RectTransform>();
                int width = 512, height = 512;

                if (rect != null)
                {
                    // anchoredPosition 초기화해서 중앙 정렬
                    rect.anchoredPosition = Vector2.zero;
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);

                    width = Mathf.Max((int)rect.sizeDelta.x, 1);
                    height = Mathf.Max((int)rect.sizeDelta.y, 1);
                }

                // Canvas 해상도 맞추기
                CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

                cam.orthographicSize = height / 2f;
                cam.aspect = (float)width / height;

                // 렌더링
                RenderTexture rt = new RenderTexture(width, height, 32, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt;
                cam.Render();

                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();

                byte[] png = tex.EncodeToPNG();
                string outFile = Path.Combine(outputPath, prefab.name + ".png");
                File.WriteAllBytes(outFile, png);
                count++;

                // 정리
                RenderTexture.active = null;
                cam.targetTexture = null;
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(camGO);
                Object.DestroyImmediate(canvasGO);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"{prefab.name} 실패: {e.Message}");
            }
        }

        EditorUtility.DisplayDialog("완료", $"{count}개 Prefab PNG 추출 완료!\n저장 위치: {outputPath}", "확인");
    }
}