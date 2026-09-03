#nullable enable
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Demo.Editor
{
    /// <summary>
    /// Assets/Fonts 의 메이플스토리 서체(넥슨 무료 배포)로 TextMeshPro 한글 폰트 에셋을 만든다.
    /// 런타임에는 OfflineBootstrap 이 Resources/Fonts/KoreanFallback SDF 를 읽어 TMP 폴백에 등록한다.
    /// 메뉴: Demo > Build Korean Font  /  배치: -executeMethod Demo.Editor.DemoKoreanFontBuilder.BuildAll
    /// </summary>
    public static class DemoKoreanFontBuilder
    {
        public const string FONT_ASSET_RESOURCE_PATH = "Fonts/KoreanFallback SDF";

        private const string SOURCE_FONT_PATH = "Assets/Fonts/Maplestory Light.ttf";

        [MenuItem("Demo/Build Korean Font")]
        public static void BuildAll()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(SOURCE_FONT_PATH);
            if (font == null)
            {
                Debug.LogError($"[DemoKoreanFontBuilder] 폰트 파일이 없습니다: {SOURCE_FONT_PATH}");
                return;
            }

            var fontAsset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
            if (fontAsset == null)
            {
                Debug.LogError("[DemoKoreanFontBuilder] TMP 폰트 에셋 생성 실패");
                return;
            }

            Directory.CreateDirectory("Assets/Resources/Fonts");
            string outputPath = "Assets/Resources/" + FONT_ASSET_RESOURCE_PATH + ".asset";
            AssetDatabase.DeleteAsset(outputPath);
            fontAsset.name = "KoreanFallback SDF";
            AssetDatabase.CreateAsset(fontAsset, outputPath);
            fontAsset.material.name = fontAsset.name + " Material";
            fontAsset.atlasTexture.name = fontAsset.name + " Atlas";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[DemoKoreanFontBuilder] 생성 완료: {outputPath} (원본 {SOURCE_FONT_PATH})");
        }
    }
}
