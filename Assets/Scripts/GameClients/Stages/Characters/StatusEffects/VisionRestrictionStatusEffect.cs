using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.Characters.StatusEffects
{
    //해당 상태이상 효과는 중복해서 적용하지 않는걸 추천한다
    //혹시 중복해서 적용하고 싶으면 연출이 중복 재생되지 않도록 수정해야됨. (시야 제한 연출때문에 어색해보임)
    public class VisionRestrictionStatusEffect : StatusEffect
    {
        private const string EFFECT_BLACKSPRITE_RESOURCE_PATH = "Stages/StatusEffects/VisionRestrictionBlackSprtie.prefab";

        private SpriteRenderer _blackSprite;
        private SpriteMask _visionMask;
        private float _visionRadius;
        private float _fadeOutAt;
        private bool _isFadeOut;

        public VisionRestrictionStatusEffect(float visionRadius ,float duration) : base (StatusEffectType.VisionRestriction, duration)
        {
            _visionRadius = visionRadius;
        }

        public override void Begin(Stage stage, Character owner, float now)
        {
            base.Begin(stage, owner, now);

            //화면을 가릴 검은색 큰 이미지 (쉐이더 기능을 통해 가운데 구멍이 뚫려있다.)
            _blackSprite = ResourcePool.Instance.InstantiateFromResource(EFFECT_BLACKSPRITE_RESOURCE_PATH).GetComponent<SpriteRenderer>();
            _blackSprite.gameObject.SetActive(true);
            _blackSprite.transform.SetParent(owner.transform);
            _blackSprite.transform.localPosition = Vector3.zero;
            _blackSprite.transform.localScale = Vector3.one * 40f;
            _blackSprite.color = new Color(0f, 0f, 0f, 0f);
            _blackSprite.material.SetVector("_MaskScale", new Vector4(0.03f * _visionRadius, 0.03f * _visionRadius, 0f, 0f));
            var fadeInSequence = DOTween.Sequence();
            fadeInSequence.Append(_blackSprite.DOFade(1f, 0.2f));
            fadeInSequence.Play();

            _fadeOutAt = now + Duration - 1f;
        }
        public override void Update(Stage stage, Character owner, float now)
        {
            if(_fadeOutAt <= now)
            {
                var fadeOutSequence = DOTween.Sequence();
                fadeOutSequence.Append(_blackSprite.DOFade(0f, 1));
                fadeOutSequence.Play();
                _fadeOutAt = float.MaxValue;
                _isFadeOut = true;
            }
        }

        public override void Cancel(Character owner, Stage stage)
        {
            if(_isFadeOut)
            {
                _blackSprite.transform.SetParent(null);
                _blackSprite.gameObject.SetActive(false);
                ResourcePool.Instance.PutBackInstance(EFFECT_BLACKSPRITE_RESOURCE_PATH, _blackSprite.gameObject);
                _blackSprite = null;
            }
            else
            {
                var fadeOutSequence = DOTween.Sequence();
                fadeOutSequence.Append(_blackSprite.DOFade(0f, 1));
                fadeOutSequence.Play();
                fadeOutSequence.OnComplete(() =>
                {
                    _blackSprite.transform.SetParent(null);
                    _blackSprite.gameObject.SetActive(false);
                    ResourcePool.Instance.PutBackInstance(EFFECT_BLACKSPRITE_RESOURCE_PATH, _blackSprite.gameObject);
                    _blackSprite = null;
                });
            }
        }

        public override void End(Stage stage, Character owner, float now)
        {
            if (_isFadeOut)
            {
                _blackSprite.transform.SetParent(null);
                _blackSprite.gameObject.SetActive(false);
                ResourcePool.Instance.PutBackInstance(EFFECT_BLACKSPRITE_RESOURCE_PATH, _blackSprite.gameObject);
                _blackSprite = null;
            }
            else
            {
                var fadeOutSequence = DOTween.Sequence();
                fadeOutSequence.Append(_blackSprite.DOFade(0f, 1));
                fadeOutSequence.Play();
                fadeOutSequence.OnComplete(() =>
                {
                    _blackSprite.transform.SetParent(null);
                    _blackSprite.gameObject.SetActive(false);
                    ResourcePool.Instance.PutBackInstance(EFFECT_BLACKSPRITE_RESOURCE_PATH, _blackSprite.gameObject);
                    _blackSprite = null;
                });
            }
        }
    }

}
