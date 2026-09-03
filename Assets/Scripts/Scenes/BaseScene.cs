using System;
using System.Threading;
using UnityEngine;
using SamMul.GameClients;
using SamMul.GameClients.Cameras;
using SamMul.UnityHelpers;
using SamMul.UnityHelpers.SceneManagements;
using Object = System.Object;

namespace SamMul.Scenes
{

    public enum SceneType
    {
        Invalid = 0,
        Loading,
        Lobby,
        Stage,
    }

    /// <summary>
    /// 씬을 초기화할 때 필요한 정보를 전달합니다.
    /// 씬마다 필요한 정보가 다르기 때문에, 구체화 클래스를 구현하여 전달합니다.
    /// </summary>
    public interface ISceneInitialData
    {
        public SceneType SceneType { get; }
    }

    /// <summary>
    /// 우리가 사용할 Scene은 모두 BaseScene을 상속받아 구현해야 한다.
    /// 싱글턴 인스턴스 초기화 등 게임 관리에 필요한 기본 제약을 구현하고 있다.
    /// </summary>
    public abstract class BaseScene : MonoBehaviour
    {
        public abstract SceneType SceneType { get; }

        // Sibling 관계에 있는 "@UIRoot"의 스크립트참조
        public BaseSceneUIRoot UI { get; private set; }

        private TimeScaleController _timeScaleController;
        public TimeScaleController TimeScaleController => _timeScaleController;

        /// <summary>
        /// 씬이 정리되었는지 여부. 정리된 뒤로부터는 씬을 접근해서는 안 된다. 
        /// </summary>
        public bool IsSceneCleared => _isSceneCleared;
        private bool _isSceneCleared = false;

        /// <summary>
        /// 씬이 초기화되었는지 여부. 스크립트 레이어에서 초기화됨을 명시
        /// </summary>
        public bool IsSceneInitialized => _isSceneInitialized;
        private bool _isSceneInitialized = false;

        private Object _initializationLock = new ();


        private void TakeInitilizationLockOrThrow()
        {
            int retryCount = 0;
            while (!Monitor.TryEnter(_initializationLock, TimeSpan.FromSeconds(1)))
            {
                Debug.LogError($"InitializationLock is not taken. Retry to retake. Retried[{retryCount}] BaseScene.Awake()");
                ++retryCount;

                if (retryCount >= 30)
                {
                    throw new Exception($"Scene Initilization failed. Retried Count [{retryCount}]");
                }
            }
        }

        private void ReleaseInitializationLock()
        {
            Monitor.Exit(_initializationLock);
        }
        
        protected virtual void Awake()
        {
            TakeInitilizationLockOrThrow();
            try
            {
                _isSceneCleared = false;
                _isSceneInitialized = false;
                //--------
                // 오프라인 데모 부트스트랩: 정적 데이터/리소스풀/전역 객체/게임 클라이언트를 한 번만 초기화한다.
                OfflineBootstrap.EnsureInitialized();

                //--------
                _timeScaleController = new TimeScaleController();

                BaseSceneUIRoot uiRoot = null;
                foreach (var sceneRoot in this.gameObject.scene.GetRootGameObjects())
                {
                    if (sceneRoot.name == "@UIRoot")
                    {
                        uiRoot = sceneRoot.GetComponent<BaseSceneUIRoot>();
                    }
                }

                Debug.Assert(uiRoot != null);
                this.UI = uiRoot;

                // 씬 초기화시 오디오리스너를 꺼두고 시작한다. SceneChanger에서 AudioListener켜준다.
                this.SwitchAudioListener(false);

                //화면 비를 체크해서 너무 극단적인 비율의 해상도는 개발자가 지정한 해상도로 강제 고정한다.
                GameClient.CameraController.SetMainCameraResolution(this.gameObject.scene);

                UnityGlobal.Instance.OnNewSceneAwaked(this, this.UI);
            }
            finally
            {
                ReleaseInitializationLock();
            }
        }
        
        /// <summary>
        /// 씬이 로드된 이후(Awake이후), 씬이 활성화(Activation)되기 전에 호출됩니다.
        /// 
        /// <see cref="SceneChanger"/>를 통해 초기화 되지않은 가장 첫번째 씬은 <paramref name="initialData"/>로 null이 전달됩니다.
        /// 정상적인 씬 초기화 상황에서는 Splash씬이 가장 첫번째 씬으로 <paramref name="initialData"/>이 null이 되고,
        /// 다른 씬들은 모두 <paramref name="initialData"/>가 null이 아닌 값으로 전달됩니다.
        /// 개발용 빌드에서 특정 씬을 지정하여 시작한 경우, Splash씬 외에 다른 씬도 null이 전달될 수 있습니다.
        /// </summary>
        public virtual void Initialize(ISceneInitialData initialData)
        {
            TakeInitilizationLockOrThrow();
            try
            {
                //화면 비를 체크해서 너무 극단적인 비율의 해상도는 개발자가 지정한 해상도로 강제 고정한다.
                GameClient.CameraController.SetMainCameraResolution(this.gameObject.scene);

                _timeScaleController.Initialize();
                _isSceneInitialized = true;
            }
            finally
            {
                ReleaseInitializationLock();
            }
        }

        /// <summary>
        /// 이 씬을 가리고 있는 로딩씬이 제거되고, 씬이 온전히 보여지게된 시점에 호출됩니다.
        /// 씬 전환(등장) 과정에서 반드시 한 번 호출됩니다.
        /// 
        /// 씬 전환 과정은 다음과 같이 진행됩니다 :
        /// <see cref="Initialize(ISceneInitialData)"/> -> Loading씬이 제거 완료된 후 -> <see cref="OnLoadingSceneRemoved"/>가 호출
        /// </summary>
        public virtual void OnLoadingSceneRemoved()
        {
            UI.OnLoadingSceneRemoved();
        }
        
        /// <summary>
        /// 다른 씬으로 변경되기 직전, 이 씬 오브젝트를 제거하기 직전에 호출됩니다.
        /// 해제해야할 리소스를 해제하고, 정리해주세요.
        /// </summary>
        public virtual void Clear()
        {
            this.UI.ClearBeforeChangingScene(this.gameObject.scene);
            Destroy(this.UI.gameObject);
            this.UI = null;

            _isSceneCleared = true;

            if (this.SceneType != SceneType.Loading)
            {
                GameClient.Instance.ClearBeforeChangingScene(this.gameObject.scene, this.SceneType);
            }
            UnityGlobal.Instance.ClearBeforeChangingScene(this.gameObject.scene, this.SceneType);
        }

        protected virtual void Update()
        {
            if (!_isSceneInitialized || _isSceneCleared)
            {
                return;
            }

            if (this.SceneType != SceneType.Loading)
            {
                UnityGlobal.Instance.Update();
                GameClient.Instance.Update(this.SceneType);
            }
        }

        public void SwitchAudioListener(bool enable)
        {
            var cameraControllerObject = CameraController.FindMainCameraControllerFromScene(this.gameObject.scene);
            var mainCameraAudioListener = cameraControllerObject?.GetComponentInChildren<AudioListener>();
            if (mainCameraAudioListener != null)
            {
                mainCameraAudioListener.enabled = enable;
            }
            else
            {
                Debug.LogWarning($"{this.gameObject?.scene.name ?? "??"} 씬에 @MainCamera에 오디오리스너가 없습니다. Enable[{enable}] 실패");
            }
        }
    }
}