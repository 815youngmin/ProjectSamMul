#nullable enable
using DG.Tweening;
using UnityEngine;


namespace Z.UIs.Lobbies.MainLobbyPages
{
    public class MenuButton : MonoBehaviour
    {
        public bool IsMenuGroupOpen => _menuGroup.gameObject.activeSelf;

        [SerializeField] private ZButton _menuButton;
        [SerializeField] private RectTransform _menuGroup;

        // 데모에서 제거된 옵션/우편함/출석부 버튼. 프리팹에 남아 있으므로 숨겨둔다.
        [SerializeField] private ZButton _optionButton;
        [SerializeField] private ZButton _mailBoxButton;
        [SerializeField] private ZButton _attendanceButton;

        public void Initialize()
        {
            _menuButton.onClick.RemoveAllListeners();
            _menuButton.onClick.AddListener(() =>
            {
                DOTween.Kill(_menuGroup);
                if (IsMenuGroupOpen)
                {
                    DOTween.Sequence(_menuGroup)
                        .Append(_menuGroup.DOScale(1.05f, 0.05f).SetEase(Ease.InCubic).From(1.0f))
                        .Append(_menuGroup.DOScale(0.0f, 0.1f).SetEase(Ease.InCubic))
                        .OnComplete(() => _menuGroup.gameObject.SetActive(false));
                }
                else
                {
                    DOTween.Sequence(_menuGroup)
                        .OnStart(() => _menuGroup.gameObject.SetActive(true))
                        .Append(_menuGroup.DOScale(1.05f, 0.1f).SetEase(Ease.InCubic).From(0.0f))
                        .Append(_menuGroup.DOScale(1.0f, 0.05f).SetEase(Ease.InCubic));
                }
            });
            _menuGroup.gameObject.SetActive(false);

            _optionButton.gameObject.SetActive(false);
            _mailBoxButton.gameObject.SetActive(false);
            _attendanceButton.gameObject.SetActive(false);
        }
    }
}
