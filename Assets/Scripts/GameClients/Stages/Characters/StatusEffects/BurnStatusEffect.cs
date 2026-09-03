#nullable enable
using Shared.GameDataTypes;
using UnityEngine;
using SamMul.ResourcePools;
using SamMul.UnityHelpers;

namespace SamMul.GameClients.Stages.Characters.StatusEffects
{
    public class BurnStatusEffect : StatusEffect
    {
        private const string EFFECT_ANIMATION_RESOURCE_PATH = "Stages/StatusEffects/StatusEffect_Fire.prefab";
        private readonly float _period;
        private readonly float _dotDamage;

        private SpriteAnimationHandler? _effectAnimation;

        public BurnStatusEffect(float duration, float dotDamage) : base(StatusEffectType.Burn, duration)
        {
            _period = 0.66f;
            _dotDamage = dotDamage;
            _effectAnimation = null;
        }

        public override void Begin(Stage stage, Character owner, float now)
        {
            base.Begin(stage, owner, now);

            _lastDamagedAt = now;

            // NOTE: BodyEffect의 인스턴스 ID 체계를 만들어야겠다.
            // 혹여나 BurnStatusEffect가 먼저 끝난다면, 부여한 BodyEffect도 제거해줘야한다.
            // End(...) 함수에서 이를 해제할 수 있게 해야한다.
            owner.AnimationController.BeginBurnBodyEffect(this.Duration);

            // 히트박스 크기에 맞춰 그려준다.
            // 1배 미만은 안 줄이고, 2배 이상은 안 키운다.
            // 8배 이상은 제거한다. 
            float hitBoxWidth = owner.GetHitBoxSize().x;
            if (hitBoxWidth <= 8.0f)
            {
                _effectAnimation = ResourcePool.Instance.InstantiateFromResource<SpriteAnimationHandler>(EFFECT_ANIMATION_RESOURCE_PATH);
                _effectAnimation.transform.SetParent(owner.transform, worldPositionStays: false);
                _effectAnimation.transform.localScale = Mathf.Clamp(hitBoxWidth, 1.0f, 2.0f) * Vector3.one;
                _effectAnimation.transform.localPosition = owner.CharacterType.IsHeroType() ? new Vector2(0f, 1.3f) : owner.UIPositionOffset;
                _effectAnimation.InitializeAndPlay();
            }
            else
            {
                _effectAnimation = null;
            }
        }

        private float _lastDamagedAt = 0.0f;
        public override void Update(Stage stage, Character owner, float now)
        {
            if (_lastDamagedAt + _period < now)
            {
                _lastDamagedAt = now;
                owner.Hitted(stage, attacker: null, _dotDamage, Vector2.zero, owner.UIPos, hitSoundPrefabPath: string.Empty);
            }
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
