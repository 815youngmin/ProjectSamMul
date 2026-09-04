#nullable enable
using Shared.Localizers;
using UnityEngine;
using SamMul.GameClients;
using SamMul.UIs.Lobbies;
using SamMul.UIs.Lobbies.BattlePages;
using SamMul.UIs.Lobbies.MainLobbyPages;

namespace SamMul.Scenes
{
    public class LobbySceneUIRoot : BaseSceneUIRoot
    {
        public enum Page { Invalid, MainLobbyPage, BattlePage }

        [SerializeField] private NavigationBarGroup _navigationBarGroup = null!;

        [SerializeField] private MainLobbyPage _mainLobbyPage = null!;
        [SerializeField] private BattlePage _battlePage = null!;

        public NavigationBarGroup NavigationBarGroup => _navigationBarGroup;
        public MainLobbyPage MainLobbyPage => _mainLobbyPage;
        public BattlePage BattlePage => _battlePage;


        private int _seletedChapterNumber;
        public int SeletedChapterNumber => _seletedChapterNumber;

        public bool IsCompleteInitialize => _isCompleteInitialize;
        private bool _isCompleteInitialize = false;

        public Page CurrentPage { get; private set; }

        public void Initialize(
            SceneType sceneType,
            int clearedHighestChapter, long highestStageTimeInSeconds)
        {
            this.InitializeBase(sceneType);

            Debug.Assert(_navigationBarGroup);
            Debug.Assert(_mainLobbyPage);
            Debug.Assert(_battlePage);

            _seletedChapterNumber = clearedHighestChapter + 1;
            //다음 챕터 정보가 없다. 마지막 챕터 정보를 선택해준다.
            if (GameClient.CS.ServiceFinalChapterNumber < _seletedChapterNumber)
            {
                _seletedChapterNumber = GameClient.CS.ServiceFinalChapterNumber;
            }

            _battlePage.Initialize(clearedHighestChapter, highestStageTimeInSeconds, CloseBattlePage, SetSelectedChapterNumer);

            _navigationBarGroup.Initialize(ChangeToMainLobbyPage);

            this.ChangeToMainLobbyPage();
            this.ShowNavigationBar(true);

            _isCompleteInitialize = true;
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

        private void CloseBattlePage()
        {
            this.ChangeToMainLobbyPage();
        }

        private void ChangeToMainLobbyPage()
        {
            if (CurrentPage == Page.MainLobbyPage)
            {
                return;
            }

            CurrentPage = Page.MainLobbyPage;

            var gameData = GameClient.CS.UserGameData;
            _mainLobbyPage.Initialize(SeletedChapterNumber, gameData.ClearedHighestChapter, gameData.HighestStageTimeInSeconds, ChangeToBattlePage, ChangeToBattlePageWithSelectedChapterTransition);

            _mainLobbyPage.gameObject.SetActive(true);
            _battlePage.gameObject.SetActive(false);

            this.ShowNavigationBar(true);

        }

        private void ChangeToBattlePage()
        {
            if (CurrentPage == Page.BattlePage)
            {
                return;
            }

            CurrentPage = Page.BattlePage;

            this.ShowNavigationBar(false);

            _battlePage.gameObject.SetActive(true);
            _battlePage.OnPageOpened(SeletedChapterNumber);
        }

        private void ChangeToBattlePageWithSelectedChapterTransition(int index)
        {
            CurrentPage = Page.BattlePage;

            this.ShowNavigationBar(false);

            _battlePage.gameObject.SetActive(true);
            _battlePage.OnPageOpened(SeletedChapterNumber);
            _battlePage.SetSelectedChapterAndTransition(index);
        }

        private void ShowNavigationBar(bool show)
        {
            _navigationBarGroup.gameObject.SetActive(show);
        }



        private void SetSelectedChapterNumer(int chapterNumber)
        {
            _seletedChapterNumber = chapterNumber;
        }
    }
}
