#nullable enable
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Z.Scenes;

namespace Z.UnityHelpers.SceneManagements
{
    /// <summary>
    /// 씬 전환을 담당합니다. 씬은 Build Settings 에 SceneType 이름과 같은 이름으로 등록되어 있어야 합니다.
    /// Loading 씬이 등록되어 있으면 전환 중에 Loading 씬을 덧씌워 보여줍니다.
    /// </summary>
    public class SceneChanger : MonoBehaviour
    {
        private Coroutine? _loadingRoutine;
        private SceneType _nextSceneType = SceneType.Invalid;

        // 현재 씬. 전환 도중에는 새로 로드된 씬이 현재 씬이다. Loading 씬은 여기에 넣지 않는다.
        private BaseScene? _currentScene;
        // 유니티 오브젝트는 파괴되면 null 과 같아지므로, 타입을 별도로 기억한다.
        private SceneType _currentSceneType = SceneType.Invalid;
        private BaseSceneUIRoot? _currentSceneUIRoot;
        private BaseScene? _loadingScene;

        public bool CanChangeScene => _loadingRoutine == null;

        public TScene GetCurrentScene<TScene>() where TScene : BaseScene => (_currentScene as TScene)!;

        public TSceneUI GetCurrentSceneUI<TSceneUI>() where TSceneUI : BaseSceneUIRoot => (_currentSceneUIRoot as TSceneUI)!;

        public BaseSceneUIRoot GetCurrentSceneUI() => _currentSceneUIRoot!;

        public static SceneChanger Create()
        {
            var gameObject = new GameObject("@SceneChanger");
            DontDestroyOnLoad(gameObject);
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            return gameObject.AddComponent<SceneChanger>();
        }

        /// <summary>
        /// BaseScene.Awake 에서 호출됩니다. 새로 깨어난 씬을 현재 씬으로 등록합니다.
        /// </summary>
        public void OnNewSceneAwaked(BaseScene newScene, BaseSceneUIRoot newSceneUIRoot)
        {
            if (newScene.SceneType == SceneType.Loading)
            {
                _loadingScene = newScene;
                return;
            }

            bool isFirstScene = _currentSceneType == SceneType.Invalid;

            _currentScene = newScene;
            _currentSceneType = newScene.SceneType;
            _currentSceneUIRoot = newSceneUIRoot;

            if (isFirstScene)
            {
                // 앱 시작 후 첫 씬은 SceneChanger 를 거치지 않았으므로 여기서 바로 초기화한다.
                newScene.SwitchAudioListener(true);
                newScene.Initialize(null!);
                newScene.OnLoadingSceneRemoved();
            }
        }

        public void ChangeTo(SceneType sceneType, ISceneInitialData sceneInitialData, string sceneChangeReason)
        {
            Debug.Assert(sceneType == sceneInitialData.SceneType);

            if (_loadingRoutine != null)
            {
                throw new InvalidOperationException($"다른 씬[{_nextSceneType}] 로딩 중입니다. 현재 씬[{_currentSceneType}]에서 씬[{sceneType}] 전환 요청을 무시합니다. 이유: {sceneChangeReason}");
            }

            Debug.Log($"{_currentSceneType} 씬에서 {sceneType} 씬으로 전환합니다. 이유: {sceneChangeReason}");
            _nextSceneType = sceneType;
            _loadingRoutine = StartCoroutine(this.CorLoadScene(sceneType, sceneInitialData));
        }

        private IEnumerator CorLoadScene(SceneType nextSceneType, ISceneInitialData sceneInitialData)
        {
            try
            {
                var previousScene = _currentScene!;
                previousScene.SwitchAudioListener(false);

                string loadingSceneName = SceneType.Loading.ToString();
                if (Application.CanStreamedLevelBeLoaded(loadingSceneName) &&
                    !SceneManager.GetSceneByName(loadingSceneName).isLoaded)
                {
                    yield return SceneManager.LoadSceneAsync(loadingSceneName, LoadSceneMode.Additive);
                    _loadingScene?.SwitchAudioListener(true);
                }

                string nextSceneName = nextSceneType.ToString();
                var loadOp = SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Additive);
                while (!loadOp.isDone)
                {
                    (_loadingScene as LoadingScene)?.SetLoadingProgress(loadOp.progress);
                    yield return null;
                }

                // 새 씬의 BaseScene.Awake 가 OnNewSceneAwaked 를 호출하여 _currentScene 을 교체했어야 한다.
                Debug.Assert(_currentScene != null && _currentScene.SceneType == nextSceneType, "새 씬이 현재 씬으로 등록되지 않았습니다.");

                try
                {
                    previousScene.Clear();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                yield return SceneManager.UnloadSceneAsync(previousScene.gameObject.scene);

                // 새 씬을 ActiveScene 으로 두어야 초기화 중 생성되는 오브젝트가 새 씬에 들어간다.
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(nextSceneName));

                _loadingScene?.SwitchAudioListener(false);
                _currentScene!.SwitchAudioListener(true);
                _currentScene.Initialize(sceneInitialData);

                yield return null;

                if (SceneManager.GetSceneByName(loadingSceneName).isLoaded)
                {
                    yield return SceneManager.UnloadSceneAsync(loadingSceneName);
                }

                _currentScene.OnLoadingSceneRemoved();
            }
            finally
            {
                _loadingRoutine = null;
                _loadingScene = null;
                _nextSceneType = SceneType.Invalid;
            }
        }
    }
}
