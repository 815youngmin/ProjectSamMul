using Shared.GameDataTypes;
using Shared.Localizers;
using Shared.StaticDatas;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using SamMul.GameClients.Stages;
using SamMul.GameClients.Stages.Characters.PCs;
using SamMul.ResourcePools;
using SamMul.UnityHelpers;
using Random = UnityEngine.Random;

namespace SamMul.UIs.Stages.Popups
{
    public class SkillSelectorPopup : BasePopup
    {
        public enum RewardType { Gold, Meat }

        public static readonly string PREFAB_PATH = "Stage/UIs/SkillSelectorPopup/SkillSelectorPopup.prefab";

        [SerializeField] private RectTransform _skillButtonGroup;
        [SerializeField] private List<SkillSelectorButton> _skillButtons;

        [SerializeField] private AcquiredSkillGroup _acquiredSkillGroup;
        [SerializeField] private ZButton _skillRefreshButton;
        //스티커 이미지 생성위치
        [SerializeField] private RectTransform _backgroundStickerTransform;

        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _levelShadowText;
        [SerializeField] private TextMeshProUGUI _levelupText;
        [SerializeField] private TextMeshProUGUI _selectSkilllText;
        [SerializeField] private TextMeshProUGUI _refeshButtonText;


        private List<SkillKey[]> _skillCandidates;
        private int _currentRefreshCount;

        private void Initialize(HeroType heroType, int level, Action<bool> closeRequester)
        {
            Debug.Assert(_skillButtons.Count == 3);
            base.InitializeBase(closeRequester);

            _levelText.gameObject.SetActive(true);
            _levelText.text = level.ToString();
            _levelShadowText.gameObject.SetActive(true);
            _levelShadowText.text = level.ToString();
            _levelupText.gameObject.SetActive(level > 1);
            _levelupText.text = Localizer.Instance.GetText("UI_SKILL_SELECT_LEVELUP");
        }

        public void InitializeForSkills(Stage stage, PlayerCharacter owner, List<SkillKey[]> skillCandidates, SkillKey[] acquiredSkills, Action<bool> closeRequester)
        {
            this.Initialize(owner.StaticData.HeroType, owner.Level, closeRequester);

            _skillCandidates = skillCandidates;
            _currentRefreshCount = 0;
            this.InitializeSkillButtons(stage, owner, _skillCandidates[_currentRefreshCount]);

            _acquiredSkillGroup.Initialize(acquiredSkills);
            _acquiredSkillGroup.PlayAcquiredAnimationRelatedToSkillCandidates(_skillCandidates[_currentRefreshCount]);
            if (owner != null && _skillCandidates[_currentRefreshCount].Length > 2)
            {
                _skillRefreshButton.gameObject.SetActive(owner.IsCanSelectSkillRefesh);
            }
            else
            {
                _skillRefreshButton.gameObject.SetActive(false);
            }

            _skillRefreshButton.onClick.RemoveAllListeners();
            _skillRefreshButton.onClick.AddListener(() =>
            {
                if (false == _skillRefreshButton.gameObject.activeSelf)
                {
                    return;
                }
                _currentRefreshCount++;
                this.InitializeSkillButtons(stage, owner, _skillCandidates[_currentRefreshCount]);
                _acquiredSkillGroup.PlayAcquiredAnimationRelatedToSkillCandidates(_skillCandidates[_currentRefreshCount]);
                owner.SetUsedSelectSkillRefesh();
                _skillRefreshButton.gameObject.SetActive(owner.IsCanSelectSkillRefesh);

            });
            _skillRefreshButton.transform.DOButtonInitialTween(delay: 0.11f, scaleUpOffset: new Vector3(0.1f, 0.1f, 0.1f));

            _selectSkilllText.gameObject.SetActive(true);
            _selectSkilllText.text = Localizer.Instance.GetText("UI_SKILL_SELECT");
            _refeshButtonText.gameObject.SetActive(true);
            _refeshButtonText.text = Localizer.Instance.GetText("UI_REFRESH_BUTTON");
        }


        public override void Close(bool skipAnimation)
        {
            foreach (var skillButton in _skillButtons)
            {
                skillButton.gameObject.SetActive(false);
            }
            base.Close(skipAnimation);
        }

        private void InitializeSkillButtons(Stage stage, PlayerCharacter owner, SkillKey[] skillCandidates)
        {
            // 획득 가능한 스킬이 없으면 골드와 고기를 띄워준다.
            if (skillCandidates.Length <= 0)
            {
                _skillButtons[0].Initialize(owner, RewardType.Meat, parentCloser: this.Close);
                _skillButtons[0].gameObject.SetActive(true);

                if (stage.IsAbleToGiveLevelUpBonusGold())
                {
                    _skillButtons[1].Initialize(owner, RewardType.Gold, parentCloser: this.Close);
                    _skillButtons[1].gameObject.SetActive(true);
                }
                else
                {
                    _skillButtons[1].gameObject.SetActive(false);
                }

                _skillButtons[2].gameObject.SetActive(false);
                return;
            }

            for (int i = 0; i < _skillButtons.Count; ++i)
            {
                if (i < skillCandidates.Length)
                {
                    var skillKey = skillCandidates[i];
                    var skillStaticData = StaticDataRepository.Instance.Skills.Get(skillKey);
                    _skillButtons[i].Initialize(owner, skillStaticData, parentCloser: this.Close);
                    _skillButtons[i].gameObject.SetActive(true);
                }
                else
                {
                    _skillButtons[i].gameObject.SetActive(false);
                }
            }

            _skillButtons[0].transform.DOButtonInitialTween(delay: 0.21f, scaleUpOffset: new Vector3(0.06f, 0.06f, 0.06f));
            _skillButtons[1].transform.DOButtonInitialTween(delay: 0.24f, scaleUpOffset: new Vector3(0.06f, 0.06f, 0.06f));
            _skillButtons[2].transform.DOButtonInitialTween(delay: 0.27f, scaleUpOffset: new Vector3(0.06f, 0.06f, 0.06f));
        }

    }
}
