using UnityEngine;
using UnityEngine.SceneManagement;
using SamMul.UnityHelpers;

namespace SamMul.GameClients.Cameras
{
    public readonly struct CameraMovableWindow
    {
        // 한화면에 들어오는 월드좌표상 크기
        // 카메라 움직임이 주어진 던전크기를 벗어나지 않게 제어하기 위함이다.
        // TODO : renderableWorldWidth/Height를 카메라 화각에 따라 실제 렌더링 되는 영역의 월드좌표계 크기만큼으로 변환하여 처리할 것.
        public readonly float RenderableWorldWidth;
        public readonly float RenderableWorldHeight;

        public readonly float MinX;
        public readonly float MaxX;
        public readonly float MinY;
        public readonly float MaxY;

        public CameraMovableWindow(
            float renderableWorldWidth,
            float renderableWorldHeight,
            float minX,
            float maxX,
            float minY,
            float maxY)
        {
            this.RenderableWorldWidth = renderableWorldWidth;
            this.RenderableWorldHeight = renderableWorldHeight;
            this.MinX = minX;
            this.MaxX = maxX;
            this.MinY = minY;
            this.MaxY = maxY;
        }

    }

    public class CameraController
    {
        public readonly CameraShakeController ShakeController;

        private GameObject _mainCameraController;
        private Camera _mainCamera;
        public Camera MainCamera => _mainCamera;
        private CameraMovableWindow cameraMovableWindow;

        private Vector2 _cameraResolution;
        public Vector2 CameraResolution => _cameraResolution;

        //가로 추가 이동 공간
        private float _walkableAreaBlankWidth;
        
        //세로 추가 이동 공간
        private float _walakbleAreaBlankHeight;

        //상단 UI로 인해 카메라에 가려지는 공간이 많아 보정해주기 위한 수치값
        //UI가 가리는 높이 만큼 값을 넣어주면 된다
        private float _stageTopUIHeight;

        private Vector3 positionOffset;
        private GameObject followingTarget;
        private float cameraMoveSpeed;

        private float _targetOrthographicSize;
        public float OrthographicSize => _targetOrthographicSize;
        
        public CameraController()
        {
            this._mainCameraController = null;
            this._mainCamera = null;

            this.ShakeController = new CameraShakeController();
            this._walkableAreaBlankWidth = 2f;
            this._walakbleAreaBlankHeight = 2f;
            this._stageTopUIHeight = 6f;

            this.followingTarget = null;
        }

        public void Update()
        {
            this.FollowTarget();
            this.AdjustOrthographicSizeGradually(Time.deltaTime);
        }

        /// <param name="stageWalkableArea">플레이어 캐릭터가 걸어다닐 수 있는 씬공간의 크기. 스테이지이ㅡ 크기. </param>
        /// <param name="target">카메라가 따라갈 씬의 게임오브젝트. 플레이어 캐릭터의 게임오브젝트.</param>
        public void SetupMainCamera(Scene targetScene, Rect stageWalkableArea, GameObject target)
        {
            this.LoadMainCamera(targetScene);
            this.SetCameraFollowingTarget(target, 20);
            this.ShakeController.SetTargetCamera(this._mainCamera);

            this._mainCamera.orthographicSize = _targetOrthographicSize = 11f;

            this.AdjustCameraMovableWindow(stageWalkableArea);
        }

        /// <summary>
        /// 카메라가 그려줄 영역을 변경합니다. Orthographic카메라의 OrthographicSize를 변화
        /// </summary>
        public void SetOrthographicSize(float orthographicSize)
        {
            if (_targetOrthographicSize == orthographicSize)
            {
                return;
            }

            _targetOrthographicSize = orthographicSize;
            _leftTimeToAdjustSize = CAMERA_ADJUST_DURATION;
        }

        // 1초동안 카메라 사이즈를 점진적으로 조정한다.
        private readonly float CAMERA_ADJUST_DURATION = 1f;
        private float _leftTimeToAdjustSize = 0f;
        private void AdjustOrthographicSizeGradually(float deltaTime)
        {
            if (_leftTimeToAdjustSize <= 0f)
            {
                return;
            }

            _leftTimeToAdjustSize -= deltaTime;
            float progress = Mathf.Min(1f, (CAMERA_ADJUST_DURATION - _leftTimeToAdjustSize) / CAMERA_ADJUST_DURATION);

            float currentSize = _mainCamera.orthographicSize;
            if (_targetOrthographicSize == currentSize)
            {
                return;
            }

            _mainCamera.orthographicSize = Mathf.Lerp(currentSize, _targetOrthographicSize, progress);
            var walkableArea = GameClient.Stage.StaticData.GetWalkableArea();

            //카메라 반경이 변경됨에 따라 카메라 이동 가능한 공간을 다시 계산해준다.
            this.AdjustCameraMovableWindow(walkableArea);
        }

        public static GameObject FindMainCameraControllerFromScene(Scene scene)
        {
            // 우리가 사용할 모든 씬들은 RootObject에 @MainCameraController 게임오브젝트를 포함하고,
            // @MainCameraController의 자식 게임오브젝트로 @MainCamera (카메라)를 포함해야 한다.
            foreach (var rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.name == "@MainCameraController")
                {
                    return rootObject;
                }
            }

            return null;
        }

        public void LoadMainCamera(Scene targetScene)
        {
            _mainCamera = null;

            _mainCameraController = FindMainCameraControllerFromScene(targetScene);
            _mainCamera = _mainCameraController?.GetComponentInChildren<Camera>();

            if (this._mainCameraController == null)
            {
                throw new LogicErrorException($"{targetScene.name} 씬에 @MainCameraController가 없습니다.");
            }
            
            if (this._mainCamera == null)
            {
                throw new LogicErrorException($"{targetScene.name} 씬의 @MainCameraController에 @MainCamera가 없습니다.");
            }
        }

        /// <summary>스테이지의 움직일수 있는 영역 안으로 카메라가 이동가능한 위치를 제한한다.</summary>
        /// <param name="stageWalkableArea">스테이지의 크기. 스테이지에서 플레이어가 걸어서 다닐 수 있는 공간.</param>
        public void AdjustCameraMovableWindow(Rect stageWalkableArea)
        {
            // 카메라가 움직일 수 있는 공간은, 걸을 수 있는 스테이지 크기보다 조금 더 넓다.
            Rect cameraMovableArea = new Rect(
                stageWalkableArea.x - _walkableAreaBlankWidth,
                stageWalkableArea.y - _walakbleAreaBlankHeight,
                stageWalkableArea.width + 2 * _walkableAreaBlankWidth,
                stageWalkableArea.height + 2 * _walakbleAreaBlankHeight + _stageTopUIHeight);

            float left = _mainCamera.rect.x * Screen.width;
            float bottom = _mainCamera.rect.y * Screen.height;

            float right = left + _mainCamera.rect.width * Screen.width;
            float top = bottom + _mainCamera.rect.height * Screen.height;

            var leftBottom = Camera.main.ScreenToWorldPoint(new Vector3(left, bottom, -Camera.main.transform.position.z));
            var rightTop = Camera.main.ScreenToWorldPoint(new Vector3(right, top, -Camera.main.transform.position.z));

            float renderableWorldWidth = rightTop.x - leftBottom.x;
            float renderableWorldHeight = rightTop.y - leftBottom.y;

            this.cameraMovableWindow = new CameraMovableWindow(
                renderableWorldWidth,
                renderableWorldHeight,
                minX: Mathf.Clamp(cameraMovableArea.xMin + (renderableWorldWidth * 0.5f),float.MinValue , 0f),
                maxX: Mathf.Clamp(cameraMovableArea.xMax - (renderableWorldWidth * 0.5f),0, float.MaxValue), 
                minY: Mathf.Clamp(cameraMovableArea.yMin + (renderableWorldHeight * 0.5f),float.MinValue, 0f),
                maxY: Mathf.Clamp(cameraMovableArea.yMax - (renderableWorldHeight * 0.5f),0, float.MaxValue));

            this.positionOffset.y = renderableWorldHeight * 0.05f;
        }

        /// <summary>
        /// 카메라 타겟 설정
        /// </summary>
        /// <param name="dungeon">플레이할 물리 공간. 메인씬에서 초기화되어 주입된다.</param>
        /// <param name="target">카메라의 타겟이 될 객체</param>
        /// <param name="moveSpeed">카메라 이동 속도(보간 강도)</param>
        public void SetCameraFollowingTarget(GameObject target, float moveSpeed)
        {
            Debug.Assert(target != null);
            this.followingTarget = target;
            this.cameraMoveSpeed = moveSpeed;
            this.positionOffset = new Vector3(0f, 0f, -25f);
        }

        private void FollowTarget()
        {
            if (this.followingTarget == null)
            {
                return;
            }

            var targetCameraPosition = followingTarget.transform.position + positionOffset;
            targetCameraPosition.x = Mathf.Clamp(targetCameraPosition.x, this.cameraMovableWindow.MinX, this.cameraMovableWindow.MaxX);
            targetCameraPosition.y = Mathf.Clamp(targetCameraPosition.y, this.cameraMovableWindow.MinY, this.cameraMovableWindow.MaxY);

            this._mainCameraController.transform.position = Vector3.Lerp(
                this._mainCameraController.transform.position,
                targetCameraPosition,
                this.cameraMoveSpeed * Time.fixedDeltaTime);
        }

        public enum ScreenSpaceMode
        {
            Normal,
            EmptySpaceOnLeftAndRight, // 좌우에 까만 여백
            EmptySpaceOnTopAndBottom, // 상하에 까만 여백
        }

        public void SetMainCameraResolution(Scene targetScene)
        {
            this.LoadMainCamera(targetScene);

            var mainDisplay = Display.main;
#if UNITY_STANDALONE
            float deviceWidth = Screen.width;
            float deviceHeight = Screen.height;
            var isFullScreenNow = Screen.fullScreen;
#else
            float deviceWidth = mainDisplay.systemWidth;
            float deviceHeight = mainDisplay.systemHeight;
            bool isFullScreenNow = true;
#endif // UNITY_STANDALONE

            float screenRatio = deviceWidth / deviceHeight;

            float targetWidth = deviceWidth;
            float targetHeight = deviceHeight;
            var screenSpaceMode = ScreenSpaceMode.Normal;

            float targetScreenRatio = 9f / 18f;
            if (screenRatio > targetScreenRatio)
            {
                // 가로 비율이 기준보다 넓어지는 경우
                // 좌우에 여백을 두고, 가로폭을 줄여 그림이 그려지는 영역의 비율을 9:19으로 맞춘다.
                targetWidth = (targetHeight * targetScreenRatio);
                screenSpaceMode = ScreenSpaceMode.EmptySpaceOnLeftAndRight;
            }
            else if (screenRatio < targetScreenRatio)
            {
                // 가로 비율이 기준보다 좁아지는 경우
                // 상하에 여백을 두고, 세로폭을 줄여 그림이 그려지는 영역의 비율을 9:19으로 맞춘다.
                targetHeight = (targetWidth * (1f / targetScreenRatio));
                screenSpaceMode = ScreenSpaceMode.EmptySpaceOnTopAndBottom;
            }

            Screen.SetResolution((int)targetWidth, (int)targetHeight, isFullScreenNow);

            _mainCamera.aspect = targetScreenRatio;
            switch (screenSpaceMode)
            {
                case ScreenSpaceMode.EmptySpaceOnLeftAndRight:
                    {
                        float widthRatio = targetWidth / deviceWidth;
                        _mainCamera.rect = new Rect((1f - widthRatio) * 0.5f, 0f, widthRatio, 1f);
                    }
                    break;
                case ScreenSpaceMode.EmptySpaceOnTopAndBottom:
                    {
                        float heightRatio = targetHeight / deviceHeight;
                        _mainCamera.rect = new Rect(0f, (1f - heightRatio) * 0.5f, 1f, heightRatio);
                    }
                    break;
                case ScreenSpaceMode.Normal:
                default:
                    {
                        _mainCamera.rect = new Rect(0f, 0f, 1f, 1f);
                    }
                    break;
            }
            
            _cameraResolution = new Vector2(targetWidth, targetHeight);
        }

        /// <summary>
        /// 카메라가 바라보는 월드 공간의 사각 영역을 얻습니다.
        /// </summary>
        /// <param name="leftPixelsToExclude">
        /// 사각 영역을 계산할 때 왼쪽에서 제외할 캔버스 기준 픽셀 수입니다. 
        /// UI 등의 요소로 월드가 가려질 때 사용합니다. 
        /// </param>
        /// <param name="rightPixelsToExclude">
        /// 사각 영역을 계산할 때 오른쪽에서 제외할 캔버스 기준 픽셀 수입니다. 
        /// UI 등의 요소로 월드가 가려질 때 사용합니다. 
        /// </param>
        /// <param name="bottomPixelsToExclude">
        /// 사각 영역을 계산할 때 아래쪽에서 제외할 캔버스 기준 픽셀 수입니다. 
        /// UI 등의 요소로 월드가 가려질 때 사용합니다. 
        /// </param>
        /// <param name="topPixelsToExclude">
        /// 사각 영역을 계산할 때 위쪽에서 제외할 캔버스 기준 픽셀 수입니다. 
        /// UI 등의 요소로 월드가 가려질 때 사용합니다. 
        /// </param>
        /// <returns>
        /// 월드 공간의 사각 영역입니다. 
        /// </returns>
        public Rect GetWorldRectInCamera(float leftPixelsToExclude, float rightPixelsToExclude, float bottomPixelsToExclude, float topPixelsToExclude)
        {
            // 월드 좌표계 기준 캔버스 크기의 절반
            float canvasHeightInWorld = _mainCamera.orthographicSize;
            float canvasWidthInWorld = _mainCamera.orthographicSize * _cameraResolution.x / _cameraResolution.y;

            RectTransform canvasRect = UnityGlobal.Scenes.GetCurrentSceneUI().GetComponent<RectTransform>();
            // 월드 좌표계 기준 캔버스 1픽셀 크기
            float pixelSizeInWorld = canvasHeightInWorld / canvasRect.sizeDelta.y;

            float left = _mainCamera.transform.position.x - canvasWidthInWorld + (pixelSizeInWorld * leftPixelsToExclude);
            float right = _mainCamera.transform.position.x + canvasWidthInWorld - (pixelSizeInWorld * rightPixelsToExclude);
            float bottom = _mainCamera.transform.position.y - canvasHeightInWorld + (pixelSizeInWorld * bottomPixelsToExclude);
            float top = _mainCamera.transform.position.y + canvasHeightInWorld - (pixelSizeInWorld * topPixelsToExclude);

            float width = right - left;
            float height = top - bottom;

            return new Rect(left, bottom, width, height);
        }
    }
}
