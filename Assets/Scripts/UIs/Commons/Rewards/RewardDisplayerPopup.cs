#nullable enable
using DG.Tweening;
using Shared.GameDataTypes;
using Shared.Localizers;
using SamMul.Animations.Placeholder;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SamMul.ResourcePools;
using SamMul.UnityHelpers;

namespace SamMul.UIs.Commons.Rewards
{
    public class RewardDisplayerPopup : BasePopup
    {
        public static readonly string PREFAB_PATH = "Commons/Reward/RewardDisplayerPopup.prefab";
        public static readonly int MAX_DISPLAY_REWARD_COUNT = 24;
        private static readonly string TITLE_PARTICLE_PATH = "Commons/UIEffects/IconEffects/fx_flare.prefab";
        private static readonly string ITEM_PARTICLE_PATH = "Commons/UIEffects/IconEffects/fx_icon_wave.prefab";

        [SerializeField] private Button _blackBackgroundTouchScreen;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _messageText;

        [SerializeField] private RectTransform _displayerContents;
        [SerializeField] private GridLayoutGroup _contentLayoutGroup;
        [SerializeField] private List<RewardItemCard> _displayedItems;

        private readonly List<GameObject> _itemParticles = new List<GameObject>();
        private GameObject _titleParticle = null!;

        public void Initialize(IReadOnlyList<RewardItemData> acquiredItems, Action<bool> closeRequester)
        {
            InitializeBase(closeRequester);

            _titleText.text = Localizer.Instance.GetText("UI_REWARD_DISPLAYER_TITLE");
            _messageText.text = Localizer.Instance.GetText("UI_REWARD_DISPLAYER_MESSAGE");

            _titleParticle = ResourcePool.Instance.InstantiateFromResource(TITLE_PARTICLE_PATH);
            _titleParticle.transform.SetParent(_titleText.transform);
            _titleParticle.transform.localScale = Vector3.one * 2;
            _titleParticle.transform.localPosition = Vector3.zero;
            _titleParticle.GetComponent<SkeletonGraphic>().AnimationState.SetAnimation(0, "Gem_fx", false);

            _blackBackgroundTouchScreen.onClick.RemoveAllListeners();
            _blackBackgroundTouchScreen.onClick.AddListener(() =>
            {
                closeRequester(false);
            });

            foreach (var itemCard in _displayedItems)
            {
                itemCard.transform.SetParent(null);
                ResourcePool.Instance.PutBackInstance(RewardItemCard.PREFAB_PATH, itemCard.gameObject);
            }
            _displayedItems.Clear();

            float totalWidth = _contentLayoutGroup.padding.left + _contentLayoutGroup.padding.right;
            Sequence displaySequence = DOTween.Sequence(this.transform);

            int count = 0;
            foreach (var acquiredItem in acquiredItems)
            {
                if (acquiredItem.Amount <= 0)
                {
                    Debug.LogWarning($"RewardItemList 에 RewardItem.Amount[{acquiredItem.Amount}] 값이 잘못됨. 양수여야 함. 무시합니다.");
                    continue;
                }

                if (count >= MAX_DISPLAY_REWARD_COUNT)
                {
                    break;
                }
                ++count;

                var itemCard = ResourcePool.Instance.InstantiateFromResource<RewardItemCard>(RewardItemCard.PREFAB_PATH);
                var itemParticle = ResourcePool.Instance.InstantiateFromResource(ITEM_PARTICLE_PATH);

                switch (acquiredItem.RewardItemType)
                {
                    case RewardItemType.Gold:
                    case RewardItemType.Gem:
                    case RewardItemType.Exp:
                    case RewardItemType.AttendancePoint:
                    case RewardItemType.SpecialDNA:
                    case RewardItemType.BattlePassExp:
                    case RewardItemType.StarCandy:
                    case RewardItemType.ResurrectionCoin:
                    case RewardItemType.RocketFuel:
                    case RewardItemType.TemporarySpaceCoin:
                    case RewardItemType.PermanentSpaceCoin:
                    case RewardItemType.MainCharacterTicket:
                    case RewardItemType.SubCharacterTicket:
                    case RewardItemType.EquipmentTicket:
                        itemCard.Initialize(acquiredItem.RewardItemType, acquiredItem.Amount);
                        break;
                    case RewardItemType.Character:
                        itemCard.InitializeForCharacter(acquiredItem.CharacterType!.Value, acquiredItem.Grade!.Value, acquiredItem.Amount);
                        break;
                    case RewardItemType.Equipment:
                        itemCard.InitializeForEquipment(acquiredItem.EquipmentId!.Value, acquiredItem.Grade!.Value, acquiredItem.Amount);
                        break;
                    case RewardItemType.None:
                        break;
                    default:
                        throw new NotImplementedException($"{acquiredItem.RewardItemType}타입 RewardPopup 구현 안 됨");
                }

                itemCard.transform.SetParent(_displayerContents, false);
                itemCard.transform.localScale = Vector3.one;
                _displayedItems.Add(itemCard);

                itemParticle.transform.SetParent(itemCard.transform);
                itemParticle.transform.localPosition = Vector3.zero;
                itemParticle.transform.localScale = Vector3.one * 1.5f;
                itemParticle.SetActive(false);
                _itemParticles.Add(itemParticle);

                displaySequence.InsertCallback(count * 0.15f, () =>
                {
                    itemParticle.SetActive(true);
                    itemParticle.GetComponent<SkeletonGraphic>().AnimationState.SetAnimation(0, "Gem_fx2", true);
                    itemParticle.GetComponent<SkeletonGraphic>().AnimationState.AddAnimation(0, "Gem_fx", false, 0.12f);
                    itemParticle.GetComponent<SkeletonGraphic>().UnscaledTime = true;
                });
                displaySequence.Insert(count * 0.15f, itemCard.transform.DOScale(1.3f, 0.12f).From(0f));
                displaySequence.Insert(count * 0.15f, itemParticle.GetComponent<SkeletonGraphic>().DOFade(0.6f, 0.12f).From(0f));
                displaySequence.Insert(count * 0.15f + 0.12f, itemCard.transform.DOScale(1f, 0.08f));
                displaySequence.Insert(count * 0.15f + 0.12f, itemParticle.GetComponent<SkeletonGraphic>().DOFade(1f, 0));
                displaySequence.SetUpdate(isIndependentUpdate: true);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_displayerContents);

            UnityGlobal.Sounds.PlayBySoundPrefab("Sounds/SoundEffects/UIs/ItemIcon_SFX.prefab", Vector3.zero);
        }

        public override void Close(bool skipAnimation)
        {
            if (_titleParticle != null)
            {
                ResourcePool.Instance.PutBackInstance(TITLE_PARTICLE_PATH, _titleParticle);
                _titleParticle = null!;
            }

            foreach (var particle in _itemParticles)
            {
                particle.transform.SetParent(null);
                ResourcePool.Instance.PutBackInstance(ITEM_PARTICLE_PATH, particle);
            }
            _itemParticles.Clear();
            base.Close(skipAnimation);
        }
    }
}
