#nullable enable
using Shared.DataTables;
using Shared.GameDataTypes;
using Shared.Localizers;
using Shared.StaticDatas;
using SamMul.Animations.Placeholder;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SamMul.GameClients;
using SamMul.ResourcePools;
using SamMul.UIs.Lobbies;
using SamMul.UnityHelpers;

namespace SamMul.UIs.Commons.Rewards
{
    public class RewardItemCard : MonoBehaviour
    {
        public static readonly string PREFAB_PATH = "Commons/Reward/RewardItemCard.prefab";

        // 이펙트 
        private static readonly string EFFECT_FRONT = "Commons/UIEffects/EquipmentCardEffects/fx_icon_f.prefab";
        private static readonly string EFFECT_BACK = "Commons/UIEffects/EquipmentCardEffects/fx_icon_b.prefab";

        // 배경
        private static readonly string NORMAL_BACKGROUND_SPRITE = "Commons/CardIcons/ClassD_Back.png";
        private static readonly string UNIQUE_BACKGROUND_SPRITE = "Commons/CardIcons/ClassSS_Back.png";
        private static readonly string GEM_BACKGROUND_SPRITE = "Commons/CardIcons/Gem_Back.png";
        private static readonly string GOLD_BACKGROUND_SPRITE = "Commons/CardIcons/Gold_Back.png";
        private static readonly string BATTLE_PASS_EXP_BACKGROUND_SPRITE = "Commons/CardIcons/BattlePassExp_Back.png";
        private static readonly string STAR_CANDY_BACKGROUND_SPRITE = "Commons/CardIcons/StarCandyBackground.png";
        private static readonly string RESURRECTION_COIN_BACKGROUND_SPRITE = "Commons/CardIcons/ResurrectionCoinBackground.png";
        private static readonly string ROCKET_FUEL_BACKGROUND_SPRITE = "Commons/CardIcons/Stamina_Back.png";
        private static readonly string TEMPORARY_SPACE_COIN_BACKGROUND_SPRITE = "Commons/CardIcons/Gold_Back.png";
        private static readonly string PERMANENT_SPACE_COIN_BACKGROUND_SPRITE = "Commons/CardIcons/Gem_Back.png";

        // 아이콘
        private static readonly string GOLD_ICON_SPRITE = "Commons/Icon/Gold_Icon.png";
        private static readonly string GEM_ICON_SPRITE = "Commons/Icon/Gem_Icon.png";
        private static readonly string EXP_ICON_SPRITE = "Commons/Reward/Reward_ExpIcon.png";
        private static readonly string ATTENDANCE_POINT_ICON_SPRITE = "Commons/ProductIcons/AttendancePointIcon.png";
        private static readonly string SPECIAL_DNA_ICON_SPRITE = "Commons/Icon/SpecialDNAIcon.png";
        private static readonly string EQUIPMENT_TICKETICON_SPRITE = "Commons/Icon/Equipment_TicketIcon_Random.png";
        private static readonly string MAIN_CHARACTER_TICKET_ICON_SPRITE = "Commons/Icon/CharacterTicketIcon.png";
        private static readonly string SUB_CHARACTER_TICKET_ICON_SPRITE = "Commons/Icon/SubCharacterTicketIcon.png";
        private static readonly string BATTLE_PASS_EXP_ICON_SPRITE = "Commons/Icon/BattlePassExpIcon.png";
        private static readonly string STAR_CANDY_ICON_SPRITE = "Commons/Icon/StarCandyIcon.png";
        private static readonly string RESURRECTION_COIN_ICON_SPRITE = "Commons/Icon/ResurrectionCoin_Icon.png";
        private static readonly string ROCKET_FUEL_ICON_SPRITE = "Commons/Icon/RocketFuelIcon.png";
        private static readonly string TEMPORARY_SPACE_COIN_ICON_SPRITE = "Commons/Icon/TemporarySpaceCoinIcon.png";
        private static readonly string PERMANENT_SPACE_COIN_ICON_SPRITE = "Commons/Icon/PermanentSpaceCoinIcon.png";

        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private Image _background;
        [SerializeField] private Image _icon;
        [SerializeField] private Image _slotBackground;
        [SerializeField] private Image _slotIcon;
        [SerializeField] private Image _rarityIcon;
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private ZButton _zButton;

        [SerializeField] private RectTransform _backEffectRectTransform;
        [SerializeField] private RectTransform _frontEffectRectTransform;
        private SkeletonGraphic? _backEffect;
        private SkeletonGraphic? _frontEffect;

        public RectTransform RectTransform => _rectTransform;
        public Image BackGround => _background;

        public ItemType ItemType => _itemType;
        private ItemType _itemType;

        public void MakeVisibleIcon()
        {
            var color = _icon.color;
            color.a = 1f;
            _icon.color = color;
        }

        public void Initialize(RewardItemType rewardItemType, long amount)
        {
            this.SetBackground(rewardItemType);
            this.SetIcon(rewardItemType);
            this.SetAmountText(amount, rewardItemType == RewardItemType.Gem);
            this.SetSlot(rewardItemType);
            this.ClearEquipmentGradeEffect();
            this.InitializeItemTypeAndBriefPopup(rewardItemType.ToItemType());
            _rarityIcon.enabled = false;
        }

        public void InitializeForCharacter(HeroType character, Grade grade, long amount)
        {
            this.MakeVisibleIcon();
            this.SetBackgroundByGrade(grade, isEquipment: false);
            this.SetSlot(RewardItemType.Character);
            this.ClearEquipmentGradeEffect();
            var characterImagePath = StaticDataRepository.Instance.CharacterImagePaths.Get(character).DatachipIconPath;
            _icon.sprite = ResourcePool.Instance.LoadResource<Sprite>(characterImagePath);
            this.SetAmountText(amount);
            this.InitializeItemTypeAndBriefPopup(character.ToItemType());
            _rarityIcon.enabled = false;
        }

        public void InitializeForEquipment(EquipmentId equipment, Grade grade, long amount)
        {
            this.MakeVisibleIcon();
            var equipmentStaticData = StaticDataRepository.Instance.Equipments.Get(equipment);
            this.SetBackgroundByGrade(grade, isEquipment: true);
            this.SetSlot(RewardItemType.Equipment);
            this.ClearEquipmentGradeEffect();
            _icon.sprite = ResourcePool.Instance.LoadResource<Sprite>(equipmentStaticData.IconPath);
            _slotIcon.sprite = ResourcePool.Instance.LoadResource<Sprite>(equipmentStaticData.Slot.IconPath());
            _rarityIcon.enabled = equipmentStaticData.Rarity == Rarity.Special;

            switch (grade)
            {
                case Grade.A:
                case Grade.A1:
                case Grade.A2:
                    {
                        var rectTransform = this.GetComponent<RectTransform>();
                        float effectScale = rectTransform.rect.width / 190f;

                        _backEffect = ResourcePool.Instance.InstantiateFromResource<SkeletonGraphic>(EFFECT_BACK);
                        _backEffect.AnimationState.SetAnimation(0, "Epic", true);
                        _backEffect.transform.SetParent(_backEffectRectTransform);
                        _backEffect.transform.localPosition = Vector3.zero;
                        _backEffect.transform.localScale = Vector3.one * effectScale;

                        _frontEffect = ResourcePool.Instance.InstantiateFromResource<SkeletonGraphic>(EFFECT_FRONT);
                        _frontEffect.AnimationState.SetAnimation(0, "Epic", true);
                        _frontEffect.transform.SetParent(_frontEffectRectTransform);
                        _frontEffect.transform.localPosition = Vector3.zero;
                        _frontEffect.transform.localScale = Vector3.one * effectScale;
                    }
                    break;
                case Grade.S:
                case Grade.S1:
                case Grade.S2:
                    {
                        var rectTransform = this.GetComponent<RectTransform>();
                        float effectScale = rectTransform.rect.width / 190f;

                        _backEffect = ResourcePool.Instance.InstantiateFromResource<SkeletonGraphic>(EFFECT_BACK);
                        _backEffect.AnimationState.SetAnimation(0, "Legendary", true);
                        _backEffect.transform.SetParent(_backEffectRectTransform);
                        _backEffect.transform.localPosition = Vector3.zero;
                        _backEffect.transform.localScale = Vector3.one * effectScale;

                        _frontEffect = ResourcePool.Instance.InstantiateFromResource<SkeletonGraphic>(EFFECT_FRONT);
                        _frontEffect.AnimationState.SetAnimation(0, "Legendary", true);
                        _frontEffect.transform.SetParent(_frontEffectRectTransform);
                        _frontEffect.transform.localPosition = Vector3.zero;
                        _frontEffect.transform.localScale = Vector3.one * effectScale;
                    }
                    break;
                case Grade.S3:
                case Grade.SS:
                case Grade.SS1:
                case Grade.SS2:
                case Grade.SS3:
                    {
                        var rectTransform = this.GetComponent<RectTransform>();
                        float effectScale = rectTransform.rect.width / 190f;

                        _backEffect = ResourcePool.Instance.InstantiateFromResource<SkeletonGraphic>(EFFECT_BACK);
                        _backEffect.AnimationState.SetAnimation(0, "Unique", true);
                        _backEffect.transform.SetParent(_backEffectRectTransform);
                        _backEffect.transform.localPosition = Vector3.zero;
                        _backEffect.transform.localScale = Vector3.one * effectScale;

                        _frontEffect = ResourcePool.Instance.InstantiateFromResource<SkeletonGraphic>(EFFECT_FRONT);
                        _frontEffect.AnimationState.SetAnimation(0, "Unique", true);
                        _frontEffect.transform.SetParent(_frontEffectRectTransform);
                        _frontEffect.transform.localPosition = Vector3.zero;
                        _frontEffect.transform.localScale = Vector3.one * effectScale;
                    }
                    break;
                default:
                    break;
            }

            this.SetAmountText(amount);
            this.InitializeItemTypeAndBriefPopup(equipment.ToItemType());
        }

        private void SetAmountText(long amount) => this.SetAmountText(amount, isGem: false);

        private void SetAmountText(long amount, bool isGem)
        {
            if (amount > 1)
            {
                _text.text = amount.ToShortNumericText(isGem ? GameConstants.GEM_THRESHOLD_TO_SHORTEN : 0);
                _text.gameObject.SetActive(true);
            }
            else
            {
                _text.gameObject.SetActive(false);
            }
        }

        private void SetBackground(RewardItemType rewardItemType)
        {
            _background.sprite = rewardItemType switch
            {
                RewardItemType.Gold => ResourcePool.Instance.LoadResource<Sprite>(GOLD_BACKGROUND_SPRITE),
                RewardItemType.Gem => ResourcePool.Instance.LoadResource<Sprite>(GEM_BACKGROUND_SPRITE),
                RewardItemType.AttendancePoint => null,
                RewardItemType.SpecialDNA => ResourcePool.Instance.LoadResource<Sprite>(UNIQUE_BACKGROUND_SPRITE),
                RewardItemType.BattlePassExp => ResourcePool.Instance.LoadResource<Sprite>(BATTLE_PASS_EXP_BACKGROUND_SPRITE),
                RewardItemType.StarCandy => ResourcePool.Instance.LoadResource<Sprite>(STAR_CANDY_BACKGROUND_SPRITE),
                RewardItemType.ResurrectionCoin => ResourcePool.Instance.LoadResource<Sprite>(RESURRECTION_COIN_BACKGROUND_SPRITE),
                RewardItemType.RocketFuel => ResourcePool.Instance.LoadResource<Sprite>(ROCKET_FUEL_BACKGROUND_SPRITE),
                RewardItemType.TemporarySpaceCoin => ResourcePool.Instance.LoadResource<Sprite>(TEMPORARY_SPACE_COIN_BACKGROUND_SPRITE),
                RewardItemType.PermanentSpaceCoin => ResourcePool.Instance.LoadResource<Sprite>(PERMANENT_SPACE_COIN_BACKGROUND_SPRITE),
                _ => ResourcePool.Instance.LoadResource<Sprite>(NORMAL_BACKGROUND_SPRITE),
            };

            _background.enabled = (_background.sprite != null);
            _slotBackground.enabled = false;
        }

        private void SetBackgroundByGrade(Grade grade, bool isEquipment)
        {
            // 등급별 배경 경로 확장(Grade.IconBackgroundPath / SlotBackgroundPath)은 데모에 없으므로
            // 세부 등급(A1, S2 ...)을 대표 등급(A, S ...)으로 줄여 카드 배경 경로를 만든다. 슬롯 배경은 프리팹의 기본 스프라이트를 그대로 쓴다.
            string gradeName = grade.ToString().TrimEnd('1', '2', '3');
            _background.sprite = ResourcePool.Instance.LoadResource<Sprite>($"Commons/CardIcons/Class{gradeName}_Back.png");
            _background.enabled = true;

            _slotBackground.enabled = isEquipment;
        }

        private void SetIcon(RewardItemType rewardItemType)
        {
            _icon.sprite = rewardItemType switch
            {
                RewardItemType.Gold => ResourcePool.Instance.LoadResource<Sprite>(GOLD_ICON_SPRITE),
                RewardItemType.Exp => ResourcePool.Instance.LoadResource<Sprite>(EXP_ICON_SPRITE),
                RewardItemType.Gem => ResourcePool.Instance.LoadResource<Sprite>(GEM_ICON_SPRITE),
                RewardItemType.AttendancePoint => ResourcePool.Instance.LoadResource<Sprite>(ATTENDANCE_POINT_ICON_SPRITE),
                RewardItemType.SpecialDNA => ResourcePool.Instance.LoadResource<Sprite>(SPECIAL_DNA_ICON_SPRITE),
                RewardItemType.MainCharacterTicket => ResourcePool.Instance.LoadResource<Sprite>(MAIN_CHARACTER_TICKET_ICON_SPRITE),
                RewardItemType.SubCharacterTicket => ResourcePool.Instance.LoadResource<Sprite>(SUB_CHARACTER_TICKET_ICON_SPRITE),
                RewardItemType.EquipmentTicket => ResourcePool.Instance.LoadResource<Sprite>(EQUIPMENT_TICKETICON_SPRITE),
                RewardItemType.BattlePassExp => ResourcePool.Instance.LoadResource<Sprite>(BATTLE_PASS_EXP_ICON_SPRITE),
                RewardItemType.StarCandy => ResourcePool.Instance.LoadResource<Sprite>(STAR_CANDY_ICON_SPRITE),
                RewardItemType.ResurrectionCoin => ResourcePool.Instance.LoadResource<Sprite>(RESURRECTION_COIN_ICON_SPRITE),
                RewardItemType.RocketFuel => ResourcePool.Instance.LoadResource<Sprite>(ROCKET_FUEL_ICON_SPRITE),
                RewardItemType.TemporarySpaceCoin => ResourcePool.Instance.LoadResource<Sprite>(TEMPORARY_SPACE_COIN_ICON_SPRITE),
                RewardItemType.PermanentSpaceCoin => ResourcePool.Instance.LoadResource<Sprite>(PERMANENT_SPACE_COIN_ICON_SPRITE),
                _ => null
            };
            this.MakeVisibleIcon();
        }

        private void SetSlot(RewardItemType rewardItemType)
        {
            switch (rewardItemType)
            {
                case RewardItemType.Equipment:
                    _slotBackground.enabled = true;
                    _slotIcon.enabled = true;
                    break;
                default:
                    _slotBackground.enabled = false;
                    _slotIcon.enabled = false;
                    break;
            }
        }


        private void ClearEquipmentGradeEffect()
        {
            if (_frontEffect != null)
            {
                ResourcePool.Instance.PutBackInstance(EFFECT_FRONT, _frontEffect.gameObject);
                _frontEffect = null;
            }

            if (_backEffect != null)
            {
                ResourcePool.Instance.PutBackInstance(EFFECT_BACK, _backEffect.gameObject);
                _backEffect = null;
            }
        }

        private void InitializeItemTypeAndBriefPopup(ItemType itemType)
        {
            _itemType = itemType;

            _zButton.SetInteractable(true);
            _zButton.onClick.RemoveAllListeners();
            _zButton.onClick.AddListener(() =>
            {
                BriefPopup.AddBriefPopupToUIRoot(itemType, _rectTransform);
            });
        }

    }
}

