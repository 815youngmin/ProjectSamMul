#nullable enable
using Shared.CSProtocols.ReqResData.Chapters;
using Shared.Localizers;
using Shared.StaticDatas;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using SamMul.GameClients;
using SamMul.Scenes;
using SamMul.UnityHelpers;

namespace SamMul.UIs.Lobbies.BattlePages
{
    public class MainChapterStartButton : MonoBehaviour
    {
        [SerializeField] private ZButton _button;
        [SerializeField] private TextMeshProUGUI _startButtonText;

        private ChapterStaticData? _selectedChapter;

        public void Initialize(ChapterStaticData selectedChapter)
        {
            Assert.IsNotNull(_button);
            Assert.IsNotNull(_startButtonText);

            _selectedChapter = selectedChapter;

            _startButtonText.text = Localizer.Instance.GetText("UI_MAINLOBBY_BATTLE_START_BUTTON");

            _button.enabled = true;
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(this.OnBattleStartButtonClickEvent);
        }

        private void OnBattleStartButtonClickEvent()
        {
            // 중복 눌림 방지
            if (!_button.enabled)
            {
                return;
            }
            _button.enabled = false;

            UnityGlobal.Sounds.PlayBySoundPrefab("Sounds/SoundEffects/UIs/GameStart_SFX.prefab", Vector3.zero);

            this.EnterChapter(_selectedChapter!);
        }

        private void EnterChapter(ChapterStaticData selectedChapter)
        {
            _button.enabled = false;

            GameClient.CS.EnterChapter(new EnterChapterRequest(selectedChapter.ChapterNumber),
                onCompleted: (EnterChapterResponse response) =>
                {
                    _button.enabled = true;
                    Debug.Log($"{response.ResultCode}");

                    switch (response.ResultCode)
                    {
                        case EnterChapterResultCode.Success:
                            {
                                // 성공한 경우엔 씬전환으로 입장하기 전까지 더 안눌리도록 false로 셋팅해둔다.
                                _button.enabled = false;

                                var userSkillDeck = GameClient.CS.UserSkillDeck;
                                var userGameData = GameClient.CS.UserGameData;
                                var heroData = userGameData.GetSelectedHeroData();
                                var equipmentInventory = userGameData.EquipmentInventory();

                                var stageInitialData = StageSceneInitialData.CreateForMainChapter(
                                    selectedChapter,
                                    userSkillDeck,
                                    heroData,
                                    equipmentInventory.GetEquippedEquipments().Values);

                                UnityGlobal.Scenes.ChangeTo(SceneType.Stage, stageInitialData, "메인 챕터 시작");
                                break;
                            }
                        case EnterChapterResultCode.NotClearedPreviousChapter:
                            {
                                // TODO 아무튼 안내메시지 팝업 만들어서 보여주기
                                UnityGlobal.Scenes.GetCurrentSceneUI().AddCommonMessagePopup(
                                  Localizer.Instance.GetText("UI_POPUP_NEED_CLEAR_PREVIOUS_CHAPTER_TITLE"),
                                  Localizer.Instance.GetText("UI_POPUP_NEED_CLEAR_PREVIOUS_CHAPTER_MESSAGE"),
                                  Localizer.Instance.GetText("UI_OK"), () => { });
                                break;
                            }
                        case EnterChapterResultCode.InvalidChapter:
                            {
                                UnityGlobal.Scenes.GetCurrentSceneUI().AddCommonMessagePopup(
                                  Localizer.Instance.GetText("UI_POPUP_INVALID_CHAPTER_TITLE"),
                                  Localizer.Instance.GetText("UI_POPUP_INVALID_CHAPTER_MESSAGE"),
                                  Localizer.Instance.GetText("UI_OK"), () => { });
                                break;
                            }
                        case EnterChapterResultCode.AlreadyChapterPlaying:
                            {
                                // 이미 입장한 상태면 게임을 지속할 수 있도록 에러처리하지 않고 메시지 팝업을 띄우고 그냥 진행시킨다.
                                UnityGlobal.Scenes.GetCurrentSceneUI().AddCommonMessagePopup(
                                  Localizer.Instance.GetText("UI_POPUP_ALREADY_PLAYING_CHAPTER_TITLE"),
                                  Localizer.Instance.GetText("UI_POPUP_ALREADY_PLAYING_CHAPTER_MESSAGE"),
                                  Localizer.Instance.GetText("UI_OK"), () => { });
                                break;
                            }
                        case EnterChapterResultCode.InvalidSession:
                            {
                                UnityGlobal.HandleInvalidSessionInfo(nameof(EnterChapterRequest), GameClient.CS.ApplicationVersion, GameClient.CS.CachedAccountId);
                                break;
                            }
                        default:
                            {
                                throw new NotImplementedException($"{response.ResultCode} is not handled.");
                            }
                    }
                }, UnityGlobal.HandleInternalServerError_LogAndRestart, UnityGlobal.HandleNetworkErrorAndContinueRetry);
        }
    }
}
