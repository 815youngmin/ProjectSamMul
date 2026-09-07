#nullable enable
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SamMul.GameClients;
using SamMul.Loggers;
using SamMul.ResourcePools;
using SamMul.UIs;
using SamMul.UIs.Commons.Popups;
using SamMul.UnityHelpers;

namespace SamMul.Scenes
{
    /// <summary>
    /// 씬 UI 루트의 공통 기반. 캔버스 설정, 팝업 스택(CreateAndAddPopup / CloseAndDestroyPopup), UI 블로커, 팝업 중 일시정지(PauseResumeOnPopup)를 제공한다.
    /// </summary>
    public abstract class BaseSceneUIRoot : MonoBehaviour
    {
        public RectTransform RectTransform { get; private set; } = null!;

        [SerializeField] private Canvas _canvas = null!;
        [SerializeField] private CanvasScaler _canvasScaler = null!;
        private static EventSystem _eventSystem = null!;

        private UIBlocker? _uiBlocker;

        private List<(BasePopup Popup, bool UseUIBlocker)> _popups = null!;
        public bool HasAnyPopup() => _popups?.Any() ?? false;

        public static readonly string UI_BLOCKER_RESOURCE_PATH = "Stage/Common/UIBlocker.prefab";


        /// <remarks>UIRoot의 Awake에는 시스템 코드만 넣어주세요. 일반 MonoBehaviour와 달리 초기화 순서가 꼬이기 때문</remarks>
        protected virtual void Awake()
        {
            // UIRoot의 Awake에는 시스템 코드만 넣어주세요. 일반 MonoBehaviour와 달리 초기화 순서가 꼬이기 때문

            // 이벤트시스템을 끈 상태로 초기화한다. 이후 SceneChanger에서 켜준다.
            // -> iOS에서 Awake가 Initialize보다 뒤에 불릴때가 있다. 그래서 씬 전환 후 이벤트 수신이 안 되는 경우가 생긴.
            // Awake()에 코드를 더 추가하지 말자.
            CheckAndLoadEventSystem();
            SetUpCanvasScaler();
        }

        public void InitializeBase(SceneType sceneType)
        {
            Debug.Assert(_canvas != null, "SceneUI 게임오브젝트 프리팹에서 가장바닥이될 Canvas와 CanvasScaler를 컴퍼넌트로 붙여두어야 합니다.");

            CheckAndLoadEventSystem();

            RectTransform = this.GetComponent<RectTransform>();

            _canvas!.overrideSorting = true;

            _popups = new List<(BasePopup, bool)>();

            _uiBlocker = null;

            SetUpCanvasScaler(sceneType);
            
        }

        private void CheckAndLoadEventSystem()
        {
            if (_eventSystem == null)
            {
                // EventSystem 은 유니티 앱도메인에 단 하나만 존재해야 한다.
                var eventSystemObject = GameObject.Find("EventSystem");
                if (eventSystemObject == null)
                {
                    // NOTE : 여기는 ResourcePool이 초기화되기 전, Scene Awake코드에서도 호출되어야 하기 때문에,
                    // ResourcePool을 통하지 않고 직접 Resource.Load한다.
                    var eventSystemObjectResource = Resources.Load<GameObject>("Commons/@EventSystem");
                    eventSystemObject = GameObject.Instantiate(eventSystemObjectResource);
                    eventSystemObject.name = "EventSystem";
                }
                _eventSystem = eventSystemObject.GetComponent<EventSystem>();
                Debug.Assert(_eventSystem != null);
                GameObject.DontDestroyOnLoad(eventSystemObject);
            }
        }

        /// <summary>
        /// 이 씬을 가리고 있는 로딩씬이 제거되고, 씬이 온전히 보여지게된 시점에 호출됩니다.
        /// 씬 전환(등장) 과정에서 반드시 한 번 호출됩니다.
        /// 
        /// 씬 전환 과정은 다음과 같이 진행됩니다 :
        /// <see cref="BaseScene.Initialize(ISceneInitialData)"/> -> Loading씬이 제거 완료된 후 -> <see cref="OnLoadingSceneRemoved"/>가 호출
        /// </summary>
        public virtual void OnLoadingSceneRemoved()
        {
        }

        public virtual void ClearBeforeChangingScene(Scene clearingScene)
        {
            if (_popups.Count > 0)
            {
                Log.I.Warn($"SceneUIRoot가 정리되는데 팝업이 정상적으로 정리되지 않은 것 같습니다. _popups.Count:[{_popups.Count}]");
                _popups.Clear();
            }

        }

        public virtual void Update()
        {
        }



        // 카메라 화면에 맞춰 Canvas 조절할 수 있도록 설정한다.
        public void SetUpCanvasScaler(SceneType sceneType)
        {
            this.SetUpCanvasScaler();
        }

        public void SetUpCanvasScaler()
        {
            var canvas = _canvas;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = GameClient.Instance != null ?
                GameClient.CameraController?.MainCamera :
                null;
            canvas.sortingLayerID = SortingLayer.NameToID("UI");

            var canvasScaler = _canvasScaler;
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            canvasScaler.matchWidthOrHeight = 0;
        }

        public T CreateAndAddPopup<T>(string fullPath, bool playSound) where T : BasePopup
            => CreateAndAddPopup<T>(fullPath, skipPopUpAnimation: false, playSound, true);

        public T CreateAndAddPopup<T>(string fullPath, bool skipPopUpAnimation, bool playSound) where T : BasePopup
            => CreateAndAddPopup<T>(fullPath, skipPopUpAnimation, playSound, true);


        public T CreateAndAddPopup<T>(string fullPath, bool skipPopUpAnimation, bool playSound, bool useUIBlocker) where T : BasePopup
        {
            if (_uiBlocker == null && useUIBlocker)
            {
                var blocker = ResourcePool.Instance.InstantiateFromResource<UIBlocker>(UI_BLOCKER_RESOURCE_PATH);
                blocker.gameObject.transform.SetParent(this.transform);
                blocker.gameObject.transform.localPosition = Vector3.zero;
                _uiBlocker = blocker;
            }

            GameObject go = ResourcePool.Instance.InstantiateFromResource(fullPath);
            T popup = go.GetComponent<T>();
            Debug.Assert(popup != null);
            _popups.Add((popup!, useUIBlocker));

            go.transform.SetParent(this.gameObject.transform);

            var parentRectTransform = this.transform.GetComponent<RectTransform>();
            var rectTransform = go.GetComponent<RectTransform>();
            rectTransform.sizeDelta = parentRectTransform.sizeDelta;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;
            go.SetActive(true);

            if (_uiBlocker != null && useUIBlocker)
            {
                _uiBlocker.SetTargetPopup(popup);
                _uiBlocker.transform.SetAsLastSibling();
            }

            go.transform.SetAsLastSibling();

            if (!skipPopUpAnimation)
            {
                var originalScale = go.transform.localScale;
                go.transform.localScale = new Vector2(0.3f, 0.3f);

                DOTween.Sequence(go)
                    .Append(go.transform.DOScale(originalScale + new Vector3(0.05f, 0.05f, 0.05f), 0.065f).SetEase(Ease.InCubic))
                    .Append(go.transform.DOScale(originalScale, 0.05f).SetEase(Ease.InCubic))
                    .SetUpdate(isIndependentUpdate: true)
                    .OnComplete(() => { popup!.OnPopupOpenAnimationFinished(); });
            }
            else
            {
                DOTween.Sequence(go)
                    .AppendInterval(0f)
                    .SetUpdate(isIndependentUpdate: true)
                    .OnComplete(() => { popup!.OnPopupOpenAnimationFinished(); });
            }

            if (playSound)
            {
                UnityGlobal.Sounds.PlayBySoundPrefab("Sounds/SoundEffects/UIs/PopupOpen_SFX.prefab", Vector3.zero);
            }

            return popup!;
        }

        public void CloseAndDestroyPopup(BasePopup popup, UnityAction onCompleted, bool skipAnimation)
        {
            if (skipAnimation)
            {
                DestroyPopup();
            }
            else
            {
                DOTween.Sequence(popup)
                    .Append(popup.transform.DOScale(new Vector3(0f, 0f, 0f), 0.075f).SetEase(Ease.InCubic))
                    .OnComplete(DestroyPopup)
                    .SetUpdate(isIndependentUpdate: true);
            }

            void DestroyPopup()
            {
                // 닫을 팝업의 인덱스, 없으면 -1.
                var targetIndex = _popups.FindIndex(x => x.Popup == popup);
                if (targetIndex < 0)
                {
                    Debug.Log($"팝업 제거 실패. {popup.name}");
                    return;
                }
                _popups.RemoveAt(targetIndex);
                GameObject.Destroy(popup.gameObject);

                if (_uiBlocker != null)
                {
                    // UI Blocker를 사용하는 팝업의 인덱스, 없으면 -1.
                    var lastIndexUsingUIBlocker = _popups.FindLastIndex(x => x.UseUIBlocker);

                    // 남아있는 팝업이 없어 UI블록을 제거해줘야 할때.
                    if (lastIndexUsingUIBlocker < 0)
                    {
                        _uiBlocker.transform.SetParent(null);
                        ResourcePool.Instance.PutBackInstance(UI_BLOCKER_RESOURCE_PATH, _uiBlocker.gameObject);
                        _uiBlocker = null;
                    }
                    // 팝업이 남아있어 UI블록과 팝업 정렬을 다시 해줘야할때.
                    else
                    {
                        for (int i = 0; i < _popups.Count; ++i)
                        {
                            if (i == lastIndexUsingUIBlocker)
                            {
                                _uiBlocker.transform.SetAsLastSibling();
                                _uiBlocker.SetTargetPopup(_popups[i].Popup);
                            }
                            _popups[i].Popup.transform.SetAsLastSibling();
                        }
                    }
                }

                onCompleted();
            }
        }

        public void AddCommonMessagePopup(string title, string message, string okButtonText, Action okButtonAction)
        {
            var popup = this.CreateAndAddPopup<CommonPopup>("Stage/UIs/CommonPopup.prefab", playSound: true);
            popup.InitializeCommonPopup(title, message, okButtonText, okButtonAction, closeRequester: (bool skipAnimation) =>
            {
                this.CloseAndDestroyPopup(popup, onCompleted: () => { }, skipAnimation);
            });
        }
        
        public void AddErrorMessagePopup(
            string title,
            string message,
            string okButtonText,
            string protocolName,
            string appVersion,
            long accountId,
            Action okButtonAction)
        {
            // 전용 오류 팝업 대신 공용 팝업을 쓰고, 문의용 정보(요청 이름/버전/계정)는 메시지 아래에 덧붙인다.
            string detail = $"{protocolName} / v{appVersion} / #{accountId}";
            this.AddCommonMessagePopup(title, message + "\n\n" + detail, okButtonText, okButtonAction);
        }

        public void AddOKCancelPopup(string title, string message, string okButtonText, Action? okButtonAction, string cancelText, Action? cancelAction)
        {
            var popup = this.CreateAndAddPopup<CommonPopup>("Stage/UIs/CommonPopup.prefab", playSound: true);
            popup.Initialize(title, message, okButtonText, okButtonAction, cancelText, cancelAction, closeRequester: (bool skipAnimation) =>
            {
                this.CloseAndDestroyPopup(popup, onCompleted: () => { }, skipAnimation);
            });
        }


        public void PauseResumeOnPopup()
        {
            if (_popups.Count > 0)
            {
                UnityGlobal.Scenes.GetCurrentScene<BaseScene>().TimeScaleController.PauseTimeScale();
            }
            else
            {
                UnityGlobal.Scenes.GetCurrentScene<BaseScene>().TimeScaleController.ResumeTimeScale();
            }
        }
    }
}