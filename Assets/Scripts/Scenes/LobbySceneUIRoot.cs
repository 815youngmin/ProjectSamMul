#nullable enable
using Shared.GameLogics;
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
        [SerializeField] private LobbyWalletBarGroup _walletBarGroup = null!;
        [SerializeField] private LobbyUserInfoGroup _userInfoGroup = null!;

        [SerializeField] private MainLobbyPage _mainLobbyPage = null!;
        [SerializeField] private BattlePage _battlePage = null!;

        public NavigationBarGroup NavigationBarGroup => _navigationBarGroup;
        public MainLobbyPage MainLobbyPage => _mainLobbyPage;
        public BattlePage BattlePage => _battlePage;

        public LobbyWalletBarGroup WalletBarGroup => _walletBarGroup;
        public LobbyUserInfoGroup UserInfoGroup => _userInfoGroup;

        private int _seletedChapterNumber;
        public int SeletedChapterNumber => _seletedChapterNumber;

        public bool IsCompleteInitialize => _isCompleteInitialize;
        private bool _isCompleteInitialize = false;

        private BriefPopup? _briefPopup;

        public Page CurrentPage { get; private set; }

        public void Initialize(
            SceneType sceneType,
            int clearedHighestChapter, long highestStageTimeInSeconds,
            long goldAmount,
            int accountLevel, long accountExp,
            IHeroInventory heroInventory)
        {
            this.InitializeBase(sceneType);

            Debug.Assert(_navigationBarGroup);
            Debug.Assert(_walletBarGroup);
            Debug.Assert(_mainLobbyPage);
            Debug.Assert(_userInfoGroup);
            Debug.Assert(_battlePage);

            _briefPopup = null;

            _seletedChapterNumber = clearedHighestChapter + 1;
            //다음 챕터 정보가 없다. 마지막 챕터 정보를 선택해준다.
            if (GameClient.CS.ServiceFinalChapterNumber < _seletedChapterNumber)
            {
                _seletedChapterNumber = GameClient.CS.ServiceFinalChapterNumber;
            }

            _walletBarGroup.Initialize();
            _userInfoGroup.Initialize(heroInventory.MainHero.HeroType, accountLevel, accountExp);
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
            if (_briefPopup != null)
            {
                _briefPopup.UpdateLogic();
            }

            if (_walletBarGroup != null)
            {
                _walletBarGroup.UpdateLogic();
            }

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
            _userInfoGroup.UpdateProfileImage(gameData.GetSelectedHeroData().HeroType);

            _mainLobbyPage.gameObject.SetActive(true);
            _battlePage.gameObject.SetActive(false);

            this.ShowUserInfoGroup(true);
            this.ShowWalletBarGroup(true);
            this.ShowNavigationBar(true);

            _walletBarGroup.SetDefaultMode();
        }

        private void ChangeToBattlePage()
        {
            if (CurrentPage == Page.BattlePage)
            {
                return;
            }

            CurrentPage = Page.BattlePage;

            this.ShowUserInfoGroup(false);
            this.ShowWalletBarGroup(false);
            this.ShowNavigationBar(false);

            _battlePage.gameObject.SetActive(true);
            _battlePage.OnPageOpened(SeletedChapterNumber);
        }

        private void ChangeToBattlePageWithSelectedChapterTransition(int index)
        {
            CurrentPage = Page.BattlePage;

            this.ShowUserInfoGroup(false);
            this.ShowWalletBarGroup(false);
            this.ShowNavigationBar(false);

            _battlePage.gameObject.SetActive(true);
            _battlePage.OnPageOpened(SeletedChapterNumber);
            _battlePage.SetSelectedChapterAndTransition(index);
        }

        private void ShowNavigationBar(bool show)
        {
            _navigationBarGroup.gameObject.SetActive(show);
        }

        public void ShowUserInfoGroup(bool show)
        {
            _userInfoGroup.gameObject.SetActive(show);
        }

        public void ShowWalletBarGroup(bool show)
        {
            _walletBarGroup.gameObject.SetActive(show);
        }

        private void SetSelectedChapterNumer(int chapterNumber)
        {
            _seletedChapterNumber = chapterNumber;
        }

        /// <summary>
        /// 아이템의 간단할 정보를 표기하는 팝업 생성 함수
        /// 해당 함수는 기존 Popup과 달리 UIBlocker를 사용하지 않고 팝업 정렬에 포함되지 않는다.
        /// 내부 함수를 통해 target transform 하위로 들어가 위치를 계산한다.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="itemName"></param>
        /// <param name="itemDescription"></param>
        public void AddBriefPopup(RectTransform target, string itemName, string itemDescription)
        {
            if (_briefPopup != null)
            {
                return;
            }
            _briefPopup = this.CreateAndAddPopup<BriefPopup>(BriefPopup.PREFAB_PATH, false, false, false);
            _briefPopup.Initialize(target, itemName, itemDescription, CloseBriefPopup, isCloseOnOutsideClick: true);
        }

        /// <summary>
        /// 기존 팝업 코드와 달리 내부에서 직접 제거 작업을 진행한다.
        /// </summary>
        /// <param name="skipAnimation"></param>
        public void CloseBriefPopup(bool skipAnimation)
        {
            if (_briefPopup == null)
            {
                return;
            }

            this.CloseAndDestroyPopup(_briefPopup, () =>
            {
                _briefPopup = null;
            }, skipAnimation);
        }
    }
}
