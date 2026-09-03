using System;
using Shared.GameDataTypes;
using Shared.Localizers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SamMul.GameClients;
using SamMul.Scenes;
using SamMul.UnityHelpers;

namespace SamMul.UIs.Lobbies
{
    public class BriefPopup : BasePopup
    {
        [SerializeField] private TextMeshProUGUI _itemName;
        [SerializeField] private TextMeshProUGUI _itemDescription;
        [SerializeField] private RectTransform _tailRectTransform;

        private Vector2 _saveTailLocalPosition;
        private bool isOpen;
        private RectTransform _rectTransform;
        public static readonly string PREFAB_PATH = "Lobbys/UIs/BriefPopup/BriefPopup.prefab";

        public static void AddBriefPopupToUIRoot(ItemType itemType, RectTransform targetRect)
        {
            // 아이템 이름/설명은 로컬라이즈 키로 직접 조회한다. (Localizer 는 키가 없으면 키 문자열을 그대로 돌려준다.)
            string itemName = Localizer.Instance.GetText($"ITEM_{itemType}_NAME");
            string itemDescription = Localizer.Instance.GetText($"ITEM_{itemType}_DESC");

            var sceneType = UnityGlobal.Scenes.GetCurrentScene<BaseScene>().SceneType;

            if (sceneType == SceneType.Stage)
            {
                var sceneUIRoot = UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>();
                sceneUIRoot.AddBriefPopup(targetRect, itemName, itemDescription);
            }
            else if (sceneType == SceneType.Lobby)
            {
                var sceneUIRoot = UnityGlobal.Scenes.GetCurrentSceneUI<LobbySceneUIRoot>();
                sceneUIRoot.AddBriefPopup(targetRect, itemName, itemDescription);
            }
        }
        
        public void Initialize(RectTransform targetRect, string itemName, string itemDescription, Action<bool> closePopupAction, bool isCloseOnOutsideClick)
        {
            base.InitializeBase(closePopupAction, isCloseOnOutsideClick);

            //아이템 이름 및 설명 문구 넣어주고 사이즈 업데이트
            _itemName.text = itemName;
            _itemDescription.text = itemDescription;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_itemName.rectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_itemDescription.rectTransform);

            //타겟의 화면 위치를 계산한다.
            {
                Vector3[] targetCorners = new Vector3[4];
                targetRect.GetWorldCorners(targetCorners);
                Vector2 targetBottomLeft = RectTransformUtility.WorldToScreenPoint(GameClient.CameraController.MainCamera, targetCorners[0]);
                Vector2 targetTopRight = RectTransformUtility.WorldToScreenPoint(GameClient.CameraController.MainCamera, targetCorners[2]);
                Vector3 targetCenter = (targetBottomLeft + targetTopRight) * 0.5f;

                //팝업 위치를 타겟의 위치로 적용해준다.
                //추가 계산을 위해 사이즈를 업데이트 해준다.
                _rectTransform = this.GetComponent<RectTransform>();

                RectTransformUtility.ScreenPointToLocalPointInRectangle(UnityGlobal.Scenes.GetCurrentSceneUI().RectTransform, targetCenter, GameClient.CameraController.MainCamera, out Vector2 canvasLocalPoint);
                _rectTransform.localScale = Vector3.one;
                _rectTransform.localPosition = canvasLocalPoint;
                LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);

                _saveTailLocalPosition = _tailRectTransform.localPosition;
            }

            //팝업이 화면의 왼쪽이나 오른쪽으로 나간 경우 중앙으로 위치를 이동 시켜준다.
            //위쪽이나 아래쪽으로 나간 경우는 생각하지 않는다.
            {
                //양쪽 화면 어두운 부분 범위 가져오는 코드
                float screenX = Screen.width * (1 - GameClient.CameraController.MainCamera.rect.width) * 0.5f;
                Vector3[] corners = new Vector3[4];
                _rectTransform.GetWorldCorners(corners);
                Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(GameClient.CameraController.MainCamera, corners[0]);
                Vector2 topRight = RectTransformUtility.WorldToScreenPoint(GameClient.CameraController.MainCamera, corners[2]);
                Vector2 center = (bottomLeft + topRight) * 0.5f;
                float targetWidthToScreen = topRight.x - bottomLeft.x;
                float targetHeightToScreen = topRight.y - bottomLeft.y;

                if (bottomLeft.x - screenX < 0)
                {
                    //왼쪽으로 나간 경우
                    float offsetX = bottomLeft.x - screenX;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(UnityGlobal.Scenes.GetCurrentSceneUI().RectTransform, center + new Vector2(-offsetX, targetHeightToScreen), GameClient.CameraController.MainCamera, out Vector2 newPosition);
                    float tailOffsetX = _rectTransform.localPosition.x - newPosition.x;
                    _rectTransform.localPosition = newPosition;
                    _tailRectTransform.localPosition = _saveTailLocalPosition + new Vector2(tailOffsetX, 0);

                }
                else if (topRight.x + screenX > Screen.width)
                {
                    //오른쪽으로 나간 경우
                    float offsetX = topRight.x + screenX - Screen.width;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(UnityGlobal.Scenes.GetCurrentSceneUI().RectTransform, center + new Vector2(-offsetX, targetHeightToScreen), GameClient.CameraController.MainCamera, out Vector2 newPosition);
                    float tailOffsetX = _rectTransform.localPosition.x - newPosition.x; 
                    _rectTransform.localPosition = newPosition;
                    _tailRectTransform.localPosition = _saveTailLocalPosition + new Vector2(tailOffsetX, 0);
                }
                else
                {
                    //나가지 않은경우
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(UnityGlobal.Scenes.GetCurrentSceneUI().RectTransform, center + new Vector2(0, targetHeightToScreen), GameClient.CameraController.MainCamera, out Vector2 newPosition);
                    _rectTransform.localPosition = newPosition;
                    _tailRectTransform.localPosition = _saveTailLocalPosition;
                }
            }
            isOpen = true;
        }

        public void UpdateLogic()
        {
            bool isClick = (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) || Input.GetMouseButtonDown(0);
            //열려있는 상태에서 화면 터치시 닫히는 처리 진행하는 코드
            if (isOpen && isClick)
            {
                this.Close(true);
                isOpen = false;
            }
        }

        public override void Close(bool skipAnimation)
        {
            //닫기 전 꼬리 위치 돌려놔야된다.
            //안하면 재사용할때 위치 이상하게 나옴
            _tailRectTransform.localPosition = _saveTailLocalPosition;
            base.Close(skipAnimation);
        }

    }
}
