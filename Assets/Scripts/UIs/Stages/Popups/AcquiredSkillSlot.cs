using DG.Tweening;
using Shared.DataTables;
using Shared.GameDataTypes;
using Shared.StaticDatas;
using UnityEngine;
using UnityEngine.UI;
using SamMul.ResourcePools;

namespace SamMul.UIs.Stages.Popups
{
    public class AcquiredSkillSlot : MonoBehaviour
    {
        [SerializeField] private Image _backGround;
        [SerializeField] private Image _skillIcon;

        private SkillId _skillId;
        private Sequence _scalingAnimationSequence;
        public SkillId SkillId=>_skillId;

        private void Awake()
        {
            _scalingAnimationSequence = DOTween.Sequence();
            float scale = 1.2f;
            RectTransform rectTransform = GetComponent<RectTransform>();
            _scalingAnimationSequence.Append(rectTransform.DOScale(new Vector2(scale, scale), 0.2f));
            _scalingAnimationSequence.Append(rectTransform.DOScale(Vector2.one, 0.2f));
            _scalingAnimationSequence.Append(rectTransform.DOScale(new Vector2(scale, scale), 0.2f));
            _scalingAnimationSequence.Append(rectTransform.DOScale(Vector2.one, 0.2f));
            _scalingAnimationSequence.SetLoops(-1);
            _scalingAnimationSequence.SetUpdate(isIndependentUpdate: true);
            _scalingAnimationSequence.SetRecyclable(true);
            _scalingAnimationSequence.SetAutoKill(false);
            _scalingAnimationSequence.Pause();
        }

        private void OnDestroy()
        {
            _scalingAnimationSequence.Pause();
            _scalingAnimationSequence.Kill();
        }

        public void Initialize(SkillKey skillKey)
        {
            _skillId = skillKey.Id;
            if (skillKey.Id == SkillId.Invalid)
            {
                _backGround.gameObject.SetActive(false);
                _skillIcon.gameObject.SetActive(false);
                return;
            }

            _backGround.gameObject.SetActive(true);

            SkillStaticData skillStaticData = StaticDataRepository.Instance.Skills.Get(skillKey);
            
            // SkillIcon
            {
                _skillIcon.gameObject.SetActive(true);
                _skillIcon.sprite = ResourcePool.Instance.LoadResource<Sprite>(skillStaticData.IconResourcePath);
            }
            // BackGround.
            {
                switch (skillStaticData.skillType)
                {
                    case SkillType.Active:
                        {
                            if(GameConstants.SKILL_TRANSCENDENT_LEVEL == skillKey.Level)
                            {
                                _backGround.color = new Color(0.575471f, 0.0841491f, 0.420137f, 0.796078f);
                            }
                            else
                            {
                                _backGround.color = new Color(0, 0, 0, 0.5f);
                            }
                        }
                        break;
                    case SkillType.Passive:
                        {
                            _backGround.color = new Color(0, 0, 0, 0.5f);
                        }
                        break;
                }
            }
        }

        public void PlayScalingAnimation()
        {
            _scalingAnimationSequence.Restart();
        }
        public void StopScalingAnimation()
        {
            _scalingAnimationSequence.Pause();
            transform.localScale = Vector2.one;
        }
    }
}
