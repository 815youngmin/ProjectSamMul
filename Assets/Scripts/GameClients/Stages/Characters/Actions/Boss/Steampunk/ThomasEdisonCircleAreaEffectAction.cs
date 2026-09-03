using UnityEngine;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;

namespace SamMul.GameClients.Stages.Characters.Actions.Boss.Steampunk
{
    public class ThomasEdisonCircleAreaEffectAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string ReadyAnimationName = "PeriodicExecutionBegin2";
        private static readonly string AttackAnimationName = "PeriodicExecutionRepeat2";
        private static readonly string EndAnimationName = "PeriodicExecutionEnd2";

        private static readonly float DAMAGE_COEFFICIENT = 1.0f;

        private Monster _owner;
        private Character _target;

        private Animation _readyAnimation;
        private Animation _attackAnimation;
        private Animation _endAnimation;

        private static readonly int AreaEffectAmount = 6;
        private static readonly float AreaEffectCreateDelay = 0.1f;
        private static readonly float AreaEffectCreateRadius = 6f;
        private static readonly float AreaEffectRadius = 3f;
        private static readonly float IndicatorTime = 3f;

        private int _currentCreateAreaEffect;


        public override void Initialize(Monster owner, Character target, SpineMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
                animationController.FindAnimation(ReadyAnimationName).Duration +
                AreaEffectCreateDelay * AreaEffectAmount +
                animationController.FindAnimation(EndAnimationName).Duration,
                animationController);

            _owner = owner;
            _target = target;

            _readyAnimation = AnimationController.FindAnimation(ReadyAnimationName);
            _attackAnimation = AnimationController.FindAnimation(AttackAnimationName);
            _endAnimation = AnimationController.FindAnimation(EndAnimationName);

            _currentCreateAreaEffect = 0;
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);
            AnimationController.SetAnimation(BodyAnimationTrack.WholeBody, _readyAnimation, false);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _attackAnimation, true, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _endAnimation, false, AreaEffectCreateDelay * AreaEffectAmount);

            float attackAt = now + _readyAnimation.Duration;
            for (int i = 0; i < AreaEffectAmount; i++)
            {
                base.AddOneOffSubAction(attackAt + i * AreaEffectCreateDelay, (Stage stage, float deltaTime, float now) =>
                {
                    CreateLightning(stage, _currentCreateAreaEffect++);
                });
            }
        }


        public override ActionBase End(Stage stage)
        {
            return null;
        }

        private void CreateLightning(Stage stage, int currentCreateCount)
        {
            Vector2 dir = Quaternion.Euler(0.0f, 0.0f, 360f / AreaEffectAmount * currentCreateCount) * Vector2.up;
            Vector2 createPos = _owner.CenterPos + dir * AreaEffectCreateRadius;

            stage.CreateZeusLightningAreaEffectObject(
                _owner, createPos, AreaEffectRadius, DAMAGE_COEFFICIENT * _owner.SpecialAttackPower, delay: 0f, IndicatorTime);
        }

    }
}

