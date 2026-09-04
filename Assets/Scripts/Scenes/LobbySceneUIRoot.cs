#nullable enable
using Shared.Localizers;
using UnityEngine;
using SamMul.UIs.Lobbies.MainLobbyPages;

namespace SamMul.Scenes
{
    public class LobbySceneUIRoot : BaseSceneUIRoot
    {
        [SerializeField] private MainLobbyPage _mainLobbyPage = null!;

        public MainLobbyPage MainLobbyPage => _mainLobbyPage;

        public void Initialize(SceneType sceneType, int clearedHighestChapter)
        {
            this.InitializeBase(sceneType);

            Debug.Assert(_mainLobbyPage);

            _mainLobbyPage.Initialize(clearedHighestChapter);
            _mainLobbyPage.gameObject.SetActive(true);
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
