using UnityEngine;
using SamMul.GameClients.Stages;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.Characters.Actions;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;

namespace SamMul.GameClients.Stages.Characters.Actions.Boss.Alibaba
{
    public class SinbadMultiPoisonPotionThrowAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string ReadyAnimationName = "PeriodicExecutionBegin";
        private static readonly string AttackAnimationName = "PeriodicExecutionRepeat";
        private static readonly string EndAnimationName = "PeriodicExecutionEnd";
        
        private static readonly float DAMAGE_COEFFICIENT = 1.0f;

        private Monster _owner;
        private Character _target;

        private Animation _readyAnimation;
        private Animation _attackAnimation;
        private Animation _endAnimation;

        private static readonly int AttackCount = 3;
        private static readonly int PotionAmount = 6;
        private static readonly float PotionPoisonRadius = 2f;
        private static readonly float PotionThrowRadius = 10f;
        private static readonly float PotionThrowRandomDelay = 0.3f;
        private static readonly float PoisonDuration = 5f;
        private static readonly float PotionIndicatorDuration = 0.5f;


        public override void Initialize(Monster owner, Character target, SpineMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
                animationController.FindAnimation(ReadyAnimationName).Duration +
                animationController.FindAnimation(AttackAnimationName).Duration * AttackCount +
                animationController.FindAnimation(EndAnimationName).Duration,
                animationController);

            _owner = owner;
            _target = target;

            _readyAnimation = AnimationController.FindAnimation(ReadyAnimationName);
            _attackAnimation = AnimationController.FindAnimation(AttackAnimationName);
            _endAnimation = AnimationController.FindAnimation(EndAnimationName);
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);
            AnimationController.SetAnimation(BodyAnimationTrack.WholeBody, _readyAnimation, false);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _attackAnimation, true, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _endAnimation, false, _attackAnimation.Duration * AttackCount);

            for(int i = 0; i < AttackCount; i++)
            {
                float attackAt = now + _readyAnimation.Duration + AnimationController.FindHitTime(_attackAnimation) + _attackAnimation.Duration * i;
                base.AddOneOffSubAction(attackAt, (Stage stage, float deltaTime, float now) =>
                {
                    this.ThrowPoisonPotions(stage, PotionIndicatorDuration + Random.Range(0, PotionThrowRandomDelay));
                });
            }
        }
        public override ActionBase End(Stage stage)
        {
            return null;
        }

        private void ThrowPoisonPotions(Stage stage, float delay)
        {
            var rect = stage.FenceRect != null ? stage.FenceRect.Value : stage.StaticData.GetWalkableArea();
            Vector2 startPosition = new Vector2(rect.xMin - 5f, rect.center.y);
            for (int i = 0; i < PotionAmount; i++)
            {
                stage.CreatePoisonPotion(
                    _owner,
                    startPosition,
                    _owner.CenterPos + Random.insideUnitCircle * PotionThrowRadius,
                   delay,
                    PotionPoisonRadius,
                    _owner.SpecialAttackPower * DAMAGE_COEFFICIENT,
                    PoisonDuration);
            }
        }

    }
}

