#nullable enable
using DG.Tweening;
using Shared.CSProtocols.ReqResData.Chapters;
using Shared.GameDataTypes;
using Shared.Localizers;
using Shared.StaticDatas;
using Shared.UserDatas;
using SamMul.Animations.Placeholder;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SamMul.GameClients;
using SamMul.ResourcePools;
using SamMul.Scenes;
using SamMul.UIs.Commons.Rewards;
using SamMul.UnityHelpers;

namespace SamMul.UIs.Stages.Popups
{
    public class StageResultPopup : BasePopup
    {
        //최상단 Victory 타이틀 애니메이션
        [Header("SuccessTitle")]
        [SerializeField] private SkeletonGraphic _successTitle;

        [Header("FailTitle")]
        [SerializeField] private TextMeshProUGUI _failTitle;

        [Header("ChapterName")]
        [SerializeField] private Image _chapterNameLabel;
        [SerializeField] private TextMeshProUGUI _chapterNameText;

        [Header("SurvivalTime")]
        [SerializeField] private TextMeshProUGUI _newRecordLabelText;
        [SerializeField] private TextMeshProUGUI _survivalLabelText;
        [SerializeField] private TextMeshProUGUI _survivalTimeText;

        //킬 카운트 UI
        [Header("KillCount")]
        [SerializeField] private Image _killIconLabel;
        [SerializeField] private Image _killIconImage;
        [SerializeField] private TextMeshProUGUI _killCountText;

        //정복포인트 UI
        [Header("ConquerorPoint")]
        [SerializeField] private Image _conquerorLabel;
        [SerializeField] private TextMeshProUGUI _conquerorLabelText;
        [SerializeField] private TextMeshProUGUI _conquerorPointText;

        //생존 시간
        [Header("SurvivalRecord")]
        [SerializeField] private Image _bestRecordLabel;
        [SerializeField] private TextMeshProUGUI _bestRecordLabelText;
        [SerializeField] private TextMeshProUGUI _bestRecordTimeText;

        //경험치 UI
        [Header("ExpBar")]
        [SerializeField] private Image _expLabel;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private Slider _expSlider;
        [SerializeField] private TextMeshProUGUI _currentExpText;
        [SerializeField] private TextMeshProUGUI _slashText;
        [SerializeField] private TextMeshProUGUI _maxExpText;

        //보상
        [Header("Reward")]
        [SerializeField] private Image _rewardBackground;
        [SerializeField] private Image _rewardItemBackground;
        [SerializeField] private TextMeshProUGUI _rewardTitleText;

        //골드
        [Header("Reward_Gold")]
        [SerializeField] private Image _goldIconImage;
        [SerializeField] private Image _goldLabel;
        [SerializeField] private TextMeshProUGUI _goldLabelText;
        [SerializeField] private TextMeshProUGUI _goldResultText;

        //클리어 보상
        [Header("Reward_Clear")]
        [SerializeField] private Image _clearLabel;
        [SerializeField] private TextMeshProUGUI _clearLabelText;
        [SerializeField] private TextMeshProUGUI _clearResultText;
        [SerializeField] private List<RewardItemCard> _rewardItems;
        [SerializeField] private ScrollRect _rewardItemDisplayScroll;
        [SerializeField] private HorizontalLayoutGroup _rewardItemLayoutGroup;

        //골드 추가 획득 퍼센트 표시
        [Header("Buff")]
        [SerializeField] private Image _goldBuffBackground;
        [SerializeField] private TextMeshProUGUI _goldBuffLabelText;
        [SerializeField] private TextMeshProUGUI _goldBuffPercentText;
        [SerializeField] private Transform _successGoldBuffPosition;
        [SerializeField] private Transform _failGoldBuffPosition;

        [Header("ConfirmButton")]
        [SerializeField] private ZButton _confirmButton;

        [Header("OneMoreButton")]
        [SerializeField] private ZButton _oneMoreButton;
        [SerializeField] private TextMeshProUGUI _oneMoreButtonText;

        private string _chapterName = null!;

        private int _killCount = 0;
        private long _incrementAccountExp = 0;

        private int _dropGold = 0;
        private int _clearGold = 0;

        private float _stageRunningtime;
        private long _previousBestRecordStagePlayTime;

        private long _levelUpRewardGold; // 스테이지 완료시 레벨업시, 계정레벨업에 따른 골드도 적용되기 때문에 골드 연출에서 해당 값만큼 유저 Gold를 제외하고 연출을 진행합니다.  

        private int _previousAccountLevel;
        private long _previousAccountExp;

        // 서버로 결과 종료 요청이 잘 처리되었는지 여부.
        private bool _isServerRequestCompleted;

        private float _mainChapterGoldBuffPercent;

        private bool _isOneMoreButtonEnable = false;

        private bool _showPreviousBestRecored;
        private bool _showConquerorPointAndExpBar;

        public void InitializeForMainChapterResult(
            ChapterStaticData chapter,
            StagePlayResult stagePlayResult,
            long totalGold,
            long totalGem,
            long totalRandomEquipmentElement,
            float stageRunningtime,
            long eliminatedBosses,
            long eliminatedElites,
            long eliminatedMonsters,
            Action<bool> closeRequester)
        {
            base.InitializeBase(closeRequester);
            _isServerRequestCompleted = false;
            _stageRunningtime = stageRunningtime;
            _killCount = (int)eliminatedMonsters;
            _chapterName = chapter.ChapterName;

            this.SetInitialState(chapter);
            _showConquerorPointAndExpBar = true;

            this.SaveMainChapterResultToServer(chapter, stagePlayResult, stageRunningtime, totalGold, totalGem, totalRandomEquipmentElement, eliminatedBosses, eliminatedElites, eliminatedMonsters);
        }

        public override void Close(bool skipAnimation)
        {
            base.Close(skipAnimation);
            _confirmButton.SetInteractable(false, false);
            _oneMoreButton.SetInteractable(false, false);
        }

        private void SetInitialState(ChapterStaticData chapter)
        {
            var userGameData = GameClient.CS.UserGameData;
            if (userGameData == null)
            {
                Debug.LogWarning("로그인 안 된 상태에서 결과창을 요청했습니다. 더미 데이터를 사용합니다.");
                _previousAccountLevel = 2;
                _previousAccountExp = 50;
                _previousBestRecordStagePlayTime = 0;
            }
            else
            {
                // 챕터 종료 결과를 서버로부터 받기 전의 Level, Exp에서부터 디스플레이를 시작한다.
                _previousAccountLevel = userGameData.AccountLevel;
                _previousAccountExp = userGameData.AccountExp;

                if (chapter.ChapterNumber == userGameData.ClearedHighestChapter + 1)
                {
                    _showPreviousBestRecored = true;
                    _previousBestRecordStagePlayTime = userGameData.HighestStageTimeInSeconds;
                }
                else
                {
                    _showPreviousBestRecored = false;
                    _previousBestRecordStagePlayTime = long.MaxValue;
                }
            }

            // 구독(골드 버프) 컨텐츠는 데모에 없으므로 추가 골드 획득량은 항상 0이다.
            _mainChapterGoldBuffPercent = 0f;


            //스테이지 결과창 스파인 파일 비활성화
            {
                _successTitle.gameObject.SetActive(false);
                _failTitle.gameObject.SetActive(false);
                _failTitle.text = Localizer.Instance.GetText("UI_STAGE_RESULT_POPUP_FAILED");
            }

            //챕터 이름 UI 비활성화
            {
                _chapterNameLabel.gameObject.SetActive(false);
                _chapterNameText.gameObject.SetActive(false);
            }
            //실패시 나오는 생존 기록 비활성화
            {
                _newRecordLabelText.gameObject.SetActive(false);
                _survivalTimeText.gameObject.SetActive(false);
                _survivalLabelText.gameObject.SetActive(false);

                _newRecordLabelText.text = Localizer.Instance.GetText("UI_STAGE_RESULT_POPUP_NEW_RECORD");
                _survivalLabelText.text = Localizer.Instance.GetText("UI_STAGE_RESULT_POPUP_TIME_SURVIVAL");
            }

            //킬 카운트 UI 비활성화
            {
                _killIconImage.gameObject.SetActive(false);
                _killIconLabel.gameObject.SetActive(false);
                _killCountText.gameObject.SetActive(false);
            }

            //정복자 포인트 UI 비활성화
            {
                _conquerorLabel.gameObject.SetActive(false);
                _conquerorLabelText.gameObject.SetActive(false);
                _conquerorPointText.gameObject.SetActive(false);

                _conquerorLabelText.text = Localizer.Instance.GetText("UI_STAGE_RESULT_POPUP_CONQUEROR_POINT");
            }

            //생존시간 UI 비활성화
            {
                _bestRecordLabel.gameObject.SetActive(false);
                _bestRecordLabelText.gameObject.SetActive(false);
                _bestRecordTimeText.gameObject.SetActive(false);

                _bestRecordLabelText.text = Localizer.Instance.GetText("UI_STAGE_RESULT_POPUP_BEST_RECORD");
            }

            //경험치 슬라이더 UI 비활성화
            {
                _expLabel.gameObject.SetActive(false);
                _levelText.gameObject.SetActive(false);
                _expSlider.gameObject.SetActive(false);

                _currentExpText.gameObject.SetActive(false);
                _slashText.gameObject.SetActive(false);
                _maxExpText.gameObject.SetActive(false);
            }

            //보상 백그라운드 이미지 비활성화
            {
                _rewardBackground.gameObject.SetActive(false);
                _rewardItemBackground.gameObject.SetActive(false);
                _rewardTitleText.gameObject.SetActive(false);

                _rewardTitleText.text = Localizer.Instance.GetText("UI_STAGE_RESULT_POPUP_REWARD");
            }

            //골드 UI 비활성화
            {
                _goldIconImage.gameObject.SetActive(false);
                _goldLabel.gameObject.SetActive(false);
                _goldLabelText.gameObject.SetActive(false);
                _goldResultText.gameObject.SetActive(false);

                _goldLabelText.text = Localizer.Instance.GetText("UI_GOLD");
            }

            //클리어 골드 UI 비활성화
            {
                _clearLabel.gameObject.SetActive(false);
                _clearLabelText.gameObject.SetActive(false);
                _clearResultText.gameObject.SetActive(false);
                _clearLabelText.text = Localizer.Instance.GetText("UI_CLEAR");
            }

            //골드 버프 UI 비활성화
            {
                _goldBuffBackground.gameObject.SetActive(false);
                _goldBuffLabelText.gameObject.SetActive(false);
                _goldBuffPercentText.gameObject.SetActive(false);
                _goldBuffLabelText.text = Localizer.Instance.GetText("UI_STAGERESULT_BUFF");
                _goldBuffPercentText.text = $"+{_mainChapterGoldBuffPercent.ToString("N0")}%";
            }

            //확인 버튼 비활성화
            {
                _confirmButton.gameObject.SetActive(false);
                _confirmButton.onClick.RemoveAllListeners();
                _confirmButton.onClick.AddListener(() =>
                {
                    if (!_isServerRequestCompleted)
                    {
                        UnityGlobal.Scenes.GetCurrentSceneUI().AddCommonMessagePopup(
                            "Processing the result.",
                            "Please try again later.",
                            "OK",
                            () => { });
                        return;
                    }

                    if (!_confirmButton.IsActive())
                    {
                        return;
                    }

                    this.Close(skipAnimation: false);
                    UnityGlobal.Scenes.ChangeTo(SceneType.Lobby, new LobbySceneInitialData(GameClient.CS.UserGameData), "스테이지 종료");
                });
                _confirmButton.ButtonText.text = Localizer.Instance.GetText("UI_OK");
            }

            //한번더 버튼 비활성화
            {
                _oneMoreButton.gameObject.SetActive(false);
                _oneMoreButton.onClick.RemoveAllListeners();

                if (userGameData != null)
                {
                    _oneMoreButton.SetInteractable(true);
                    _oneMoreButton.onClick.AddListener(() =>
                    {
                        if (!_isServerRequestCompleted)
                        {
                            UnityGlobal.Scenes.GetCurrentSceneUI().AddCommonMessagePopup(
                                "Processing the result.",
                                "Please try again later.",
                                "OK",
                                () => { });
                            return;
                        }

                        if (!_oneMoreButton.IsActive())
                        {
                            return;
                        }

                        this.Close(skipAnimation: false);
                        this.StartCurrentChapterOneMoreTime(chapter);
                    });
                    _oneMoreButtonText.text = Localizer.Instance.GetText("UI_CHAPTER_PLAY_ONEMORE");
                }
            }
        }

        private void StartCurrentChapterOneMoreTime(ChapterStaticData chapter)
        {
            if (chapter == null)
            {
                return;
            }

            GameClient.CS.EnterChapter(new EnterChapterRequest(chapter.ChapterNumber),
                onCompleted: (EnterChapterResponse response) =>
                {
                    Debug.Log($"{response.ResultCode}");

                    switch (response.ResultCode)
                    {
                        case EnterChapterResultCode.Success:
                            {
                                var userSkillDeck = GameClient.CS.UserSkillDeck;
                                var userGameData = GameClient.CS.UserGameData!;
                                var heroData = userGameData.GetSelectedHeroData()!;

                                var equipmentInventory = userGameData.EquipmentInventory();

                                var stageInitialData = StageSceneInitialData.CreateForMainChapter(
                                    chapter,
                                    userSkillDeck,
                                    heroData,
                                    equipmentInventory.GetEquippedEquipments().Values);

                                UnityGlobal.Scenes.ChangeTo(SceneType.Stage, stageInitialData, "챕터 재도전");
                            }
                            break;
                        case EnterChapterResultCode.NotClearedPreviousChapter:
                        case EnterChapterResultCode.InvalidChapter:
                            {
                                UnityGlobal.Scenes.ChangeTo(SceneType.Lobby, new LobbySceneInitialData(GameClient.CS.UserGameData), $"resultCode[{response.ResultCode}] 챕터 재도전 실패");
                            }
                            break;
                        case EnterChapterResultCode.AlreadyChapterPlaying:
                        case EnterChapterResultCode.InvalidSession:
                            {
                                UnityGlobal.HandleInvalidSessionInfo(nameof(EnterChapterRequest), GameClient.CS.ApplicationVersion, GameClient.CS.CachedAccountId);
                            }
                            break;
                        default:
                            throw new NotImplementedException($"{response.ResultCode} is not handled.");
                    }
                }, UnityGlobal.HandleInternalServerError_LogAndRestart, UnityGlobal.HandleNetworkErrorAndContinueRetry);
        }

        private void PlaySuccessAnimation()
        {
            long nextLevelUpMaxExp = StaticDataRepository.Instance.AccountLevels.GetExpForLevelUp(_previousAccountLevel);
            Sequence sequenceAnimation = DOTween.Sequence();

            //스테이지 결과창 성공 타이틀 애니메이션 
            Sequence successTitleSequence = DOTween.Sequence();
            successTitleSequence.AppendCallback(() =>
            {
                _successTitle.UnscaledTime = true;
                _successTitle.gameObject.SetActive(true);
                _successTitle.AnimationState.SetAnimation(0, "Begin", false);
                _successTitle.AnimationState.AddAnimation(0, "Repeat", true, 0f);
            });
            successTitleSequence.AppendInterval(_successTitle.Skeleton.Data.FindAnimation("Begin").Duration - 0.2f);

            //챕터 이름 활성화 시퀀스
            Sequence chapterNameSequence = DOTween.Sequence();
            chapterNameSequence.AppendCallback(() =>
            {
                _chapterNameLabel.gameObject.SetActive(true);
                _chapterNameLabel.color = new Color(1f, 1f, 1f, 0f);

                _chapterNameText.gameObject.SetActive(true);
                _chapterNameText.color = new Color(1f, 1f, 1f, 0f);
                _chapterNameText.text = _chapterName;
            });
            chapterNameSequence.Append(_chapterNameLabel.DOFade(1f, 0.2f));
            chapterNameSequence.Append(_chapterNameText.DOFade(1f, 0.2f));

            //킬 카운트 활성화 시퀀스
            Sequence killCountSequence = DOTween.Sequence();
            killCountSequence.AppendCallback(() =>
            {
                _killIconLabel.gameObject.SetActive(true);
                _killIconLabel.color = new Color(1f, 1f, 1f, 0f);
                _killIconImage.gameObject.SetActive(true);
                _killIconImage.color = new Color(1f, 1f, 1f, 0f);
                _killCountText.gameObject.SetActive(true);
                _killCountText.color = new Color(1f, 1f, 1f, 0f);
                _killCountText.text = "0";
            });
            killCountSequence.Append(_killIconLabel.DOFade(1f, 0.2f));
            killCountSequence.Join(_killIconImage.DOFade(1f, 0.2f));
            killCountSequence.Append(_killCountText.DOCounter(0, (int)_killCount, 0.2f));
            killCountSequence.Join(_killCountText.DOFade(1f, 0.2f));

            //정복자 포인트 활성화 시퀀스
            Sequence conquerorSequence = DOTween.Sequence();
            conquerorSequence.AppendCallback(() =>
            {
                _conquerorLabel.gameObject.SetActive(true);
                _conquerorLabel.color = new Color(1f, 1f, 1f, 0f);
                _conquerorLabelText.gameObject.SetActive(true);
                _conquerorLabelText.color = new Color(1f, 1f, 1f, 0f);
                _conquerorPointText.gameObject.SetActive(true);
                _conquerorPointText.color = new Color(1f, 1f, 1f, 0f);
                _conquerorPointText.text = "0";
            });
            conquerorSequence.Append(_conquerorLabel.DOFade(1f, 0.2f));
            conquerorSequence.Join(_conquerorLabelText.DOFade(1f, 0.2f));
            conquerorSequence.Join(_conquerorPointText.DOFade(1f, 0.2f));
            conquerorSequence.Join(_conquerorPointText.DOCounter(0, (int)_incrementAccountExp, 0.2f));

            //경험치 슬라이더 활성화 시퀀스
            Sequence expSliderSequence = DOTween.Sequence();
            expSliderSequence.AppendCallback(() =>
            {
                _expLabel.gameObject.SetActive(true);
                _expLabel.color = new Color(1f, 1f, 1f, 0f);
                _levelText.gameObject.SetActive(true);
                _levelText.color = new Color(1f, 1f, 1f, 0f);
                _levelText.text = $"Lv {_previousAccountLevel}";
            });
            expSliderSequence.Append(_expLabel.DOFade(1f, 0.2f));
            expSliderSequence.Join(_levelText.DOFade(1f, 0.2f));

            Sequence expSliderMoveSequence = DOTween.Sequence();
            expSliderMoveSequence.AppendCallback(() =>
            {
                _expSlider.gameObject.SetActive(true);
                _expSlider.value = _previousAccountExp / nextLevelUpMaxExp;

                _currentExpText.gameObject.SetActive(true);
                _slashText.gameObject.SetActive(true);
                _maxExpText.gameObject.SetActive(true);

                _currentExpText.text = $"{_previousAccountExp}";
                _maxExpText.text = $"{nextLevelUpMaxExp:N0}";
            });

            long leftExp = _previousAccountExp + _incrementAccountExp;
            int nextLevel = _previousAccountLevel + 1;

            if (nextLevelUpMaxExp > leftExp)
            {
                // 레벨업 아닐 때 연출
                expSliderMoveSequence.Join(_expSlider.GetComponent<Slider>().DOValue((float)(leftExp) / nextLevelUpMaxExp, 0.4f));
                expSliderMoveSequence.Join(_currentExpText.DOCounter(0, (int)leftExp, 0.4f));
            }
            else
            {
                // 레벨업일때연출
                while (nextLevelUpMaxExp <= leftExp)
                {
                    long maxExpForDisplayingNextLevel = StaticDataRepository.Instance.AccountLevels.GetExpForLevelUp(nextLevel + 1);

                    expSliderMoveSequence.AppendInterval(0.03f);
                    expSliderMoveSequence.Append(_expSlider.GetComponent<Slider>().DOValue(1.0f, 0.4f));
                    expSliderMoveSequence.Join(_currentExpText.DOCounter((int)_previousAccountExp, (int)nextLevelUpMaxExp, 0.4f));
                    int diplayLevel = nextLevel;
                    expSliderMoveSequence.AppendCallback(() =>
                    {
                        _levelText.rectTransform.localScale = new Vector3(2.5f, 2.5f, 2.5f);
                        _levelText.color = new Color(1f, 1f, 1f, 0.5f);
                        _levelText.text = $"Lv {diplayLevel}";
                        _expSlider.GetComponent<Slider>().value = 0f;
                        _currentExpText.text = "0";
                        _maxExpText.text = $"{maxExpForDisplayingNextLevel:N0}";
                    });

                    expSliderMoveSequence.Join(_levelText.DOScale(1f, 0.2f));
                    expSliderMoveSequence.Join(_levelText.DOFade(1f, 0.2f));
                    expSliderMoveSequence.Join(_expSlider.GetComponent<Slider>()
                        .DOValue((float)(leftExp - nextLevelUpMaxExp) / maxExpForDisplayingNextLevel, 0.3f));
                    expSliderMoveSequence.Join(_currentExpText.DOCounter(0, (int)(leftExp - nextLevelUpMaxExp), 0.3f));

                    leftExp -= nextLevelUpMaxExp;
                    nextLevel += 1;
                    nextLevelUpMaxExp = StaticDataRepository.Instance.AccountLevels.GetExpForLevelUp(nextLevel);
                }
            }

            Sequence rewardSequence = DOTween.Sequence();
            rewardSequence.AppendCallback(() =>
            {
                _rewardBackground.gameObject.SetActive(true);
                _rewardBackground.color = new Color(1f, 1f, 1f, 0f);
                _rewardItemBackground.gameObject.SetActive(true);
                _rewardItemBackground.color = new Color(0f, 0f, 0f, 0f);
                _rewardTitleText.gameObject.SetActive(true);
                _rewardTitleText.color = new Color(1f, 1f, 1f, 0f);

                _goldLabel.gameObject.SetActive(true);
                _goldLabel.color = new Color(1f, 1f, 1f, 0f);
                _goldIconImage.gameObject.SetActive(true);
                _goldIconImage.color = new Color(1f, 1f, 1f, 0f);
                _goldLabelText.gameObject.SetActive(true);
                _goldLabelText.color = new Color(1f, 1f, 1f, 0f);
                _goldResultText.gameObject.SetActive(true);
                _goldResultText.color = new Color(1f, 1f, 1f, 0f);

                _clearLabel.gameObject.SetActive(true);
                _clearLabel.color = new Color(1f, 1f, 1f, 0f);
                _clearLabelText.gameObject.SetActive(true);
                _clearLabelText.color = new Color(1f, 1f, 1f, 0f);
                _clearResultText.gameObject.SetActive(true);
                _clearResultText.color = new Color(1f, 0.7333333f, 0.09803922f, 0f);

                _goldBuffBackground.gameObject.SetActive(true);
                _goldBuffBackground.transform.position = _successGoldBuffPosition.position;
                _goldBuffBackground.color = new Color(1f, 1f, 1f, 0f);
                _goldBuffLabelText.gameObject.SetActive(true);
                _goldBuffLabelText.color = new Color(1f, 0.7333333f, 0.09803922f, 0f);
                _goldBuffPercentText.gameObject.SetActive(true);
                _goldBuffPercentText.color = new Color(0f, 0f, 0f, 0f);

                _confirmButton.gameObject.SetActive(false);
            });


            if (_mainChapterGoldBuffPercent != 0f)
            {
                //골드 버프 추가 연출
                rewardSequence.Append(_rewardBackground.DOFade(1f, 0.2f));
                rewardSequence.Join(_rewardItemBackground.DOFade(1f, 0.2f));
                rewardSequence.Join(_rewardTitleText.DOFade(1f, 0.2f));

                rewardSequence.Append(_goldLabel.DOFade(1f, 0.2f));
                rewardSequence.Join(_goldIconImage.DOFade(1f, 0.2f));
                rewardSequence.Join(_goldLabelText.DOFade(1f, 0.2f));

                rewardSequence.Append(_goldResultText.DOFade(1f, 0.2f));
                rewardSequence.Join(_goldResultText.DOCounter(0, (_dropGold * 100) / (100 + (int)_mainChapterGoldBuffPercent), 0.2f));


                rewardSequence.Append(_clearLabel.DOFade(1f, 0.2f));
                rewardSequence.Join(_clearLabelText.DOFade(1f, 0.2f));
                rewardSequence.Append(_clearResultText.DOFade(1f, 0.2f));
                rewardSequence.Join(_clearResultText.DOCounter(0, (_clearGold * 100) / (100 + (int)_mainChapterGoldBuffPercent), 0.2f));

                rewardSequence.AppendInterval(0.2f);

                rewardSequence.Append(_goldBuffBackground.DOFade(1f, 0.2f));
                rewardSequence.Join(_goldBuffLabelText.DOFade(1f, 0.2f));
                rewardSequence.Join(_goldBuffPercentText.DOFade(1f, 0.2f));

                rewardSequence.AppendInterval(0.2f);

                rewardSequence.Append(_goldResultText.DOFade(0f, 0f));
                rewardSequence.Join(_clearResultText.DOFade(0f, 0f));
                rewardSequence.Join(_goldResultText.DOFade(1f, 0.1f));
                rewardSequence.Join(_clearResultText.DOFade(1f, 0.1f));
                rewardSequence.Join(_goldResultText.DOCounter(0, _dropGold, 0.3f));
                rewardSequence.Join(_clearResultText.DOCounter(0, _clearGold, 0.3f));
            }
            else
            {
                //일반 골드 연출
                rewardSequence.Append(_rewardBackground.DOFade(1f, 0.2f));
                rewardSequence.Join(_rewardItemBackground.DOFade(1f, 0.2f));
                rewardSequence.Join(_rewardTitleText.DOFade(1f, 0.2f));

                rewardSequence.Append(_goldLabel.DOFade(1f, 0.2f));
                rewardSequence.Join(_goldIconImage.DOFade(1f, 0.2f));
                rewardSequence.Join(_goldLabelText.DOFade(1f, 0.2f));

                rewardSequence.Append(_goldResultText.DOFade(1f, 0.2f));
                rewardSequence.Join(_goldResultText.DOCounter(0, _dropGold, 0.2f));

                rewardSequence.Append(_clearLabel.DOFade(1f, 0.2f));
                rewardSequence.Join(_clearLabelText.DOFade(1f, 0.2f));
                rewardSequence.Append(_clearResultText.DOFade(1f, 0.2f));
                rewardSequence.Join(_clearResultText.DOCounter(0, _clearGold, 0.2f));
            }

            foreach (var rewardItem in _rewardItems)
            {
                var fadeImage = this.CreateFadeImage(rewardItem.BackGround);
                rewardSequence.AppendCallback(() =>
                {
                    rewardItem.gameObject.SetActive(true);
                    fadeImage.color = Color.white;
                    LayoutRebuilder.ForceRebuildLayoutImmediate(_rewardItemDisplayScroll.content);
                });
                rewardSequence.Append(fadeImage.DOFade(0f, 0.5f));
            }

            //클리어한 메인 챕터 결과 요청시에는 한번더 버튼도 같이 노출한다.
            if (_isOneMoreButtonEnable)
            {
                rewardSequence.AppendCallback(() =>
                {
                    _oneMoreButton.gameObject.SetActive(true);
                    _confirmButton.gameObject.SetActive(true);
                });
                rewardSequence.Append(_confirmButton.ButtonImage.DOFade(1f, 0.2f).From(0.0f));
                rewardSequence.Join(_confirmButton.ButtonText.DOFade(1f, 0.2f).From(0.0f));

                rewardSequence.Join(_oneMoreButton.ButtonImage.DOFade(1f, 0.2f).From(0.0f));
                rewardSequence.Join(_oneMoreButtonText.DOFade(1f, 0.2f).From(0.0f));
            }
            else
            {
                rewardSequence.AppendCallback(() => _confirmButton.gameObject.SetActive(true));
                rewardSequence.Append(_confirmButton.ButtonImage.DOFade(1f, 0.2f).From(0.0f));
                rewardSequence.Join(_confirmButton.ButtonText.DOFade(1f, 0.2f).From(0.0f));
            }

            sequenceAnimation.Append(successTitleSequence); //성공 그래픽 스파인 시퀀스
            sequenceAnimation.Append(chapterNameSequence);  //챕터 이름 활성화 시퀀스
            sequenceAnimation.Append(killCountSequence);    //킬카운트 활성화 시퀀스

            // 경험치 시퀀스.
            if (_showConquerorPointAndExpBar)
            {
                sequenceAnimation.Append(conquerorSequence);
                sequenceAnimation.Append(expSliderSequence);
                sequenceAnimation.Append(expSliderMoveSequence);
            }
            else
            {
                conquerorSequence.Kill();
                expSliderSequence.Kill();
                expSliderMoveSequence.Kill();
            }

            sequenceAnimation.Append(rewardSequence);       //보상 시퀀스

            sequenceAnimation.SetUpdate(isIndependentUpdate: true);
            sequenceAnimation.SetAutoKill(false);
            sequenceAnimation.Play();
        }

        private void PlayFailAnimation()
        {
            Sequence sequenceAnimation = DOTween.Sequence();

            //스테이지 결과창 실패 타이틀 애니메이션 
            Sequence failTitleSequence = DOTween.Sequence();
            failTitleSequence.AppendCallback(() =>
            {
                _failTitle.gameObject.SetActive(true);
            });
            failTitleSequence.Append(_failTitle.DOFade(1f, 0.3f).From(0));

            //챕터 이름 활성화 시퀀스
            Sequence chapterNameSequence = DOTween.Sequence();
            chapterNameSequence.AppendCallback(() =>
            {
                _chapterNameLabel.gameObject.SetActive(true);
                _chapterNameLabel.color = new Color(1f, 1f, 1f, 0f);

                _chapterNameText.gameObject.SetActive(true);
                _chapterNameText.color = new Color(1f, 1f, 1f, 0f);
                _chapterNameText.text = _chapterName;
            });
            chapterNameSequence.Append(_chapterNameLabel.DOFade(1f, 0.2f));
            chapterNameSequence.Append(_chapterNameText.DOFade(1f, 0.2f));

            //생존기록 표시 시퀀스
            Sequence surviveRecordSequence = DOTween.Sequence();
            surviveRecordSequence.AppendCallback(() =>
            {
                if (_previousBestRecordStagePlayTime < _stageRunningtime)
                {
                    _newRecordLabelText.gameObject.SetActive(true);
                    _newRecordLabelText.color = new Color(1f, 0.7333333f, 0.09803922f, 0f);
                }
                else
                {
                    _survivalLabelText.gameObject.SetActive(true);
                    _survivalLabelText.color = new Color(1f, 1f, 1f, 0f);
                }
                _survivalTimeText.gameObject.SetActive(true);
                _survivalTimeText.color = new Color(1f, 1f, 1f, 0f);
                int minute = (int)_stageRunningtime / 60;
                int second = (int)_stageRunningtime % 60;
                _survivalTimeText.text = string.Format("{0:D1}:{1:D2}", minute, second);
            });
            if (_previousBestRecordStagePlayTime < _stageRunningtime)
            {
                surviveRecordSequence.Join(_newRecordLabelText.DOFade(1f, 0.2f));
            }
            else
            {
                surviveRecordSequence.Join(_survivalLabelText.DOFade(1f, 0.2f));
            }
            surviveRecordSequence.Append(_survivalTimeText.DOFade(1f, 0.2f));

            //킬 카운트 활성화 시퀀스
            Sequence killCountSequence = DOTween.Sequence();
            killCountSequence.AppendCallback(() =>
            {
                _killIconLabel.gameObject.SetActive(true);
                _killIconImage.color = new Color(1f, 1f, 1f, 0f);
                _killIconImage.gameObject.SetActive(true);
                _killIconLabel.color = new Color(1f, 1f, 1f, 0f);
                _killCountText.gameObject.SetActive(true);
                _killCountText.color = new Color(1f, 1f, 1f, 0f);
                _killCountText.text = "0";
            });
            killCountSequence.Append(_killIconLabel.DOFade(1f, 0.2f));
            killCountSequence.Join(_killIconImage.DOFade(1f, 0.2f));
            killCountSequence.Append(_killCountText.DOCounter(0, (int)_killCount, 0.2f));
            killCountSequence.Join(_killCountText.DOFade(1f, 0.2f));

            //최고시간시간 활성화 시퀀스
            Sequence bestRecordSequence = DOTween.Sequence();
            bestRecordSequence.AppendCallback(() =>
            {
                _bestRecordLabel.gameObject.SetActive(true);
                _bestRecordLabel.color = new Color(1f, 1f, 1f, 0f);

                _bestRecordLabelText.gameObject.SetActive(true);
                _bestRecordLabelText.color = new Color(1f, 1f, 1f, 0f);

                //신기록일때 new record 활성화 처리
                if (_previousBestRecordStagePlayTime < _stageRunningtime)
                {
                    int minute = (int)_stageRunningtime / 60;
                    int second = (int)_stageRunningtime % 60;
                    _bestRecordTimeText.text = string.Format("{0:D1}:{1:D2}", minute, second);
                }
                else
                {
                    int minute = (int)_previousBestRecordStagePlayTime / 60;
                    int second = (int)_previousBestRecordStagePlayTime % 60;
                    _bestRecordTimeText.text = string.Format("{0:D1}:{1:D2}", minute, second);
                }

                _bestRecordTimeText.gameObject.SetActive(true);
                _bestRecordTimeText.color = new Color(1f, 1f, 1f, 0f);

            });
            bestRecordSequence.Append(_bestRecordLabel.DOFade(1f, 0.2f));
            bestRecordSequence.Join(_bestRecordLabelText.DOFade(1f, 0.2f));
            bestRecordSequence.Join(_bestRecordTimeText.DOFade(1f, 0.2f));

            //정복자 포인트 활성화 시퀀스
            Sequence conquerorSequence = DOTween.Sequence();
            conquerorSequence.AppendCallback(() =>
            {
                _conquerorLabel.gameObject.SetActive(true);
                _conquerorLabel.color = new Color(1f, 1f, 1f, 0f);
                _conquerorLabelText.gameObject.SetActive(true);
                _conquerorLabelText.color = new Color(1f, 1f, 1f, 0f);
                _conquerorPointText.gameObject.SetActive(true);
                _conquerorPointText.color = new Color(1f, 1f, 1f, 0f);
                _conquerorPointText.text = "0";
            });
            conquerorSequence.Append(_conquerorLabel.DOFade(1f, 0.2f));
            conquerorSequence.Join(_conquerorLabelText.DOFade(1f, 0.2f));
            conquerorSequence.Join(_conquerorPointText.DOFade(1f, 0.2f));
            conquerorSequence.Join(_conquerorPointText.DOCounter(0, (int)_incrementAccountExp, 0.2f));

            //경험치 슬라이더 활성화 시퀀스
            Sequence expSliderSequence = DOTween.Sequence();
            expSliderSequence.AppendCallback(() =>
            {
                _expLabel.gameObject.SetActive(true);
                _expLabel.color = new Color(1f, 1f, 1f, 0f);
                _levelText.gameObject.SetActive(true);
                _levelText.color = new Color(1f, 1f, 1f, 0f);
                _levelText.text = $"Lv {_previousAccountLevel}";
            });
            expSliderSequence.Append(_expLabel.DOFade(1f, 0.2f));
            expSliderSequence.Join(_levelText.DOFade(1f, 0.2f));

            long previousAccountLevelMaxExp = StaticDataRepository.Instance.AccountLevels.GetExpForLevelUp(_previousAccountLevel);

            Sequence expSliderMoveSequence = DOTween.Sequence();
            expSliderMoveSequence.AppendCallback(() =>
            {
                _expSlider.gameObject.SetActive(true);
                _expSlider.value = _previousAccountExp / previousAccountLevelMaxExp;

                _currentExpText.gameObject.SetActive(true);
                _slashText.gameObject.SetActive(true);
                _maxExpText.gameObject.SetActive(true);

                _currentExpText.text = $"{_previousAccountExp}";
                _maxExpText.text = $"{previousAccountLevelMaxExp:N0}";
            });
            long totalExp = _previousAccountExp + _incrementAccountExp;

            //레벨업 일때 연출
            if (totalExp >= previousAccountLevelMaxExp)
            {
                expSliderMoveSequence.Join(_expSlider.GetComponent<Slider>().DOValue(1.0f, 0.4f));
                expSliderMoveSequence.Join(_currentExpText.DOCounter((int)_previousAccountExp, (int)previousAccountLevelMaxExp, 0.4f));
                expSliderMoveSequence.AppendCallback(() =>
                {
                    _levelText.rectTransform.localScale = new Vector3(2.5f, 2.5f, 2.5f);
                    _levelText.color = new Color(1f, 1f, 1f, 0.5f);
                    _levelText.text = $"Lv {_previousAccountLevel + 1}";
                    _expSlider.GetComponent<Slider>().value = 0f;
                    _currentExpText.text = "0";
                    _maxExpText.text = $"{StaticDataRepository.Instance.AccountLevels.GetExpForLevelUp(_previousAccountLevel + 1):N0}";
                });

                expSliderMoveSequence.Join(_levelText.DOScale(1f, 0.2f));
                expSliderMoveSequence.Join(_levelText.DOFade(1f, 0.2f));

                var nextLevelMaxExp = StaticDataRepository.Instance.AccountLevels.GetExpForLevelUp(_previousAccountLevel + 1);
                expSliderMoveSequence.Join(
                    _expSlider.GetComponent<Slider>().DOValue((float)(totalExp - previousAccountLevelMaxExp) / nextLevelMaxExp, 0.3f));
                expSliderMoveSequence.Join(_currentExpText.DOCounter(0, (int)(totalExp - previousAccountLevelMaxExp), 0.3f));
            }
            else            //레벨업 안할때 연출 (만렙 일때 연출 포함)
            {
                expSliderMoveSequence.Join(_expSlider.GetComponent<Slider>().DOValue((float)(totalExp) / previousAccountLevelMaxExp, 0.4f));
                expSliderMoveSequence.Join(_currentExpText.DOCounter(0, (int)totalExp, 0.4f));
            }

            Sequence rewardSequence = DOTween.Sequence();
            rewardSequence.AppendCallback(() =>
            {
                _rewardBackground.gameObject.SetActive(true);
                _rewardBackground.color = new Color(1f, 1f, 1f, 0f);
                _rewardItemBackground.gameObject.SetActive(true);
                _rewardItemBackground.color = new Color(0f, 0f, 0f, 0f);
                _rewardTitleText.gameObject.SetActive(true);
                _rewardTitleText.color = new Color(1f, 1f, 1f, 0f);

                _goldLabel.gameObject.SetActive(true);
                _goldLabel.color = new Color(1f, 1f, 1f, 0f);
                _goldIconImage.gameObject.SetActive(true);
                _goldIconImage.color = new Color(1f, 1f, 1f, 0f);
                _goldLabelText.gameObject.SetActive(true);
                _goldLabelText.color = new Color(1f, 1f, 1f, 0f);
                _goldResultText.gameObject.SetActive(true);
                _goldResultText.color = new Color(1f, 1f, 1f, 0f);
                _goldResultText.transform.position = _clearResultText.transform.position;

                _goldBuffBackground.gameObject.SetActive(true);
                _goldBuffBackground.transform.position = _failGoldBuffPosition.position;
                _goldBuffBackground.color = new Color(1f, 1f, 1f, 0f);
                _goldBuffLabelText.gameObject.SetActive(true);
                _goldBuffLabelText.color = new Color(1f, 0.7333333f, 0.09803922f, 0f);
                _goldBuffPercentText.gameObject.SetActive(true);
                _goldBuffPercentText.color = new Color(0f, 0f, 0f, 0f);

                _confirmButton.gameObject.SetActive(false);
            });

            if (_mainChapterGoldBuffPercent != 0)
            {
                //골드 버프 추가 연출
                rewardSequence.Append(_rewardBackground.DOFade(1f, 0.2f));
                rewardSequence.Join(_rewardItemBackground.DOFade(1f, 0.2f));
                rewardSequence.Join(_rewardTitleText.DOFade(1f, 0.2f));

                rewardSequence.Append(_goldLabel.DOFade(1f, 0.2f));
                rewardSequence.Join(_goldIconImage.DOFade(1f, 0.2f));
                rewardSequence.Join(_goldLabelText.DOFade(1f, 0.2f));

                rewardSequence.Append(_goldResultText.DOFade(1f, 0.2f));
                rewardSequence.Join(_goldResultText.DOCounter(0, (_dropGold * 100) / (100 + (int)_mainChapterGoldBuffPercent), 0.2f));

                rewardSequence.AppendInterval(0.2f);

                rewardSequence.Append(_goldBuffBackground.DOFade(1f, 0.2f));
                rewardSequence.Join(_goldBuffLabelText.DOFade(1f, 0.2f));
                rewardSequence.Join(_goldBuffPercentText.DOFade(1f, 0.2f));

                rewardSequence.AppendInterval(0.2f);

                rewardSequence.Append(_goldResultText.DOFade(0f, 0f));
                rewardSequence.Join(_goldResultText.DOFade(1f, 0.1f));
                rewardSequence.Join(_goldResultText.DOCounter(0, _dropGold, 0.3f));
            }
            else
            {
                //일반 골드 연출
                rewardSequence.Append(_rewardBackground.DOFade(1f, 0.2f));
                rewardSequence.Join(_rewardItemBackground.DOFade(1f, 0.2f));
                rewardSequence.Join(_rewardTitleText.DOFade(1f, 0.2f));

                rewardSequence.Append(_goldLabel.DOFade(1f, 0.2f));
                rewardSequence.Join(_goldIconImage.DOFade(1f, 0.2f));
                rewardSequence.Join(_goldLabelText.DOFade(1f, 0.2f));

                rewardSequence.Append(_goldResultText.DOFade(1f, 0.2f));
                rewardSequence.Join(_goldResultText.DOCounter(0, _dropGold, 0.2f));
            }

            foreach (var rewardItem in _rewardItems)
            {
                var fadeImage = this.CreateFadeImage(rewardItem.BackGround);
                rewardSequence.AppendCallback(() =>
                {
                    rewardItem.gameObject.SetActive(true);
                    fadeImage.color = Color.white;
                    LayoutRebuilder.ForceRebuildLayoutImmediate(_rewardItemDisplayScroll.content);
                });
                rewardSequence.Append(fadeImage.DOFade(0f, 0.5f));
            }

            //클리어한 메인 챕터 결과 요청시에는 한번더 버튼도 같이 노출한다.
            if (_isOneMoreButtonEnable)
            {
                rewardSequence.AppendCallback(() =>
                {
                    _oneMoreButton.gameObject.SetActive(true);
                    _confirmButton.gameObject.SetActive(true);
                });
                rewardSequence.Append(_confirmButton.ButtonImage.DOFade(1f, 0.2f).From(0.0f));
                rewardSequence.Join(_confirmButton.ButtonText.DOFade(1f, 0.2f).From(0.0f));

                rewardSequence.Join(_oneMoreButton.ButtonImage.DOFade(1f, 0.2f).From(0.0f));
                rewardSequence.Join(_oneMoreButtonText.DOFade(1f, 0.2f).From(0.0f));
            }
            else
            {
                rewardSequence.AppendCallback(() => _confirmButton.gameObject.SetActive(true));
                rewardSequence.Append(_confirmButton.ButtonImage.DOFade(1f, 0.2f).From(0.0f));
                rewardSequence.Join(_confirmButton.ButtonText.DOFade(1f, 0.2f).From(0.0f));
            }

            sequenceAnimation.Append(failTitleSequence);    //타이틀 스파인 시퀀스
            sequenceAnimation.Append(chapterNameSequence);  //챕터 이름 활성화 시퀀스
            sequenceAnimation.Append(surviveRecordSequence);//챕터 생존시간 시퀀스
            sequenceAnimation.Append(killCountSequence);    //킬카운트 활성화 시퀀스

            // 생존한 최고 시간 시퀀스.
            if (_showPreviousBestRecored)
            {
                sequenceAnimation.Append(bestRecordSequence);
            }
            else
            {
                bestRecordSequence.Kill();
            }

            // 경험치 시퀀스.
            if (_showConquerorPointAndExpBar)
            {
                sequenceAnimation.Append(conquerorSequence);
                sequenceAnimation.Append(expSliderSequence);
                sequenceAnimation.Append(expSliderMoveSequence);
            }
            else
            {
                conquerorSequence.Kill();
                expSliderSequence.Kill();
                expSliderMoveSequence.Kill();
            }

            sequenceAnimation.Append(rewardSequence);       //보상 시퀀스

            sequenceAnimation.SetUpdate(isIndependentUpdate: true);
            sequenceAnimation.SetAutoKill(false);
            sequenceAnimation.Play();
        }

        private void DisplayRewards(
            FirstClearRewardData? firstClearReward,
            long? incrementAccountExp,
            long? rewardGold,
            long? incrementGold,
            long? gainedGem,
            Dictionary<EquipmentSlot, long>? randomEquipmentTicketResults,
            long? mainHeroTicketResults,
            long? subHeroTicketResults,
            long? temporarySpaceCoins)
        {
            foreach (var exampleItem in _rewardItems)
            {
                exampleItem.transform.SetParent(null);
                exampleItem.gameObject.SetActive(false);
                ResourcePool.Instance.PutBackInstance(RewardItemCard.PREFAB_PATH, exampleItem.gameObject);
            }
            _rewardItems.Clear();

            // 첫 클리어 보상.
            if (firstClearReward != null)
            {
                // 캐릭터(히어로) 보상
                if (firstClearReward.FirstClearRewardHeroData != null)
                {
                    HeroData heroData = firstClearReward.FirstClearRewardHeroData;
                    var rewardItem = ResourcePool.Instance.InstantiateFromResource<RewardItemCard>(RewardItemCard.PREFAB_PATH);
                    rewardItem.InitializeForCharacter(heroData.HeroType, heroData.Grade, amount: 1);
                    _rewardItems.Add(rewardItem);
                }

                // 장비 보상
                if (firstClearReward.FirstClearRewardEquipments.Count > 0)
                {
                    foreach (var equipment in firstClearReward.FirstClearRewardEquipments)
                    {
                        var rewardItem = ResourcePool.Instance.InstantiateFromResource<RewardItemCard>(RewardItemCard.PREFAB_PATH);
                        rewardItem.InitializeForEquipment(equipment.EquipmentId, equipment.Grade, amount: 1);
                        _rewardItems.Add(rewardItem);
                    }
                }
            }

            _incrementAccountExp = incrementAccountExp.HasValue ? incrementAccountExp.Value : 0;

            _dropGold = incrementGold.HasValue ? (int)incrementGold.Value : 0;
            _clearGold = rewardGold.HasValue ? (int)rewardGold.Value : 0;

            // 주워먹은 보석
            if (gainedGem.HasValue && gainedGem.Value > 0)
            {
                var rewardItem = ResourcePool.Instance.InstantiateFromResource<RewardItemCard>(RewardItemCard.PREFAB_PATH);
                rewardItem.Initialize(RewardItemType.Gem, gainedGem.Value);
                _rewardItems.Add(rewardItem);
            }

            // 대표 캐릭터 강화 재료
            if (mainHeroTicketResults.HasValue && mainHeroTicketResults.Value > 0)
            {
                var rewardItem = ResourcePool.Instance.InstantiateFromResource<RewardItemCard>(RewardItemCard.PREFAB_PATH);
                rewardItem.Initialize(RewardItemType.MainCharacterTicket, mainHeroTicketResults.Value);
                _rewardItems.Add(rewardItem);
            }

            // 보조 캐릭터 강화 재료
            if (subHeroTicketResults.HasValue && subHeroTicketResults.Value > 0)
            {
                var rewardItem = ResourcePool.Instance.InstantiateFromResource<RewardItemCard>(RewardItemCard.PREFAB_PATH);
                rewardItem.Initialize(RewardItemType.SubCharacterTicket, subHeroTicketResults.Value);
                _rewardItems.Add(rewardItem);
            }

            // 임시 우주 코인
            if (temporarySpaceCoins.HasValue && temporarySpaceCoins.Value > 0)
            {
                var rewardItem = ResourcePool.Instance.InstantiateFromResource<RewardItemCard>(RewardItemCard.PREFAB_PATH);
                rewardItem.Initialize(RewardItemType.TemporarySpaceCoin, temporarySpaceCoins.Value);
                _rewardItems.Add(rewardItem);
            }

            // 랜덤 강화석 결과값
            if (randomEquipmentTicketResults != null)
            {
                foreach (var equipmentTicket in randomEquipmentTicketResults)
                {
                    if (equipmentTicket.Value > 0)
                    {
                        var rewardItem = ResourcePool.Instance.InstantiateFromResource<RewardItemCard>(RewardItemCard.PREFAB_PATH);
                        rewardItem.Initialize(RewardItemType.EquipmentTicket, equipmentTicket.Value);
                        _rewardItems.Add(rewardItem);
                    }
                }
            }

            foreach (var rewardItem in _rewardItems)
            {
                rewardItem.transform.SetParent(_rewardItemDisplayScroll.content);
                rewardItem.transform.localPosition = Vector3.zero;
                rewardItem.transform.localScale = Vector3.one;
                // 꺼뒀다가 연출하면서 킨다.
                rewardItem.gameObject.SetActive(false);
            }
        }

        private void SaveMainChapterResultToServer(
            ChapterStaticData chapter,
            StagePlayResult result,
            float playedStageTime,
            long gainedGolds,
            long gainedGems,
            long gainedRandomEquipmentElement,
            long eliminatedBosses,
            long eliminatedElites,
            long eliminatedMonsters)
        {
            _isOneMoreButtonEnable = GameClient.CS.UserGameData!.ClearedHighestChapter >= chapter.ChapterNumber;

            GameClient.CS.FinishChapter(
                new FinishChapterRequest(
                    chapter.ChapterNumber,
                    result,
                    playedStageTime,
                    gainedGolds,
                    gainedGems,
                    gainedRandomEquipmentElement,
                    eliminatedBosses,
                    eliminatedElites,
                    eliminatedMonsters),
                onCompleted: (FinishChapterResponse response) =>
                {
                    switch (response.ResultCode)
                    {
                        case FinishChapterResultCode.Success:
                            this.DisplayRewards(
                                response.FirstClearRewardData,
                                response.IncrementAccountExp,
                                response.RewardGold,        //챕터 보상 골드
                                response.IncrementGold,     //챕터에서 획득한 골드
                                response.GainedGem,
                                response.RandomEquipmentTicketResults,
                                response.RandomCharacterTicketResults,
                                null,
                                null);
                            if (result == StagePlayResult.Cleared)
                            {
                                this.PlaySuccessAnimation();
                            }
                            else
                            {
                                this.PlayFailAnimation();
                            }
                            _isServerRequestCompleted = true;
                            break;
                        case FinishChapterResultCode.InvalidSession:
                            UnityGlobal.HandleInvalidSessionInfo(nameof(FinishChapterRequest), GameClient.CS.ApplicationVersion, GameClient.CS.CachedAccountId);
                            break;
                        case FinishChapterResultCode.NotInStage:
                        case FinishChapterResultCode.NotClearedPreviousChapter:
                        default:
                            // 어뷰징 유저
                            UnityGlobal.HandleAbusingUser(nameof(FinishChapterRequest), GameClient.CS.ApplicationVersion, GameClient.CS.CachedAccountId);
                            break;
                    }
                }, UnityGlobal.HandleInternalServerError_LogAndRestart, UnityGlobal.HandleNetworkErrorAndContinueRetry);
        }

        private Image CreateFadeImage(Image targetImage)
        {
            GameObject fadeObject = new GameObject("FadeImageObj");
            fadeObject.layer = LayerMask.NameToLayer("UI");
            Image fadeImage = fadeObject.AddComponent<Image>();
            fadeImage.sprite = targetImage.sprite;
            fadeImage.material = ResourcePool.Instance.LoadResource<Material>("Commons/Reward/RewardItemCardFadeImageMat.mat");

            fadeImage.rectTransform.SetParent(targetImage.transform);
            fadeImage.rectTransform.anchoredPosition3D = Vector3.zero;
            fadeImage.rectTransform.localScale = Vector3.one;
            fadeImage.rectTransform.anchorMin = Vector2.zero;
            fadeImage.rectTransform.anchorMax = Vector2.one;
            fadeImage.rectTransform.offsetMin = Vector2.zero;
            fadeImage.rectTransform.offsetMax = Vector2.zero;

            return fadeImage;
        }
    }
}
