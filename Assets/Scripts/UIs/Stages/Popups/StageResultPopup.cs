#nullable enable
using DG.Tweening;
using Shared.CSProtocols.ReqResData.Chapters;
using Shared.GameDataTypes;
using Shared.Localizers;
using Shared.StaticDatas;
using SamMul.Animations.Placeholder;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SamMul.GameClients;
using SamMul.Scenes;
using SamMul.UnityHelpers;

namespace SamMul.UIs.Stages.Popups
{
    public class StageResultPopup : BasePopup
    {
        //최상단 Victory 타이틀 애니메이션
        [Header("SuccessTitle")]
        [SerializeField] private TextMeshProUGUI _successTitle;

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

        //생존 시간
        [Header("SurvivalRecord")]
        [SerializeField] private Image _bestRecordLabel;
        [SerializeField] private TextMeshProUGUI _bestRecordLabelText;
        [SerializeField] private TextMeshProUGUI _bestRecordTimeText;

        [Header("ConfirmButton")]
        [SerializeField] private ZButton _confirmButton;

        [Header("OneMoreButton")]
        [SerializeField] private ZButton _oneMoreButton;
        [SerializeField] private TextMeshProUGUI _oneMoreButtonText;

        private string _chapterName = null!;

        private int _killCount = 0;

        private float _stageRunningtime;
        private long _previousBestRecordStagePlayTime;

        // 서버로 결과 종료 요청이 잘 처리되었는지 여부.
        private bool _isServerRequestCompleted;

        private bool _isOneMoreButtonEnable = false;

        private bool _showPreviousBestRecored;

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
                _previousBestRecordStagePlayTime = 0;
            }
            else
            {
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

            //생존시간 UI 비활성화
            {
                _bestRecordLabel.gameObject.SetActive(false);
                _bestRecordLabelText.gameObject.SetActive(false);
                _bestRecordTimeText.gameObject.SetActive(false);

                _bestRecordLabelText.text = Localizer.Instance.GetText("UI_STAGE_RESULT_POPUP_BEST_RECORD");
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
                    UnityGlobal.Scenes.ChangeTo(SceneType.Lobby, new LobbySceneInitialData(), "스테이지 종료");
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

                                var stageInitialData = StageSceneInitialData.CreateForMainChapter(
                                    chapter,
                                    userSkillDeck,
                                    userGameData.CreateSelectedHeroData(),
                                    userGameData.CreateSelectedEquipments());

                                UnityGlobal.Scenes.ChangeTo(SceneType.Stage, stageInitialData, "챕터 재도전");
                            }
                            break;
                        case EnterChapterResultCode.InvalidChapter:
                            {
                                UnityGlobal.Scenes.ChangeTo(SceneType.Lobby, new LobbySceneInitialData(), $"resultCode[{response.ResultCode}] 챕터 재도전 실패");
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
            Sequence sequenceAnimation = DOTween.Sequence();

            //스테이지 결과창 성공 타이틀 애니메이션
            Sequence successTitleSequence = DOTween.Sequence();
            successTitleSequence.AppendCallback(() =>
            {
                _successTitle.gameObject.SetActive(true);
            });
            successTitleSequence.AppendInterval(0.2f);

            //챕터 이름 활성화 시퀀스
            Sequence chapterNameSequence = DOTween.Sequence();
            chapterNameSequence.AppendCallback(() =>
            {
                _chapterNameLabel.gameObject.SetActive(true);
                _chapterNameLabel.color = new Color(1f, 1f, 1f, 0f);

                _chapterNameText.gameObject.SetActive(true);
                _chapterNameText.color = new Color(0f, 0f, 0f, 0f);
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
                _killCountText.color = new Color(0f, 0f, 0f, 0f);
                _killCountText.text = "0";
            });
            killCountSequence.Append(_killIconLabel.DOFade(1f, 0.2f));
            killCountSequence.Join(_killIconImage.DOFade(1f, 0.2f));
            killCountSequence.Append(_killCountText.DOCounter(0, (int)_killCount, 0.2f));
            killCountSequence.Join(_killCountText.DOFade(1f, 0.2f));

            sequenceAnimation.Append(successTitleSequence); //성공 그래픽 스파인 시퀀스
            sequenceAnimation.Append(chapterNameSequence);  //챕터 이름 활성화 시퀀스
            sequenceAnimation.Append(killCountSequence);    //킬카운트 활성화 시퀀스
            sequenceAnimation.Append(this.CreateButtonSequence());

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
                _chapterNameText.color = new Color(0f, 0f, 0f, 0f);
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
                _killCountText.color = new Color(0f, 0f, 0f, 0f);
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

            sequenceAnimation.Append(this.CreateButtonSequence());

            sequenceAnimation.SetUpdate(isIndependentUpdate: true);
            sequenceAnimation.SetAutoKill(false);
            sequenceAnimation.Play();
        }

        //확인 버튼(클리어한 메인 챕터 결과 요청시에는 한번더 버튼도 같이) 노출 시퀀스
        private Sequence CreateButtonSequence()
        {
            Sequence buttonSequence = DOTween.Sequence();
            if (_isOneMoreButtonEnable)
            {
                buttonSequence.AppendCallback(() =>
                {
                    _oneMoreButton.gameObject.SetActive(true);
                    _confirmButton.gameObject.SetActive(true);
                });
                buttonSequence.Append(_confirmButton.ButtonImage.DOFade(1f, 0.2f).From(0.0f));
                buttonSequence.Join(_confirmButton.ButtonText.DOFade(1f, 0.2f).From(0.0f));

                buttonSequence.Join(_oneMoreButton.ButtonImage.DOFade(1f, 0.2f).From(0.0f));
                buttonSequence.Join(_oneMoreButtonText.DOFade(1f, 0.2f).From(0.0f));
            }
            else
            {
                buttonSequence.AppendCallback(() => _confirmButton.gameObject.SetActive(true));
                buttonSequence.Append(_confirmButton.ButtonImage.DOFade(1f, 0.2f).From(0.0f));
                buttonSequence.Join(_confirmButton.ButtonText.DOFade(1f, 0.2f).From(0.0f));
            }
            return buttonSequence;
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
                        default:
                            // 어뷰징 유저
                            UnityGlobal.HandleAbusingUser(nameof(FinishChapterRequest), GameClient.CS.ApplicationVersion, GameClient.CS.CachedAccountId);
                            break;
                    }
                }, UnityGlobal.HandleInternalServerError_LogAndRestart, UnityGlobal.HandleNetworkErrorAndContinueRetry);
        }
    }
}
