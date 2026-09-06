#nullable enable
using System;
using System.Collections.Generic;
using DG.Tweening;
using Shared.GameDataTypes;
using Shared.Localizers;
using Shared.StaticDatas;
using TMPro;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.PCs;
using SamMul.ResourcePools;

namespace SamMul.UIs.Stages.Popups
{
    /// <summary>
    /// 스킬 상자를 열었을 때 습득한 스킬을 화면 중앙에 한 줄로 펼쳐 보여주는 팝업.
    /// 습득 자체는 팝업이 열리기 전에 <see cref="PlayerCharacter"/>가 처리하고, 이 팝업은 결과만 연출한다.
    /// 슬롯이 하나씩 튀어나온 뒤 잠시 머물다 저절로 닫히며, 화면을 누르면 바로 닫힌다.
    /// </summary>
    public class SkillBoxPopup : BasePopup
    {
        public static readonly string PREFAB_PATH = "Stage/UIs/SkillBoxPopup/SkillBoxPopup.prefab";
        private static readonly string SLOT_PREFAB_PATH = "Stage/UIs/AcquiredSkillSlot.prefab";

        private const float SLOT_SPACING = 190f;
        private const float REVEAL_INTERVAL = 0.12f;
        private const float HOLD_DURATION = 1.5f;

        [SerializeField] private TextMeshProUGUI _titleText = null!;
        [SerializeField] private RectTransform _slotContainer = null!;

        private readonly List<AcquiredSkillSlot> _slots = new List<AcquiredSkillSlot>();
        private Sequence? _sequence;
        private bool _isRevealed;

        public void Initialize(SkillKey[] acquiredSkills, Action<bool> closeRequester)
        {
            base.InitializeBase(closeRequester);
            _isRevealed = false;

            _titleText.text = Localizer.Instance.GetText("UI_SKILLBOX_POPUP_TITLE");
            _titleText.rectTransform.anchorMin = _titleText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _titleText.rectTransform.anchoredPosition = new Vector2(0f, 160f);

            _slotContainer.anchorMin = _slotContainer.anchorMax = new Vector2(0.5f, 0.5f);
            _slotContainer.anchoredPosition = new Vector2(0f, -20f);

            _sequence = DOTween.Sequence(this)
                .Append(_titleText.transform.DOScale(1f, 0.25f).From(0f).SetEase(Ease.OutBack));

            // 가운데를 기준으로 좌우로 펼친다.
            for (int i = 0; i < acquiredSkills.Length; ++i)
            {
                var slot = this.CreateSlot(acquiredSkills[i], x: (i - (acquiredSkills.Length - 1) * 0.5f) * SLOT_SPACING);
                float at = 0.25f + i * REVEAL_INTERVAL;
                _sequence.Insert(at, slot.transform.DOScale(1.15f, 0.18f).From(0f).SetEase(Ease.OutBack));
                _sequence.Insert(at + 0.18f, slot.transform.DOScale(1f, 0.08f));
            }

            _sequence.AppendCallback(() => _isRevealed = true)
                .AppendInterval(HOLD_DURATION)
                .AppendCallback(() => this.Close(skipAnimation: false))
                .SetUpdate(isIndependentUpdate: true);
        }

        private AcquiredSkillSlot CreateSlot(SkillKey skillKey, float x)
        {
            var slot = ResourcePool.Instance.InstantiateFromResource<AcquiredSkillSlot>(SLOT_PREFAB_PATH);
            slot.transform.SetParent(_slotContainer, false);
            var rect = (RectTransform)slot.transform;
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.localScale = Vector3.zero;
            slot.Initialize(skillKey);

            var skill = StaticDataRepository.Instance.Skills.Get(skillKey);
            var label = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            label.rectTransform.SetParent(rect, false);
            label.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            label.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            label.rectTransform.pivot = new Vector2(0.5f, 1f);
            label.rectTransform.anchoredPosition = new Vector2(0f, -8f);
            label.rectTransform.sizeDelta = new Vector2(SLOT_SPACING - 10f, 80f);
            label.text = $"{skill.Name}\nLv.{skillKey.Level}";
            label.fontSize = 26f;
            label.alignment = TextAlignmentOptions.Top;

            _slots.Add(slot);
            return slot;
        }

        private void Update()
        {
            if (_isRevealed && Input.GetMouseButtonDown(0))
            {
                this.Close(skipAnimation: false);
            }
        }

        public override void Close(bool skipAnimation)
        {
            _sequence?.Kill();
            _sequence = null;
            _isRevealed = false;

            foreach (var slot in _slots)
            {
                var label = slot.transform.Find("Label");
                if (label != null)
                {
                    Destroy(label.gameObject);
                }
                slot.transform.SetParent(null);
                ResourcePool.Instance.PutBackInstance(SLOT_PREFAB_PATH, slot.gameObject);
            }
            _slots.Clear();

            base.Close(skipAnimation);
        }

        private void OnDestroy()
        {
            _sequence?.Kill();
        }
    }
}
