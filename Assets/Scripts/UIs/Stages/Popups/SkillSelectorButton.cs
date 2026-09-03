using DG.Tweening;
using Shared.DataTables;
using Shared.GameDataTypes;
using Shared.Localizers;
using Shared.StaticDatas;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Z.UIs.Commons;
using UnityEngine.UI;
using Z.GameClients;
using Z.GameClients.Stages;
using Z.GameClients.Stages.Characters.PCs;
using Z.ResourcePools;
using static Z.UIs.Stages.Popups.SkillSelectorPopup;

namespace Z.UIs.Stages.Popups
{
    //스킬 선택창 에서만 사용되지 않고 
    //스킬 박스 획득시에도 사용됩니다.
    //이름 변경이 필요합니다.
    public class SkillSelectorButton : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _skillNameText;
        [SerializeField] private TextMeshProUGUI _skillDescriptionText;
        [SerializeField] private TextMeshProUGUI _newText;

        [SerializeField] private GradeStarDisplayer _gradeStarDisplayer;
        [SerializeField] private GradeStarDisplayer _transcendStarDisplayer;

        [SerializeField] private Image _skillIcon;
        [SerializeField] private GameObject[] _transcendSkillBackgrounds;
        [SerializeField] private Image[] _transcendSkillIcons;
        [SerializeField] private GameObject _transcendConditionGroup;
        private Sequence[] _transcendSkillIconsSequences;

        [SerializeField] private Image _descriptionBox;
        [SerializeField] private Image _backgroundImage;
        public Image BackgroundImage => _backgroundImage;
        [SerializeField] private Image _symbolImage;
        [SerializeField] private Image _transcendConditionGroupBackground;

        [SerializeField] private TextMeshProUGUI _transcendConditionLabelText;

        [SerializeField] private ZButton _button; // button itself

        private SkillStaticData _skillStaticData;

        private static readonly string _activeBackgroundPath = "Stages/UIs/Popups/SkillSelectorPopup/ActiveButton.png";
        private static readonly string _passiveBackgroundPath = "Stages/UIs/Popups/SkillSelectorPopup/PassiveButton.png";
        private static readonly string _transcendBackgroundPath = "Stages/UIs/Popups/SkillSelectorPopup/TranscendButton.png";

        private static readonly string _goldBackgroundPath = "Stages/UIs/Popups/SkillSelectorPopup/GoldButton.png";
        private static readonly string _recoveryBackgroundPath = "Stages/UIs/Popups/SkillSelectorPopup/RecoveryButton.png";

        private static readonly string _activeSymbolPath = "Stages/UIs/Popups/SkillSelectorPopup/ActiveSymbol.png";
        private static readonly string _passiveSymbolPath = "Stages/UIs/Popups/SkillSelectorPopup/PassiveSymbol.png";
        private static readonly string _transcendSymbolPath = "Stages/UIs/Popups/SkillSelectorPopup/TranscendSymbol.png";

        private static readonly string _activeTranscendConditionGroupBackgroundPath = "Stages/UIs/Popups/SkillSelectorPopup/ActiveTranscendList.png";
        private static readonly string _passiveTranscendConditionGroupBackgroundPath = "Stages/UIs/Popups/SkillSelectorPopup/PassiveTranscendList.png";

        private static readonly Color32 _transcendSkillNameColor = new Color32(255, 220, 243, 255);
        private static readonly Color32 _passiveSkillNameColor = new Color32(255, 201, 153, 255);
        private static readonly Color32 _activeSkillNameColor = new Color32(143, 221, 253, 255);
        private static readonly Color32 _rewardNameColor = new Color32(238, 237, 226, 255);

        private static readonly Color32 _basicDescriptionTextColor = new Color32(66, 81, 92, 255);
        private static readonly Color32 _transcendDescriptionTextColor = new Color32(238, 237, 226, 255);
        private static readonly Color32 _rewardDescriptionTextColor = new Color32(238, 237, 226, 255);

        private static readonly string _goldIconPath = "Commons/SkillIcon/MaxLevel_Gold.png";
        private static readonly string _recoveryIconPath = "Commons/SkillIcon/MaxLevel_Heart.png";

        private const float _recoveryHPPercent = 0.25f;

        private void Awake()
        {
            _transcendSkillIconsSequences = new Sequence[_transcendSkillBackgrounds.Length];
            for (int i = 0; i < _transcendSkillBackgrounds.Length; ++i)
            {
                Transform target = _transcendSkillBackgrounds[i].transform;
                Sequence sequence = DOTween.Sequence();
                float scale = 1.1f;
                sequence.Append(target.DOScale(new Vector2(scale, scale), 0.2f));
                sequence.Append(target.DOScale(Vector2.one, 0.2f));
                sequence.Append(target.DOScale(new Vector2(scale, scale), 0.2f));
                sequence.Append(target.DOScale(Vector2.one, 0.2f));
                sequence.SetLoops(-1);
                sequence.SetUpdate(isIndependentUpdate: true);
                sequence.SetRecyclable(true);
                sequence.SetAutoKill(false);
                sequence.Pause();
                _transcendSkillIconsSequences[i] = sequence;
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _transcendSkillIconsSequences.Length; ++i)
            {
                _transcendSkillIconsSequences[i].Kill();
            }

            if (_gradeStarDisplayer != null)
            {
                _gradeStarDisplayer.StopAnimation();
            }
        }

        public void Initialize(PlayerCharacter owner, SkillStaticData skillStaticData, Action<bool> parentCloser)
        {
            _skillStaticData = skillStaticData;
            bool isTranscendentLevel = (GameConstants.SKILL_TRANSCENDENT_LEVEL == _skillStaticData.Level);
            //ButtonSetting
            {
                string backgroundPath;
                string symbolPath;
                string transcendConditionGroupBackgroundPath;
                Color skillNameTextColor;
                Color descriptionTextColor;

                switch (_skillStaticData.skillType)
                {
                    case SkillType.Active:
                        {
                            backgroundPath = isTranscendentLevel ? _transcendBackgroundPath : _activeBackgroundPath;
                            symbolPath = isTranscendentLevel ? _transcendSymbolPath : _activeSymbolPath;
                            transcendConditionGroupBackgroundPath = _activeTranscendConditionGroupBackgroundPath;
                            skillNameTextColor = isTranscendentLevel ? _transcendSkillNameColor : _activeSkillNameColor;
                            descriptionTextColor = isTranscendentLevel ? _transcendDescriptionTextColor : _basicDescriptionTextColor;
                        }
                        break;
                    case SkillType.Passive:
                        {
                            backgroundPath = _passiveBackgroundPath;
                            symbolPath = _passiveSymbolPath;
                            transcendConditionGroupBackgroundPath = _passiveTranscendConditionGroupBackgroundPath;
                            skillNameTextColor = _passiveSkillNameColor;
                            descriptionTextColor = _basicDescriptionTextColor;
                        }
                        break;
                    default:
                        {
                            throw new NotImplementedException("비정상적인 Type입니다. 확인해주세요.");
                        }
                }
                _backgroundImage.sprite = ResourcePool.Instance.LoadResource<Sprite>(backgroundPath);
                _symbolImage.sprite = ResourcePool.Instance.LoadResource<Sprite>(symbolPath);
                _symbolImage.gameObject.SetActive(true);

                _transcendConditionGroupBackground.sprite = ResourcePool.Instance.LoadResource<Sprite>(transcendConditionGroupBackgroundPath);
                _skillNameText.color = skillNameTextColor;
                _skillDescriptionText.color = descriptionTextColor;
            }

            // Skill Icon
            {
                _skillIcon.sprite = ResourcePool.Instance.LoadResource<Sprite>(_skillStaticData.IconResourcePath);
            }

            _skillNameText.text = _skillStaticData.Name;
            _skillDescriptionText.text = _skillStaticData.Description;

            _newText.gameObject.SetActive(1 == _skillStaticData.Level);

            {
                // Reroll때는 시퀀서가 죽지 않는다. 그러니 다시 Init시 멈추는 처리 한번 해준다.
                for (int i = 0; i < _transcendSkillIconsSequences.Length; ++i)
                {
                    _transcendSkillIconsSequences[i].Pause();
                }

                IReadOnlyList<SkillId> transcendTargets = _skillStaticData.TranscendTargets;
                if (0 != transcendTargets.Count)
                {
                    //일단 전부 비활성화 처리(초월 그룹, 초월 아이콘들)
                    _transcendConditionGroup.SetActive(false);
                    for (int i = 0; i < _transcendSkillIcons.Length; ++i)
                    {
                        _transcendSkillIcons[i].gameObject.SetActive(false);
                    }
                    for (int i = 0; i < _transcendSkillBackgrounds.Length; i++)
                    {
                        _transcendSkillBackgrounds[i].SetActive(false);
                    }

                    int transcendSkillIconMaxCount = _transcendSkillBackgrounds.Length;
                    int iconIndex = 0;
                    List<SkillId> sortedTranscendTargets = new List<SkillId>(transcendTargets.Count);
                    for (int i = 0; i < transcendTargets.Count; ++i)
                    {
                        if (!owner.IsSkillDeckContainsSkill(transcendTargets[i]))
                        {
                            continue;
                        }

                        var transcendSkillData = StaticDataRepository.Instance.Skills.Get(new SkillKey(transcendTargets[i], 1));
                        if (owner.HasSkill(transcendTargets[i]))
                        {
                            sortedTranscendTargets.Insert(0, transcendTargets[i]);
                            continue;
                        }
                        sortedTranscendTargets.Add(transcendTargets[i]);
                    }

                    for (int i = 0; i < sortedTranscendTargets.Count; ++i)
                    {
                        if (transcendSkillIconMaxCount <= iconIndex)
                        {
                            break;
                        }

                        var transcendSkillData = StaticDataRepository.Instance.Skills.Get(new SkillKey(sortedTranscendTargets[i], 1));
                        _transcendSkillIcons[iconIndex].sprite = ResourcePool.Instance.LoadResource<Sprite>(transcendSkillData.IconResourcePath);
                        _transcendSkillIcons[iconIndex].gameObject.SetActive(true);

                        if (owner.HasSkill(sortedTranscendTargets[i]) && 1 == _skillStaticData.Level)
                        {
                            _transcendSkillIconsSequences[iconIndex].Restart();
                        }

                        _transcendSkillBackgrounds[iconIndex].SetActive(true);
                        _transcendConditionGroup.SetActive(true);
                        iconIndex++;
                    }
                }
                else
                {
                    _transcendConditionGroup.SetActive(false);
                }
            }

            // Level Icon
            {
                if (isTranscendentLevel)
                {
                    _gradeStarDisplayer.gameObject.SetActive(false);
                    _transcendStarDisplayer.gameObject.SetActive(true);
                    _transcendStarDisplayer.InitializeForSkillSelector(_skillStaticData.Level);
                    _transcendStarDisplayer.PlayLastStarBlinkAnimation();
                }
                else
                {
                    _transcendStarDisplayer.gameObject.SetActive(false);
                    _gradeStarDisplayer.gameObject.SetActive(true);
                    _gradeStarDisplayer.InitializeForSkillSelector(_skillStaticData.Level);
                    _gradeStarDisplayer.PlayLastStarBlinkAnimation();
                }
            }

            _button.SetInteractable(true);
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() =>
            {
                var stage = GameClient.Stage;
                owner.AcquireOrUpgradeSkill(
                    _skillStaticData.Id,
                    owner,
                    stage);

                parentCloser(false);
            });

            _transcendConditionLabelText.text = Localizer.Instance.GetText("UI_TRANSCEND_CONDTION");
        }

        public void Initialize(PlayerCharacter owner, RewardType rewardType, Action<bool> parentCloser)
        {
            switch (rewardType)
            {
                case RewardType.Gold:
                    {
                        _backgroundImage.sprite = ResourcePool.Instance.LoadResource<Sprite>(_goldBackgroundPath);

                        var stage = GameClient.Stage;
                        long amount = stage.TakeLevelUpBonusGoldAmount();

                        _skillNameText.color = _rewardNameColor;
                        _skillNameText.text = Localizer.Instance.GetText("UI_GOLD");

                        _skillDescriptionText.color = _rewardDescriptionTextColor;
                        _skillDescriptionText.text = string.Format(Localizer.Instance.GetText("UI_GOLD_GET_MESSAGE"), amount);

                        _skillIcon.sprite = ResourcePool.Instance.LoadResource<Sprite>(_goldIconPath);

                        _button.SetInteractable(true);
                        _button.onClick.RemoveAllListeners();
                        _button.onClick.AddListener(() =>
                        {
                            owner.GainGold(amount);

                            parentCloser(false);
                        });
                    }
                    break;
                case RewardType.Meat:
                    {
                        _backgroundImage.sprite = ResourcePool.Instance.LoadResource<Sprite>(_recoveryBackgroundPath);

                        _skillNameText.color = _rewardNameColor;
                        _skillNameText.text = Localizer.Instance.GetText("UI_MEAT");

                        _skillDescriptionText.color = _rewardDescriptionTextColor;
                        _skillDescriptionText.text = Localizer.Instance.GetText("UI_MEAT_GET_MESSAGE");

                        _skillIcon.sprite = ResourcePool.Instance.LoadResource<Sprite>(_recoveryIconPath);

                        _button.SetInteractable(true);
                        _button.onClick.RemoveAllListeners();
                        _button.onClick.AddListener(() =>
                        {
                            var stage = GameClient.Stage;

                            owner.RecoverHP(stage, owner.MaxHP * _recoveryHPPercent);

                            parentCloser(false);
                        });
                    }
                    break;
                default:
                    Debug.LogError("구현되지 않은 보상 타입입니다. 확인이 필요합니다.");
                    break;
            }

            _newText.gameObject.SetActive(false);
            _gradeStarDisplayer.gameObject.SetActive(false);
            _transcendStarDisplayer.gameObject.SetActive(false);
            _transcendConditionGroup.SetActive(false);
            _symbolImage.gameObject.SetActive(false);
        }

        public void InvokeSelectEvent()
        {
            _button.onClick?.Invoke();
        }
    }
}
