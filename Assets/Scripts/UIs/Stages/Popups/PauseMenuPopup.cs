#nullable enable
using Shared.CSProtocols.ReqResData.Chapters;
using Shared.GameDataTypes;
using Shared.Localizers;
using Shared.StaticDatas;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SamMul.GameClients;
using SamMul.GameClients.Stages.Characters.PCs;
using SamMul.ResourcePools;
using SamMul.Scenes;
using SamMul.UnityHelpers;

namespace SamMul.UIs.Stages.Popups
{
    public class PauseMenuPopup : BasePopup
    {
        [SerializeField] private ZButton _homeButton = null!;
        [SerializeField] private ZButton _continueButton = null!;
        [SerializeField] private ZButton _soundButton = null!;
        [SerializeField] private Image _soundIconImage = null!;
        [SerializeField] private TextMeshProUGUI _playerCharacterLevelShadowText;
        [SerializeField] private TextMeshProUGUI _playerCharacterLevelText;

        [Header("일반 스테이지")]
        [SerializeField] private GameObject _forNormalStage;
        [SerializeField] private PauseMenuSkillIcon[] _activeSkillIcons = null!;
        [SerializeField] private PauseMenuSkillIcon[] _passiveSkillIcons = null!;


        private static readonly string SOUND_ON_ICON_PATH = "Stages/UIs/Popups/PauseMenuPopup/SoundOnIcon.png";
        private static readonly string SOUND_OFF_ICON_PATH = "Stages/UIs/Popups/PauseMenuPopup/SoundOffIcon.png";

        public void Initialize(Action<bool> closeRequester, bool isChapterZero)
        {
            base.InitializeBase(closeRequester);

            var stage = GameClient.Stage;
            if (stage != null)
            {
                this.InitializeAcquiredSkillSlots(stage.PC);
            }

            _homeButton.onClick.RemoveAllListeners();
            if (isChapterZero)
            {
                // 0챕터에서는 홈버튼이 없다. 로비로 못돌아가게 한다.
                _homeButton.gameObject.SetActive(false);
            }
            else
            {
                _homeButton.gameObject.SetActive(true);
                _homeButton.onClick.AddListener(() =>
                {
                    var sceneUI = UnityGlobal.Scenes.GetCurrentSceneUI();
                    if (sceneUI != null)
                    {
                        sceneUI.AddOKCancelPopup(
                            Localizer.Instance.GetText("UI_STAGE_PAUSE_POPUP_GO_HOME_TITLE"),
                            Localizer.Instance.GetText("UI_STAGE_PAUSE_POPUP_GO_HOME_MESSAGE"),
                            Localizer.Instance.GetText("UI_YES"), ChangeToLobbyScene,
                            Localizer.Instance.GetText("UI_NO"), null);
                    }
                });
            }

            _continueButton.onClick.RemoveAllListeners();
            _continueButton.onClick.AddListener(() =>
            {
                this.OnGameContinue();
            });

            float bgmVolume = UnityGlobal.Sounds.GetBGMVolume();
            if (bgmVolume <= 0f)
            {
                _soundIconImage.sprite = ResourcePool.Instance.LoadResource<Sprite>(SOUND_OFF_ICON_PATH);
            }
            else
            {
                _soundIconImage.sprite = ResourcePool.Instance.LoadResource<Sprite>(SOUND_ON_ICON_PATH);
            }

            _soundButton.onClick.RemoveAllListeners();
            _soundButton.onClick.AddListener(() =>
            {
                float bgmVolume = UnityGlobal.Sounds.GetBGMVolume();
                if (bgmVolume <= 0f)
                {
                    UnityGlobal.Sounds.SetBGMVolume(1f);
                    UnityGlobal.Sounds.SetEffectVolume(1f);
                    _soundIconImage.sprite = ResourcePool.Instance.LoadResource<Sprite>(SOUND_ON_ICON_PATH);
                }
                else
                {
                    UnityGlobal.Sounds.SetBGMVolume(0f);
                    UnityGlobal.Sounds.SetEffectVolume(0f);
                    _soundIconImage.sprite = ResourcePool.Instance.LoadResource<Sprite>(SOUND_OFF_ICON_PATH);
                }
            });

            _playerCharacterLevelShadowText.text = _playerCharacterLevelText.text = GameClient.Stage?.PC.Level.ToString();
        }

        private void InitializeAcquiredSkillSlots(PlayerCharacter playerCharacter)
        {
            _forNormalStage.SetActive(true);

            int nextActiveSkillIndex = 0;
            int nextPassiveSkillIndex = 0;

            var acquiredSkillKeys = playerCharacter.GetAcquiredSkillKeys();
            foreach (var acquiredSkillKey in acquiredSkillKeys)
            {
                var skillStaticData = StaticDataRepository.Instance.Skills.Get(acquiredSkillKey);
                if (skillStaticData.skillType == SkillType.Active)
                {
                    _activeSkillIcons[nextActiveSkillIndex].Initialize(skillStaticData);
                    ++nextActiveSkillIndex;
                }
                else if (skillStaticData.skillType == SkillType.Passive)
                {
                    _passiveSkillIcons[nextPassiveSkillIndex].Initialize(skillStaticData);
                    ++nextPassiveSkillIndex;
                }
                else
                {
                    throw new NotImplementedException($"[{skillStaticData.skillType}] 구현안됨");
                }
            }

            for (int i = nextActiveSkillIndex; i < _activeSkillIcons.Length; ++i)
            {
                _activeSkillIcons[i].gameObject.SetActive(false);
            }
            for (int i = nextPassiveSkillIndex; i < _passiveSkillIcons.Length; ++i)
            {
                _passiveSkillIcons[i].gameObject.SetActive(false);
            }
        }

        private void ChangeToLobbyScene()
        {
            this.Close(skipAnimation: false);

            var stage = GameClient.Stage;
            if (stage == null)
            {
                return;
            }

            if (GameClient.CS.UserGameData == null)
            {
                return;
            }

            if (stage.StageType == StageType.Chapter)
            {
                GameClient.CS.GiveUpChapter(new GiveUpChapterRequest(),
                    (GiveUpChapterResponse response) =>
                    {
                        if (response.ResultCode == GiveUpChapterResultCode.Success)
                        {
                            var lobbySceneInitialData = new LobbySceneInitialData();
                            UnityGlobal.Scenes.ChangeTo(SceneType.Lobby, lobbySceneInitialData, "챕터 포기");
                        }
                        else
                        {
                            UnityGlobal.HandleInvalidSessionInfo(nameof(GiveUpChapterRequest), GameClient.CS.ApplicationVersion, GameClient.CS.CachedAccountId);
                        }
                    }, UnityGlobal.HandleInternalServerError_LogAndRestart, UnityGlobal.HandleNetworkErrorAndContinueRetry);
            }
            else
            {
                throw new NotImplementedException($"{stage.StageType} 구현이 안 됨. 구현해주세요.");
            }

        }

        private void OnGameContinue()
        {
            this.Close(skipAnimation: false);
        }
    }

}