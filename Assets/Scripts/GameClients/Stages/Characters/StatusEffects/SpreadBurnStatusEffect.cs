#nullable enable
using Shared.GameDataTypes;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.Characters.StatusEffects
{
    public class SpreadBurnStatusEffect : StatusEffect
    {
        private const string EFFECT_ANIMATION_RESOURCE_PATH = "Stages/StatusEffects/StatusEffect_Fire.prefab";
        private const float PERIOD = 0.66f;
        private const float SPREAD_RADIUS = 4.0f;

        private readonly float _dotDamage;
        private readonly int _spreadAmount;

        private SpriteAnimationHandler? _effectAnimation;

        public SpreadBurnStatusEffect(float duration, float dotDamage, int spreadAmount) : base(StatusEffectType.SpreadBurn, duration)
        {
            _dotDamage = dotDamage;
            _spreadAmount = spreadAmount;

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
            if (_lastDamagedAt + PERIOD < now)
            {
                _lastDamagedAt = now;
                owner.Hitted(stage, attacker: null, _dotDamage, Vector2.zero, owner.UIPos, hitSoundPrefabPath: string.Empty);
            }
        }

        public override void OwnerDead(Character owner, Stage stage)
        {
            // owner는 spreadBurn에 걸렸던 대상임(Monsters) 그러니 적이 아닌 자신과 동일한 Alliance를 탐색한다.
            List<Character> foundCharacters = new List<Character>();
            stage.FindAliveCharactersInArea(owner.Alliance, new CircularTargetArea(owner.Pos, SPREAD_RADIUS), foundCharacters);

            // list에 자기 자신도 포함되어 있기때문에 자기 자신은 제거한다.
            foundCharacters.Remove(owner);

            // 주변에 점화 전이할 대상이 전이량 보다 더 많은 경우 가까운 순서대로 정렬한다. 
            if (foundCharacters.Count > _spreadAmount)
            {
                foundCharacters.Sort((v1, v2) =>
                {
                    float distance1 = (v1.Pos - owner.Pos).sqrMagnitude;
                    float distance2 = (v2.Pos - owner.Pos).sqrMagnitude;
                    return distance1.CompareTo(distance2);
                });
            }

            int spreadCount = 0;
            foreach (Character character in foundCharacters)
            {
                character.StatusEffects.AddOrUpdateStatusEffect(stage, character, StatusEffectType.SpreadBurn, Duration, Time.time, _dotDamage, _spreadAmount);
                spreadCount++;

                if (spreadCount > _spreadAmount)
                {
                    break;
                }
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
