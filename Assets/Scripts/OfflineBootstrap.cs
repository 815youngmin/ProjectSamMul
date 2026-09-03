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
        /// 기본 TMP 폰트에는 한글 글리프가 없다. Demo > Build Korean Font 로 만든 폰트 에셋(저장소 미포함)을 TMP 전역 폴백에 등록한다.
        /// </summary>
        private static void RegisterKoreanFontFallback()
        {
            var fontAsset = Resources.Load<TMPro.TMP_FontAsset>("Fonts/KoreanFallback SDF");
            if (fontAsset == null)
            {
                Debug.LogWarning("한글 폴백 폰트가 없습니다. 에디터 메뉴 Demo > Build Korean Font 를 실행하세요. (한글이 □로 표시됩니다)");
                return;
            }
            if (!TMPro.TMP_Settings.fallbackFontAssets.Contains(fontAsset))
            {
                TMPro.TMP_Settings.fallbackFontAssets.Add(fontAsset);
            }
        }
    }
}
