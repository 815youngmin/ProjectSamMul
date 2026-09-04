using DG.Tweening;
using Shared.DataTables;
using Shared.Localizers;
using Shared.StaticDatas;
using SamMul.Animations.Placeholder;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SamMul.GameClients;
using SamMul.ResourcePools;
using SamMul.UnityHelpers;

namespace SamMul.UIs.Lobbies.BattlePages
{
    public class BattlePage : MonoBehaviour
    {
        public BattlePageStageScroll BattlePageStageScroll => _battlePageStageScroll;
        public ZButton SelectButton => _selectButton;
        public ZButton BackButton => _backButton;

        [SerializeField] private BattlePageStageScroll _battlePageStageScroll;
        [SerializeField] private ZButton _selectButton;
        [SerializeField] private ZButton _backButton;
        [SerializeField] private TextMeshProUGUI _selectButtonText;
        [SerializeField] private TextMeshProUGUI _backButtonText;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private SkeletonGraphic _airshipUp;
        [SerializeField] private SkeletonGraphic _airshipBottom;

        [SerializeField] GameObject _stageInfo;
        [SerializeField] private TextMeshProUGUI _stageNameText;
        [SerializeField] private Image _stageElementImage;
        [SerializeField] private TextMeshProUGUI _stageRecordText;
        [SerializeField] private TextMeshProUGUI _chapterNumberText;

        private ChapterStaticData _selectedChapter;
        private int _clearedHighestChapterNumber;

        private Sequence _battlePageOpenSequence;
        private Sequence _battlePageCloseSequence;

        private Sequence _uiHideSequence;
        private Sequence _uiShowSequence;

        private Vector3 _airshipUpLocalPosition;
        private Vector3 _airshipUpScale;

        private Vector3 _airshipBottomLocalPosition;
        private Vector3 _airshipBottomScale;
        private Vector3 _battlePageStageScrollScale;

        public void Initialize(int clearedHighestChapterNumber, long highestStageTimeInSeconds, Action closeBattlePage, Action<int> setSelectedChapter)
        {
            Assert.IsNotNull(_battlePageStageScroll);
            Assert.IsNotNull(_selectButton);
            Assert.IsNotNull(_backButton);

            Assert.IsNotNull(_stageInfo);
            Assert.IsNotNull(_stageNameText);
            Assert.IsNotNull(_stageElementImage);
            Assert.IsNotNull(_stageRecordText);
            Assert.IsNotNull(_chapterNumberText);

            _clearedHighestChapterNumber = clearedHighestChapterNumber;
            _battlePageStageScroll.Initialize(clearedHighestChapterNumber, highestStageTimeInSeconds, UpdateBattlePageOnChapterScrollChanged, OnBeginDrag, OnDrag, OnEndDrag);

            _selectButtonText.text = Localizer.Instance.GetText("UI_CHAPTERLIST_SELECT_BUTTON");
            _backButtonText.text = Localizer.Instance.GetText("UI_CHAPTERLIST_BACK_BUTTON");

            _backButton.onClick.RemoveAllListeners();
            _backButton.onClick.AddListener(() =>
            {
                this.PlayCloseSequence();
            });

            _selectButton.onClick.RemoveAllListeners();
            _selectButton.onClick.AddListener(() =>
            {
                int selectedChapterNumber = _selectedChapter.ChapterNumber;
                if (_clearedHighestChapterNumber + 1 < selectedChapterNumber)
                {
                    UnityGlobal.Scenes.GetCurrentSceneUI().AddCommonMessagePopup(Localizer.Instance.GetText("UI_POPUP_NEED_CLEAR_PREVIOUS_CHAPTER_TITLE"), Localizer.Instance.GetText("UI_POPUP_NEED_CLEAR_PREVIOUS_CHAPTER_MESSAGE"), Localizer.Instance.GetText("UI_OK"), () => { });
                    return;
                }
                setSelectedChapter(selectedChapterNumber);
                this.PlayCloseSequence();
            });

            int currentSelectedCahpterNumber = _battlePageStageScroll.GetCurrentSelectedChapterNumber();
            _selectedChapter = StaticDataRepository.Instance.Chapters.FindChapter(currentSelectedCahpterNumber);
            Assert.IsNotNull(_selectedChapter);

            this.UpdateBattlePageInfo();


            _airshipUpLocalPosition = _airshipUp.rectTransform.anchoredPosition;
            _airshipUpScale = _airshipUp.rectTransform.localScale;
            _airshipBottomLocalPosition = _airshipBottom.rectTransform.anchoredPosition;
            _airshipBottomScale = _airshipBottom.rectTransform.localScale;
            _battlePageStageScrollScale = _battlePageStageScroll.transform.localScale;

            _chapterNumberText.transform.localScale = Vector3.zero;
            _stageInfo.transform.localScale = Vector3.zero;
            _stageRecordText.transform.localScale = Vector3.zero;
            _selectButton.transform.localScale = Vector3.zero;
            _backButton.transform.localScale = Vector3.zero;

            {
                _uiShowSequence = DOTween.Sequence();
                _uiShowSequence.Append(_chapterNumberText.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutQuad));
                _uiShowSequence.Join(_stageInfo.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutQuad));
                _uiShowSequence.Join(_stageRecordText.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutQuad));
                _uiShowSequence.Join(_selectButton.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutQuad));
                _uiShowSequence.Join(_backButton.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutQuad));
                _uiShowSequence.SetAutoKill(false);
                _uiShowSequence.SetRecyclable(true);
                _uiShowSequence.Pause();
            }

            {
                _uiHideSequence = DOTween.Sequence();
                _uiHideSequence.Append(_chapterNumberText.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InQuad));
                _uiHideSequence.Join(_stageInfo.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InQuad));
                _uiHideSequence.Join(_stageRecordText.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InQuad));
                _uiHideSequence.Join(_selectButton.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InQuad));
                _uiHideSequence.Join(_backButton.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InQuad));
                _uiHideSequence.SetAutoKill(false);
                _uiHideSequence.SetRecyclable(true);
                _uiHideSequence.Pause();
            }

            {
                _battlePageOpenSequence = DOTween.Sequence();
                _battlePageOpenSequence.Append(_airshipUp.rectTransform.DOAnchorPos(_airshipUpLocalPosition, 0.5f).SetEase(Ease.OutQuad));
                _battlePageOpenSequence.Join(_airshipBottom.rectTransform.DOAnchorPos(_airshipBottomLocalPosition, 0.5f).SetEase(Ease.OutQuad));
                _battlePageOpenSequence.Join(_airshipUp.transform.DOScale(_airshipUpScale, 0.5f).SetEase(Ease.OutQuad));
                _battlePageOpenSequence.Join(_airshipBottom.transform.DOScale(_airshipBottomScale, 0.5f).SetEase(Ease.OutQuad));
                _battlePageOpenSequence.Join(_battlePageStageScroll.transform.DOScale(_battlePageStageScrollScale, 0.5f).SetEase(Ease.OutQuad));
                _battlePageOpenSequence.AppendCallback(() =>
                {
                    this.PlayShowUISequence();
                });
                _battlePageOpenSequence.SetAutoKill(false);
                _battlePageOpenSequence.SetRecyclable(true);
                _battlePageOpenSequence.Pause();
            }

            {
                _battlePageCloseSequence = DOTween.Sequence();
                _battlePageCloseSequence.AppendCallback(() =>
                {
                    this.PlayHideUISequence();
                });
                _battlePageCloseSequence.Append(_airshipUp.rectTransform.DOAnchorPos(_airshipUpLocalPosition + new Vector3(0, 400f, 0), 0.5f).SetEase(Ease.InQuad));
                _battlePageCloseSequence.Join(_airshipBottom.rectTransform.DOAnchorPos(_airshipBottomLocalPosition - new Vector3(0, 400f, 0), 0.5f).SetEase(Ease.InQuad));
                _battlePageCloseSequence.Join(_airshipUp.transform.DOScale(Vector3.one * 1.5f, 0.5f).SetEase(Ease.InQuad));
                _battlePageCloseSequence.Join(_airshipBottom.transform.DOScale(Vector3.one * 1.5f, 0.5f).SetEase(Ease.InQuad));
                _battlePageCloseSequence.Join(_battlePageStageScroll.transform.DOScale(new Vector3(0.65f, 0.65f, 0.65f), 0.5f).SetEase(Ease.InQuad));
                _battlePageCloseSequence.OnComplete(() => { closeBattlePage(); });

                _battlePageCloseSequence.SetAutoKill(false);
                _battlePageCloseSequence.SetRecyclable(true);
                _battlePageCloseSequence.Pause();
            }

        }

        public void OnPageOpened(int selectedChapter)
        {
            _selectedChapter = StaticDataRepository.Instance.Chapters.FindChapter(selectedChapter);
            Assert.IsNotNull(_selectedChapter);
            _battlePageStageScroll.SetSelectedChapterNumber(selectedChapter);
            _backgroundImage.sprite = ResourcePool.Instance.LoadResource<Sprite>(_selectedChapter.ChapterBackgroundPath);

            this.PlayOpenSequence();
        }

        public void SetSelectedChapterAndTransition(int selectedChapter)
        {
            selectedChapter = Math.Clamp(selectedChapter, 1, GameClient.CS.ServiceFinalChapterNumber);
            if (_selectedChapter.ChapterNumber == selectedChapter)
            {
                return;
            }

            _selectedChapter = StaticDataRepository.Instance.Chapters.FindChapter(selectedChapter);
            Assert.IsNotNull(_selectedChapter);
            _battlePageStageScroll.SetSelectedChapterNumberAndPlayMoveSequence(selectedChapter);
        }


        // 챕터 스크롤이 스크롤되어 변경됨. 배틀페이지를 업데이트 함
        private void UpdateBattlePageOnChapterScrollChanged()
        {
            var selectedChapterNumber = _battlePageStageScroll.GetCurrentSelectedChapterNumber();
            if (_selectedChapter.ChapterNumber == selectedChapterNumber)
            {
                return;
            }

            _selectedChapter =  StaticDataRepository.Instance.Chapters.FindChapter(selectedChapterNumber);
            Assert.IsNotNull(_selectedChapter);

            {
                _backgroundImage.sprite = ResourcePool.Instance.LoadResource<Sprite>(_selectedChapter.ChapterBackgroundPath);
            }

        }

        private enum BattlePageScrollAnimationState
        {
            STOP,
            LEFT,
            RIGHT
        }

        private BattlePageScrollAnimationState _prevBattlePageScrollAnimationState = BattlePageScrollAnimationState.STOP;

        private void OnBeginDrag(PointerEventData eventData)
        {
            this.PlayHideUISequence();
        }

        private void OnDrag(PointerEventData eventData)
        {
            if (eventData.delta.x < 0)
            {
                if (_prevBattlePageScrollAnimationState != BattlePageScrollAnimationState.LEFT)
                {
                    _airshipUp.AnimationState.SetAnimation(0, "ui_u_start_l", false);
                    _airshipUp.AnimationState.AddAnimation(0, "ui_u_moving_l", false, 0f);

                    _airshipBottom.AnimationState.SetAnimation(0, "ui_d_start_l", false);
                    _airshipBottom.AnimationState.AddAnimation(0, "ui_d_moving_l", false, 0f);

                    UnityGlobal.Sounds.PlayBySoundPrefab("Sounds/SoundEffects/UIs/UISwipe_SFX.prefab", Vector3.zero);
                }
                _prevBattlePageScrollAnimationState = BattlePageScrollAnimationState.LEFT;
            }
            else if (eventData.delta.x > 0)
            {
                if (_prevBattlePageScrollAnimationState != BattlePageScrollAnimationState.RIGHT)
                {
                    _airshipUp.AnimationState.SetAnimation(0, "ui_u_start_r", false);
                    _airshipUp.AnimationState.AddAnimation(0, "ui_u_moving_r", false, 0f);

                    _airshipBottom.AnimationState.SetAnimation(0, "ui_d_start_r", false);
                    _airshipBottom.AnimationState.AddAnimation(0, "ui_d_moving_r", false, 0f);

                    UnityGlobal.Sounds.PlayBySoundPrefab("Sounds/SoundEffects/UIs/UISwipe_SFX.prefab", Vector3.zero);
                }
                _prevBattlePageScrollAnimationState = BattlePageScrollAnimationState.RIGHT;
            }
        }

        private void OnEndDrag(PointerEventData eventData)
        {
            this.PlayShowUISequence();
            if (_prevBattlePageScrollAnimationState == BattlePageScrollAnimationState.LEFT)
            {
                _airshipUp.AnimationState.SetAnimation(0, "ui_u_end_l", false);
                _airshipUp.AnimationState.AddAnimation(0, "ui_u_idle", true, 0f);

                _airshipBottom.AnimationState.SetAnimation(0, "ui_d_end_l", false);
                _airshipBottom.AnimationState.AddAnimation(0, "ui_d_idle", true, 0f);

            }
            else if (_prevBattlePageScrollAnimationState == BattlePageScrollAnimationState.RIGHT)
            {
                _airshipUp.AnimationState.SetAnimation(0, "ui_u_end_r", false);
                _airshipUp.AnimationState.AddAnimation(0, "ui_u_idle", true, 0f);

                _airshipBottom.AnimationState.SetAnimation(0, "ui_d_end_r", false);
                _airshipBottom.AnimationState.AddAnimation(0, "ui_d_idle", true, 0f);
            }
            _prevBattlePageScrollAnimationState = BattlePageScrollAnimationState.STOP;

        }

        private void UpdateBattlePageInfo()
        {
            Assert.IsNotNull(_stageNameText);
            Assert.IsNotNull(_stageRecordText);
            Assert.IsNotNull(_chapterNumberText);
            Assert.IsNotNull(_stageElementImage);

            _stageNameText.text = _battlePageStageScroll.StageNameText;
            _stageRecordText.text = _battlePageStageScroll.StageRecordText;
            _chapterNumberText.text = _battlePageStageScroll.ChapterNumberText;
            _stageElementImage.sprite = ResourcePool.Instance.LoadResource<Sprite>(_battlePageStageScroll.StageElementPath);
        }

        private void PlayOpenSequence()
        {
            _airshipUp.rectTransform.anchoredPosition = _airshipUpLocalPosition + new Vector3(0, 400f, 0);
            _airshipUp.transform.localScale = Vector3.one * 1.5f;
            _airshipBottom.rectTransform.anchoredPosition = _airshipBottomLocalPosition - new Vector3(0, 400f, 0);
            _airshipBottom.transform.localScale = Vector3.one * 1.5f;
            _battlePageStageScroll.transform.localScale = new Vector3(0.65f, 0.65f, 0.65f);

            _battlePageOpenSequence.Restart();
        }

        private void PlayCloseSequence()
        {
            _airshipUp.rectTransform.anchoredPosition = _airshipUpLocalPosition;
            _airshipUp.transform.localScale = _airshipUpScale;
            _airshipBottom.rectTransform.anchoredPosition = _airshipBottomLocalPosition;
            _airshipBottom.transform.localScale = _airshipBottomScale;
            _battlePageStageScroll.transform.localScale = _battlePageStageScrollScale;
            _battlePageCloseSequence.Restart();
        }

        private void PlayShowUISequence()
        {
            _uiHideSequence.Pause();

            this.UpdateBattlePageInfo();

            _chapterNumberText.transform.localScale = Vector3.zero;
            _stageInfo.transform.localScale = Vector3.zero;
            _stageRecordText.transform.localScale = Vector3.zero;
            _selectButton.transform.localScale = Vector3.zero;
            _backButton.transform.localScale = Vector3.zero;

            _uiShowSequence.Restart();
        }

        private void PlayHideUISequence()
        {
            _uiShowSequence.Pause();

            _chapterNumberText.transform.localScale = Vector3.one;
            _stageInfo.transform.localScale = Vector3.one;
            _stageRecordText.transform.localScale = Vector3.one;
            _selectButton.transform.localScale = Vector3.one;
            _backButton.transform.localScale = Vector3.one;

            _uiHideSequence.Restart();
        }
    }
}


