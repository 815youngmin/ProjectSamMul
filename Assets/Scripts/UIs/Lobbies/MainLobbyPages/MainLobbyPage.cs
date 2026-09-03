#nullable enable
using Shared.Localizers;
using Shared.StaticDatas;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SamMul.ResourcePools;
using SamMul.UIs.Lobbies.BattlePages;

namespace SamMul.UIs.Lobbies.MainLobbyPages
{
    public class MainLobbyPage : MonoBehaviour
    {
        public MainChapterStartButton MainChapterStartButton => _mainChapterStartButton;
        public StageSelectorGroup StageSelectorGroup => _stageSelectorGroup;
        public MenuButton MenuButton => _menuButton;

        [SerializeField] private StageSelectorGroup _stageSelectorGroup = null!;
        [SerializeField] private ZButton _nextStageSelectedButton = null!;
        [SerializeField] private ZButton _previousStageSelectedButton = null!;
        [SerializeField] private MenuButton _menuButton = null!;
        [SerializeField] private MainChapterStartButton _mainChapterStartButton = null!;
        [SerializeField] private Image _backgroundImage = null!;
        [SerializeField] private RewardShowGroup _rewardShowButton = null!;


        // 데모에서 제거된 콘텐트(습격, 미션, 랭킹 등)의 버튼들이 들어 있던 그룹. 자식 버튼들은 전부 숨긴다.
        [SerializeField] private RectTransform _leftSideButtonGroup = null!;
        [SerializeField] private RectTransform _rightSideButtonGroup = null!;

        public void Initialize(
            int selectedChapterNumber,
            int clearedHighestChapter,
            float highestStageTimeInSeconds,
            Action changeToBattlePage,
            Action<int> changeToBattlePageWithChapterTransition)
        {
            Debug.Assert(_stageSelectorGroup);
            Debug.Assert(_nextStageSelectedButton);
            Debug.Assert(_previousStageSelectedButton);
            Debug.Assert(_menuButton);

            ChapterStaticData selectedChapter = StaticDataRepository.Instance.Chapters.FindChapter(selectedChapterNumber)!;
            _stageSelectorGroup.Initialize(selectedChapter, clearedHighestChapter, highestStageTimeInSeconds, changeToBattlePage);
            _mainChapterStartButton.Initialize(selectedChapter);

            _nextStageSelectedButton.onClick.RemoveAllListeners();
            _nextStageSelectedButton.onClick.AddListener(() =>
            {
                changeToBattlePageWithChapterTransition.Invoke(selectedChapterNumber + 1);
            });

            _previousStageSelectedButton.onClick.RemoveAllListeners();
            _previousStageSelectedButton.onClick.AddListener(() =>
            {
                changeToBattlePageWithChapterTransition.Invoke(selectedChapterNumber - 1);
            });

            _menuButton.Initialize();

            _rewardShowButton.Initialize();
            ChapterStaticData? nearestSpecialRewardChapter = BattlePage.FindNearestSpecialRewardChapter(selectedChapterNumber, clearedHighestChapter);
            if (null != nearestSpecialRewardChapter && nearestSpecialRewardChapter.ChapterNumber == selectedChapterNumber)
            {
                _rewardShowButton.ShowSpecialReward(nearestSpecialRewardChapter);
            }
            else
            {
                _rewardShowButton.HideSpecialReward();
            }

            _backgroundImage.sprite = ResourcePool.Instance.LoadResource<Sprite>(selectedChapter!.ChapterBackgroundPath);


            HideChildren(_leftSideButtonGroup);
            HideChildren(_rightSideButtonGroup);
            this.UpdateSideButtonGroups();

            static void HideChildren(Transform parent)
            {
                foreach (Transform child in parent)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        public void UpdateSideButtonGroups()
        {
            bool isAnyChildActiveInLeftSideButtonGroup = IsAnyChildActiveIn(_leftSideButtonGroup);
            bool isAnyChildActiveInRightSideButtonGroup = IsAnyChildActiveIn(_rightSideButtonGroup);

            if (isAnyChildActiveInLeftSideButtonGroup || isAnyChildActiveInRightSideButtonGroup)
            {
                _leftSideButtonGroup.gameObject.SetActive(isAnyChildActiveInLeftSideButtonGroup);
                _rightSideButtonGroup.gameObject.SetActive(isAnyChildActiveInRightSideButtonGroup);
                _nextStageSelectedButton.gameObject.SetActive(false);
                _previousStageSelectedButton.gameObject.SetActive(false);
            }
            else
            {
                _leftSideButtonGroup.gameObject.SetActive(false);
                _rightSideButtonGroup.gameObject.SetActive(false);
                _nextStageSelectedButton.gameObject.SetActive(true);
                _previousStageSelectedButton.gameObject.SetActive(true);
            }

            static bool IsAnyChildActiveIn(Transform parent)
            {
                foreach (Transform child in parent)
                {
                    if (child.gameObject.activeSelf)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
    }
}
