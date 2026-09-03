using UnityEngine;
using SamMul.GameClients.Stages.Characters.Stats;
using SamMul.ResourcePools;
using SamMul.UnityHelpers;

namespace SamMul.GameClients.Stages.Characters.StatusEffects
{
    public class SlowMove : StatusEffect
    {
        private const string SLOW_EFFECT_PATH = "Stages/StatusEffects/StatusEffect_Slow.prefab";
        private readonly StatModifier _moveSpeedMultiplyDecreaser;
        private readonly SpriteAnimationHandler _slowEffect;

        /// <param name="moveSpeedRate">
        /// effectParameter1 : 0~1 사이의 값을 사용한다.
        /// Percent Multiplier로, 0.2의 값을 넣으면 이동속도가 20%로 줄어든다. (이동속도의 80%만큼을 차감, 근데 곱셈으로)
        /// </param>
        public SlowMove(float moveSpeedRate, float duration) : base(StatusEffectType.SlowMove, duration)
        {
            float moveSpeedDecreaseRate = 1f - moveSpeedRate;
            if (moveSpeedDecreaseRate < 0f)
            {
                Debug.LogError($"SlowMove에 잘못된 moveSpeedRate[{moveSpeedRate}]이 입력됨.");
                moveSpeedDecreaseRate = 0f;
            }

            if (moveSpeedDecreaseRate >= 1f)
            {
                Debug.LogError($"SlowMove에 잘못된 moveSpeedRate[{moveSpeedRate}]이 입력됨.");
                moveSpeedDecreaseRate = 0.9f;
            }

            _moveSpeedMultiplyDecreaser = new StatModifier(-moveSpeedDecreaseRate, StatModType.PercentMult);
            _slowEffect = ResourcePool.Instance.InstantiateFromResource<SpriteAnimationHandler>(SLOW_EFFECT_PATH);
        }

        public override void Begin(Stage stage, Character owner, float now)
        {
            base.Begin(stage, owner, now);
            owner.Stats.MoveSpeed.AddModifier(_moveSpeedMultiplyDecreaser);

            var hitBox = owner.GetHitBoxSize();
            _slowEffect.gameObject.SetActive(true);
            _slowEffect.transform.SetParent(owner.transform);
            _slowEffect.transform.localPosition = 1.2f * owner.UIPositionOffset.y * Vector3.up;
            _slowEffect.transform.localScale = 1.2f * Mathf.Clamp(hitBox.x, 1.0f, 2.0f) * Vector3.one;
            _slowEffect.InitializeAndPlay();
        }

        public override void Update(Stage stage, Character owner, float now)
        {

        }

        public override void End(Stage stage, Character owner, float now)
        {
            owner.Stats.MoveSpeed.RemoveModifier(_moveSpeedMultiplyDecreaser);
            _slowEffect.gameObject.SetActive(false);
            ResourcePool.Instance.PutBackInstance(SLOW_EFFECT_PATH, _slowEffect.gameObject);
        }

        public override void Cancel(Character owner, Stage stage)
        {
            owner.Stats.MoveSpeed.RemoveModifier(_moveSpeedMultiplyDecreaser);
            _slowEffect.gameObject.SetActive(false);
            ResourcePool.Instance.PutBackInstance(SLOW_EFFECT_PATH, _slowEffect.gameObject);
        }
    }
}
