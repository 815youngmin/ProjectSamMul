using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Z.GameClients;
using Z.GameClients.Stages;
using Z.UnityHelpers;

namespace Z.UIs.Stages.HUDs
{
    /// <summary>
    /// 목표 위치가 카메라를 벗어나면 화면 가장자리에 방향을 알려주는 내비게이션 화살표입니다.
    /// </summary>
    public class NavigationArrow : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _iconText;
        [SerializeField] private Image _arrowImage;

        public bool Show;

        private Action _removeNavigationArrow;
        private Stage _stage;
        private Transform _targetTransform;
        private bool _showAlways;       // 화면 안에 있을 때에도 해당 위치에 보여준다.

        public void Initialize(Action removeNavigationArrow, Stage stage, Transform targetTransform, bool showAlways, string iconText)
        {
            Show = true;

            _removeNavigationArrow = removeNavigationArrow;
            _stage = stage;
            _targetTransform = targetTransform;
            _showAlways = showAlways;
            _iconText.text = iconText;

            var rectTransform = (RectTransform)transform;
            rectTransform.localPosition = Vector3.zero;
            rectTransform.localScale = Vector3.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        public void RemoveNavigationArrow()
        {
            _removeNavigationArrow.Invoke();
        }

        public virtual void UpdateLogic()
        {
            this.UpdateNavigationArrow(_stage);
        }

        private void UpdateNavigationArrow(Stage stage)
        {
            if (Show && (_showAlways || !this.IsTargetPositionInCamera(_targetTransform.position)))
            {
                if (!gameObject.activeSelf)
                {
                    gameObject.SetActive(true);
                }
                this.UpdatePositionAndRotation(stage);
            }
            else
            {
                if (gameObject.activeSelf)
                {
                    gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 목표 위치가 카메라 안에 있는지를 확인합니다. 
        /// </summary>
        /// <param name="targetPosition">
        /// 카메라 안에 있는지 확인할 목표 위치입니다. 
        /// </param>
        /// <returns>
        /// 목표 위치가 카메라 안에 있으면 true, 카메라 밖에 있으면 false입니다.
        /// </returns>
        private bool IsTargetPositionInCamera(Vector2 targetPosition)
        {
            var worldRect = GameClient.CameraController.GetWorldRectInCamera(0.0f, 0.0f, 0.0f, 400.0f);
            return worldRect.Contains(targetPosition);
        }

        /// <summary>
        /// 내비게이션 화살표의 위치와 방향을 업데이트합니다. 
        /// </summary>
        private void UpdatePositionAndRotation(Stage stage)
        {
            RectTransform canvasRect = UnityGlobal.Scenes.GetCurrentSceneUI().RectTransform;

            Vector2 targetDirection = ((Vector2)_targetTransform.position - stage.PC.Pos).normalized;
            Vector2 navigationArrowWorldPosition = CalculateWorldPosition((Vector2)_targetTransform.position, targetDirection);

            Vector2 viewportPosition = GameClient.CameraController.MainCamera.WorldToViewportPoint(navigationArrowWorldPosition);
            Vector2 screenPosition = new Vector2(
            (viewportPosition.x * canvasRect.sizeDelta.x) - (canvasRect.sizeDelta.x * 0.5f),
            (viewportPosition.y * canvasRect.sizeDelta.y) - (canvasRect.sizeDelta.y * 0.5f));
            _arrowImage.rectTransform.anchoredPosition = screenPosition - targetDirection * 40;
            _iconImage.rectTransform.anchoredPosition = screenPosition - targetDirection * 115;

            float angle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
            _arrowImage.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        /// <summary>
        /// 내비게이션 화살표의 월드에서의 위치를 계산합니다. 
        /// </summary>
        /// <param name="targetPosition">
        /// </param>
        /// <param name="targetDirection">
        /// 목표가 있는 방향의 단위 벡터입니다. 
        /// </param>
        /// <returns>
        /// 내비게이션 화살표의 월드에서의 위치를 반환합니다. 
        /// </returns>
        private static Vector2 CalculateWorldPosition(Vector2 targetPosition, Vector2 targetDirection)
        {
            var worldRect = GameClient.CameraController.GetWorldRectInCamera(20.0f, 20.0f, 50.0f, 900.0f);
            Vector2 navigationArrowWorldPosition = targetPosition;

            if (navigationArrowWorldPosition.x < worldRect.xMin)
            {
                float overDistance = (worldRect.xMin - navigationArrowWorldPosition.x) / targetDirection.x;
                navigationArrowWorldPosition += (targetDirection * overDistance);
            }
            else if (navigationArrowWorldPosition.x > worldRect.xMax)
            {
                float overDistance = (worldRect.xMax - navigationArrowWorldPosition.x) / targetDirection.x;
                navigationArrowWorldPosition += (targetDirection * overDistance);
            }

            if (navigationArrowWorldPosition.y < worldRect.yMin)
            {
                float overDistance = (worldRect.yMin - navigationArrowWorldPosition.y) / targetDirection.y;
                navigationArrowWorldPosition += (targetDirection * overDistance);
            }
            else if (navigationArrowWorldPosition.y > worldRect.yMax)
            {
                float overDistance = (worldRect.yMax - navigationArrowWorldPosition.y) / targetDirection.y;
                navigationArrowWorldPosition += (targetDirection * overDistance);
            }

            return navigationArrowWorldPosition;
        }
    }
}
