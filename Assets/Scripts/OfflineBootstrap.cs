#nullable enable
using Shared;
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
            UnityGlobal.Instance.Initialize();
            GameClient.Instance.Initialize(StaticDataRepository.Instance);
        }
    }
}
