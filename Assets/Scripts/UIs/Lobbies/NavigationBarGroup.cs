using Shared.Localizers;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SamMul.UIs.Lobbies
{
    //네비게이션바의 버튼 연출은 NavigationButton에서 처리한다.(사이즈 조절, 이미지 교채 등)
    //네비게이션바 버튼들의 위치는 UI의 NavigationBarGroup/Buttons의 HorizontalLayoutGroup으로 관리한다.

    public class NavigationBarGroup : MonoBehaviour
    {
        private enum NavigationType
        {
            Lobby
        }

        [SerializeField] private HorizontalLayoutGroup _layoutGroup;

        [SerializeField] private NavigationButton _goToMainLobbyButton;
        [SerializeField] private NavigationButton _goToShopButton;
        [SerializeField] private NavigationButton _goToAvataButton;
        [SerializeField] private NavigationButton _goToChallengeButton;

        [SerializeField] private TextMeshProUGUI _goToMainLobbyButtonText;

        public NavigationButton MainLobbyButton => _goToMainLobbyButton;

        private NavigationType _currentNavigation;

        public void Initialize(Action changeToMainLobbyPage)
        {
            Debug.Assert(_goToMainLobbyButton != null);

            // 데모에서는 상점/도전 페이지가 없고, 아바타 페이지는 추후 다시 만든다. 해당 버튼들은 숨겨둔다.
            _goToShopButton.gameObject.SetActive(false);
            _goToAvataButton.gameObject.SetActive(false);
            _goToChallengeButton.gameObject.SetActive(false);

            _goToMainLobbyButton.Initialize(() =>
            {
                changeToMainLobbyPage();
                _goToMainLobbyButton.DisableButton();
                this.UnSelectButton(_currentNavigation);
                _currentNavigation = NavigationType.Lobby;
                this.SelectButton(_currentNavigation);
            });

            _currentNavigation = NavigationType.Lobby;
            _goToMainLobbyButton.SelectButton();
            // 초기상태는 메인로비 페이지를 띄운 상태로 만들어둔다.
            _goToMainLobbyButton.DisableButton();

            _goToMainLobbyButtonText.text = Localizer.Instance.GetText("UI_CONTENT_CONQUEST");
        }

        private void SelectButton(NavigationType navigationType)
        {
            switch (navigationType)
            {
                case NavigationType.Lobby:
                    _goToMainLobbyButton.SelectButton();
                    break;
                default:
                    break;
            }
        }

        private void UnSelectButton(NavigationType navigationType)
        {
            switch (navigationType)
            {
                case NavigationType.Lobby:
                    _goToMainLobbyButton.UnSelectButton();
                    break;
                default:
                    break;
            }

        }

        private void Update()
        {
            // 내비게이션 애니메이션에 따라 크기가 조정될 때에도 자연스럽게 레이아웃 반영되도록 매프레임 호출해준다.
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_layoutGroup.transform);
        }
    }
}
