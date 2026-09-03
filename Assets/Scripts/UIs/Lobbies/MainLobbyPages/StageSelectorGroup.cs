# nullable enable
using Shared.Localizers;
using Shared.StaticDatas;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SamMul.ResourcePools;
using SamMul.UIs.Lobbies.BattlePages;
using SamMul.UnityHelpers;

namespace SamMul.UIs.Lobbies.MainLobbyPages
{
    public class StageSelectorGroup : MonoBehaviour
    {
        [SerializeField] private Button _combatButton;
        [SerializeField] private BattlePageStageScrollIcon _battlePageStageScrollIcon;
        [SerializeField] private TextMeshProUGUI _chapterNumber;
        [SerializeField] private TextMeshProUGUI _stageName;
        [SerializeField] private TextMeshProUGUI _clearTimeText;
        [SerializeField] private Image _stageElement;
        [SerializeField] private Image _clearedStateImage;

        public void Initialize(ChapterStaticData chapterStaticData, int clearedHighestChpater, float highestStageTimeInSeconds, Action changeToBattlePage)
        {
            _combatButton.onClick.RemoveAllListeners();
            _combatButton.onClick.AddListener(() =>
            {
                changeToBattlePage();
                UnityGlobal.Sounds.PlayBySoundPrefab("Sounds/SoundEffects/UIs/UISwipe_SFX.prefab", Vector3.zero);
            });

            _stageElement.sprite = ResourcePool.Instance.LoadResource<Sprite>(chapterStaticData.ElementType.IconPath());
            _battlePageStageScrollIcon.Initialize(chapterStaticData.ChapterNumber, show: true);

            _stageName.text = chapterStaticData.ChapterName;
            _chapterNumber.text = string.Format(Localizer.Instance.GetText("UI_CHAPTER_NUMBER_LABEL"), chapterStaticData.ChapterNumber);

            // ClearTime
            {
                if (chapterStaticData.ChapterNumber <= clearedHighestChpater)
                {
                    _clearTimeText.text = Localizer.Instance.GetText("UI_CLEAR");
                    _clearedStateImage.enabled = true;
                }
                else
                {
                    _clearedStateImage.enabled = false;

                    int minutes = (int)(highestStageTimeInSeconds / 60.0f);
                    int seconds = (int)(highestStageTimeInSeconds % 60.0f);
                    string message = string.Format(Localizer.Instance.GetText("UI_BEST_RECORD"), minutes, seconds);
                    int subIndex = message.IndexOf(':');
                    _clearTimeText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(Color.white)}>{message.Substring(0, subIndex + 1)}</color>{message.Substring(subIndex + 1)}";
                }
            }
        }
    }
}
