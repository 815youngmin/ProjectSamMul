#nullable enable
using Shared.Localizers;
using UnityEngine;
using SamMul.UIs.Lobbies.MainLobbyPages;
using SamMul.UIs.Lobbies.Popups;

namespace SamMul.Scenes
{
    public class LobbySceneUIRoot : BaseSceneUIRoot
    {
        [SerializeField] private MainLobbyPage _mainLobbyPage = null!;
        [SerializeField] private ZButton? _optionButton;

        public MainLobbyPage MainLobbyPage => _mainLobbyPage;

        public void Initialize(SceneType sceneType)
        {
            this.InitializeBase(sceneType);

            Debug.Assert(_mainLobbyPage);

            _mainLobbyPage.Initialize();
            _mainLobbyPage.gameObject.SetActive(true);

            // 씬에 옵션 버튼을 연결하기 전에도 로비가 동작하도록 null 을 허용한다.
            if (_optionButton != null)
            {
                _optionButton.onClick.RemoveAllListeners();
                _optionButton.onClick.AddListener(this.AddOptionPopup);
            }
        }

        public void AddOptionPopup()
        {
            var popup = this.CreateAndAddPopup<OptionPopup>("Lobbys/UIs/OptionPopup/OptionPopup.prefab", playSound: true);
            popup.Initialize(
                closeRequester: skipAnimation => this.CloseAndDestroyPopup(popup, onCompleted: () => { }, skipAnimation),
                onLanguageChanged: _mainLobbyPage.UpdateTexts);
        }

        public void Clear()
        {
        }

        public void UpdateLogic()
        {
#if UNITY_ANDROID
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // 백버튼 누른 경우임. 팝업 닫을 거 없으면 게임 종료
                if (!this.HasAnyPopup())
                {
                    this.AddOKCancelPopup(
                        Localizer.Instance.GetText("UI_QUIT_GAME_TITLE"), // Quit game
                        Localizer.Instance.GetText("UI_QUIT_GAME_MESSAGE"),// "Game is going to be terminated. Are you sure?",
                        Localizer.Instance.GetText("UI_OK"),
                        okButtonAction: () =>
                        {
                            Application.Quit();
                        },
                        Localizer.Instance.GetText("UI_CANCEL"),
                        cancelAction: () => { });
                }
            }
#endif // UNITY_ANDROID
        }
    }
}
