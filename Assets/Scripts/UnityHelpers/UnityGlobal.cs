#nullable enable
using DG.Tweening;
using Shared.Localizers;
using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using SamMul.Loggers;
using SamMul.ResourcePools;
using SamMul.Scenes;
using SamMul.UnityHelpers.SceneManagements;
using SamMul.UnityHelpers.Sounds;

namespace SamMul.UnityHelpers
{
    /// <summary>
    /// 유니티 리소스를 활용하는 전역 객체들을 담은 Facade입니다.
    /// </summary>
    public class UnityGlobal
    {
        private static UnityGlobal s_Instance = null!;
        public static UnityGlobal Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new UnityGlobal();
                }
                return s_Instance;
            }
        }

        private bool _hasInitialized = false;

        private SceneChanger _sceneChanger = null!;
        private SpriteAnimationManager _spriteAnimations = null!;
        private SoundManager _sounds = null!;

        public static SceneChanger Scenes => Instance._sceneChanger;
        public static SpriteAnimationManager SpriteAnimations => Instance._spriteAnimations;
        public static SoundManager Sounds => Instance._sounds;

        public void Initialize()
        {
            DOTween.Init(recycleAllByDefault: false, useSafeMode: true, logBehaviour: null);
            DOTween.SetTweensCapacity(7812, 1950);

            if (_hasInitialized)
            {
                return;
            }
            _hasInitialized = true;

            _sceneChanger = SceneChanger.Create();
            _spriteAnimations = new SpriteAnimationManager();

            _sounds = new SoundManager();
            _sounds.Initialize();
            _sceneChanger.StartCoroutine(_sounds.AudioMixerVolumeInitializationErrorWorkaround());

            this.InitializeFrameRate();
        }

        private void InitializeFrameRate()
        {
            // 프레임레이트를 고정하기 위해 vSync를 끈다.
            // 프레임레이트 조절의 책임이 더 늘어나게 된다면, 별도 클래스로 프레임레이트를 관리하도록 하자.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 50;
        }

        public void OnNewSceneAwaked(BaseScene newScene, BaseSceneUIRoot newUIRoot)
        {
            _sceneChanger.OnNewSceneAwaked(newScene, newUIRoot);
        }

        public void ClearBeforeChangingScene(Scene clearingScene, SceneType clearingSceneType)
        {
            _spriteAnimations.ClearBeforeChangingScene(clearingScene);
            _sounds.ClearBeforeChangingScene(clearingScene);
            ResourcePool.Instance.ClearBeforeChangingScene(clearingScene);

            if (clearingSceneType != SceneType.Loading)
            {
                DOTween.KillAll(complete: false);
            }
        }

        public void Update()
        {
            _spriteAnimations.Update();
            _sounds.CheckAndReleaseSoundObjects();
        }

        // 오프라인 데모: 서버 오류 처리기는 로그와 안내 팝업만 남긴다. (재시작 씬 없음)
        public static void HandleInternalServerError_LogAndRestart(string errorReason, string url, string appVersion, long accountId, ExceptionDispatchInfo? exception)
        {
            if (exception == null)
            {
                Log.I.Error(errorReason);
            }
            else
            {
                Log.I.Error(errorReason, exception.SourceException);
            }
            ShowOfflineErrorPopup("SE", errorReason);
        }

        public static void HandleInvalidSessionInfo(string requestName, string appVersion, long accountId)
        {
            Log.I.Warn($"[{requestName}] 세션정보가 유효하지 않음.");
            ShowOfflineErrorPopup("IS", requestName);
        }

        public static void HandleAbusingUser(string requestName, string appVersion, long accountId)
        {
            Log.I.Warn($"[{requestName}] 요청 검증 실패.");
            ShowOfflineErrorPopup("AB", requestName);
        }

        public static Task HandleNetworkErrorAndContinueRetry()
        {
            // 네트워크가 없으므로 즉시 재개한다.
            return Task.CompletedTask;
        }

        private static void ShowOfflineErrorPopup(string code, string detail)
        {
            var sceneUI = UnityGlobal.Scenes.GetCurrentSceneUI();
            sceneUI.AddErrorMessagePopup(
                Localizer.Instance.GetText("UI_INVALID_REQUEST") + code,
                Localizer.Instance.GetText("UI_ERROR_INTERNAL_ERROR"),
                Localizer.Instance.GetText("UI_OK"),
                detail, Application.version, 0,
                okButtonAction: () => { });
        }
    }
}
