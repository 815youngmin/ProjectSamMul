using DG.Tweening;
using Z.Animations.Placeholder;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Z.UIs.Lobbies
{
    //네비게이션바 버튼 연출을 위한 스크립트 코드
    //활성화 비활성화 아이콘 불러오기, 선택 연출, 취소 연출을 처리한다.
    public class NavigationButton : MonoBehaviour
    {
        [SerializeField] private ZButton _button;
        [SerializeField] private SkeletonGraphic _iconAnimation;
        // 실제로 보여지는건 _iconAnimation인데, 그 아래에 같은크기로 깔아둔다. (안보이도록)
        // ContentLocker에서, 이 이미지 위치로 아이콘을 그려주기 위해 사용된다.
        [SerializeField] private Image _buttonIconTarget;

        private Sequence _buttonSelectAnimation;
        private Sequence _buttonUnselectAnimation;

        public ZButton Button => _button;
        public Image ButtonIconTarget => _buttonIconTarget;
        public SkeletonGraphic IconAnimation => _iconAnimation;

        public void Initialize(UnityAction onClickAction)
        {
            _buttonIconTarget.enabled = false;

            _button.SetInteractable(true);
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(onClickAction);

            var buttonOriginalPos = (Vector2)_iconAnimation.transform.localPosition;
            var deltaSelectedMove = new Vector2(-5f, 14f);
            {
                Vector3 selectedScale = 1.1f * Vector3.one;
                Vector3 unselectedScale = 0.925f * Vector3.one;

                RectTransform buttonRectTransform = this.gameObject.GetComponent<RectTransform>();
                buttonRectTransform.localScale = unselectedScale;

                _buttonSelectAnimation = DOTween.Sequence(this)
                    .Append(buttonRectTransform.DOScale(selectedScale, 0.2f))
                    .Join(_iconAnimation.transform.DOLocalMove(buttonOriginalPos + deltaSelectedMove, 0.2f))
                    .OnComplete(() => _button.SetButtonScale(selectedScale))
                    .SetRecyclable(true).SetAutoKill(false).Pause();

                _buttonUnselectAnimation = DOTween.Sequence(this)
                    .Append(buttonRectTransform.DOScale(unselectedScale, 0.2f))
                    .Join(_iconAnimation.transform.DOLocalMove(buttonOriginalPos, 0.2f))
                    .OnComplete(() => _button.SetButtonScale(unselectedScale))
                    .SetRecyclable(true).SetAutoKill(false).Pause();
            }
        }

        public void SelectButton()
        {
            _buttonSelectAnimation.Restart();
            _iconAnimation.AnimationState.SetAnimation(0, "move", loop: true);
        }

        public void UnSelectButton()
        {
            _buttonUnselectAnimation.Restart();
            _iconAnimation.AnimationState.SetAnimation(0, "idle", loop: false);
        }

        public void EnableButton()
        {
            _button.enabled = true;
        }

        public void DisableButton()
        {
            _button.enabled = false;
        }
    }
}
