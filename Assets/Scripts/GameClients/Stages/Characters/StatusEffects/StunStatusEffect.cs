#nullable enable
using UnityEngine;
using Z.GameClients.Stages.Characters.Actions;
using Z.ResourcePools;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.Characters.StatusEffects
{
    public class StunStatusEffect : StatusEffect
    {
        private const string EFFECT_ANIMATION_RESOURCE_PATH = "Stages/StatusEffects/StatusEffect_Stun.prefab";
        private readonly float _duration;

        private SpriteAnimationHandler? _effectAnimation;

        public StunStatusEffect(float duration)
            : base(StatusEffectType.Stun, duration)
        {
            _duration = duration;
        }

        public override void Begin(Stage stage, Character owner, float now)
        {
            base.Begin(stage, owner, now);
            owner.Action.ChangeTo(stage, new StunAction(owner.AnimationController, _duration));

            // 히트박스 크기에 맞춰 그려준다.
            // 1배 미만은 안 줄이고, 2배 이상은 안 키운다.
            // 8배 이상은 제거한다. 
            float hitBoxWidth = owner.GetHitBoxSize().x;
            if (hitBoxWidth <= 8.0f)
            {
                _effectAnimation = ResourcePool.Instance.InstantiateFromResource<SpriteAnimationHandler>(EFFECT_ANIMATION_RESOURCE_PATH);
                _effectAnimation.transform.SetParent(owner.transform, worldPositionStays: false);
                _effectAnimation.transform.localScale = Mathf.Clamp(hitBoxWidth, 1.0f, 2.0f) * Vector3.one;
                _effectAnimation.gameObject.transform.localPosition = owner.UIPositionOffset;
                _effectAnimation.InitializeAndPlay();
            }
            else
            {
                _effectAnimation = null;
            }
        }

        public override void Update(Stage stage, Character owner, float now)
        {

        }

        public override void End(Stage stage, Character owner, float now)
        {
            if (_effectAnimation != null)
            {
                ResourcePool.Instance.PutBackInstance(EFFECT_ANIMATION_RESOURCE_PATH, _effectAnimation.gameObject);
                _effectAnimation = null;
            }
        }

        public override void Cancel(Character owner, Stage stage)
        {
            if (_effectAnimation != null)
            {
                ResourcePool.Instance.PutBackInstance(EFFECT_ANIMATION_RESOURCE_PATH, _effectAnimation.gameObject);
                _effectAnimation = null;
            }
        }
    }
}
