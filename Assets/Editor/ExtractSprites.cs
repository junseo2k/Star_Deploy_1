using UnityEngine;
using UnityEditor;
using System.IO;

public class ExtractSprites : Editor
{
    [MenuItem("Tools/Extract Sprites From Folder")]
    static void ExtractSpritesFromFolder()
    {
        // 폴더 선택 다이얼로그
        string selectedFolder = EditorUtility.OpenFolderPanel(
            "스프라이트 추출할 폴더 선택",
            Application.dataPath,
            ""
        );

        if (string.IsNullOrEmpty(selectedFolder)) return;

        // Assets 상대 경로로 변환
        string relativePath = "Assets" + selectedFolder.Replace(Application.dataPath, "");

        string outputPath = Path.Combine(Application.dataPath, "..", "ExtractedSprites");
        Directory.CreateDirectory(outputPath);

        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { relativePath });
        int count = 0;

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

            if (sprite == null) continue;

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            bool wasReadable = importer.isReadable;
            if (!wasReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            Texture2D tex = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height, TextureFormat.RGBA32, false);
            Color[] pixels = sprite.texture.GetPixels(
                (int)sprite.rect.x,
                (int)sprite.rect.y,
                (int)sprite.rect.width,
                (int)sprite.rect.height);
            tex.SetPixels(pixels);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();

            // 원본 폴더 구조 유지해서 저장
            string subPath = assetPath.Replace(relativePath, "").TrimStart('/');
            string outFile = Path.Combine(outputPath, Path.GetDirectoryName(subPath), sprite.name + ".png");
            Directory.CreateDirectory(Path.GetDirectoryName(outFile));
            File.WriteAllBytes(outFile, png);

            if (!wasReadable)
            {
                importer.isReadable = false;
                importer.SaveAndReimport();
            }

            count++;
        }

        EditorUtility.DisplayDialog("완료", $"{count}개 스프라이트 추출 완료!\n저장 위치: {outputPath}", "확인");
    }
}