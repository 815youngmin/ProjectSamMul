#nullable enable
using Shared;
using UnityEngine;
using Shared.StaticDatas;
using SamMul.GameClients;
using SamMul.Localizations;
using SamMul.ResourcePools;
using SamMul.UnityHelpers;

namespace SamMul
{
    /// <summary>
    /// 서버 없이 동작하는 데모의 앱 최초 초기화.
    /// 원래는 인증·시트 데이터 다운로드·분석 서비스 초기화가 있던 자리로, 로컬 정적 데이터만 읽어 게임을 구동한다.
    /// </summary>
    public static class OfflineBootstrap
    {
        private static bool s_initialized;

        public static void EnsureInitialized()
        {
            if (s_initialized)
            {
                return;
            }
            s_initialized = true;

            // 정적 데이터: Resources/StaticData/*.json (Shared 대체 구현이 로드한다)
            SharedInitializer.Initialize(LocalizedText.GetCurrentLanguageSetting());

            ResourcePool.Initialize();
            RegisterKoreanFontFallback();
            UnityGlobal.Instance.Initialize();
            GameClient.Instance.Initialize(StaticDataRepository.Instance);
        }

        /// <summary>
        /// 기본 TMP 폰트에는 한글 글리프가 없으므로, OS 폰트로 동적 SDF 폰트를 만들어 전역 폴백에 추가한다.
        /// 저장소에 폰트 파일을 포함하지 않기 위한 런타임 처리다.
        /// </summary>
        private static void RegisterKoreanFontFallback()
        {
            foreach (var candidate in new[] { "Malgun Gothic", "NanumGothic", "Noto Sans KR", "Apple SD Gothic Neo" })
            {
                var osFont = Font.CreateDynamicFontFromOSFont(candidate, 32);
                if (osFont == null)
                {
                    continue;
                }
                var fontAsset = TMPro.TMP_FontAsset.CreateFontAsset(osFont, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, TMPro.AtlasPopulationMode.Dynamic);
                if (fontAsset == null)
                {
                    continue;
                }
                fontAsset.name = candidate + " (runtime)";
                TMPro.TMP_Settings.fallbackFontAssets.Add(fontAsset);
                return;
            }
            Debug.LogWarning("한글을 지원하는 OS 폰트를 찾지 못했습니다. 한글이 □로 표시될 수 있습니다.");
        }
    }
}
