using UnityEngine;
using Z.GameClients.Stages;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.Characters.Actions;
using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.Monsters;
using Z.Animations.Placeholder;
using Animation = Z.Animations.Placeholder.Animation;

namespace Z.GameClients.Stages.Characters.Actions.Boss.Alibaba
{
    public class SinbadPoisonPotionThrowAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string ReadyAnimationName = "PeriodicExecutionBegin";
        private static readonly string AttackAnimationName = "PeriodicExecutionRepeat2";
        private static readonly string EndAnimationName = "PeriodicExecutionEnd";

        private static readonly float DAMAGE_COEFFICIENT = 1.0f;

        private Monster _owner;
        private Character _target;

        private Animation _readyAnimation;
        private Animation _attackAnimation;
        private Animation _endAnimation;

        private static readonly int AttackCount = 3;
        private static readonly float PotionPoisonRadius = 3f;
        private static readonly float PoisonDuration = 5f;
        private static readonly float PoisonIndicatorDuration = 0.5f;

        public override void Initialize(Monster owner, Character target, SpineMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
                animationController.FindAnimation(ReadyAnimationName).Duration +
                animationController.FindAnimation(AttackAnimationName).Duration +
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

            for (int i = 0; i < AttackCount; i++)
            {
                float attackAt = now + _readyAnimation.Duration + AnimationController.FindHitTime(_attackAnimation) + _attackAnimation.Duration * i;
                base.AddOneOffSubAction(attackAt, (Stage stage, float deltaTime, float now) =>
                {
                    this.ThrowPoisonPotion(stage, PoisonIndicatorDuration);
                });
            }

        }

        public override ActionBase End(Stage stage)
        {
            return null;
        }

        private void ThrowPoisonPotion(Stage stage, float delay)
        {
            var rect = stage.FenceRect != null ? stage.FenceRect.Value : stage.StaticData.GetWalkableArea();
            Vector2 startPosition = new Vector2(rect.xMin - 5f, rect.center.y);

                stage.CreatePoisonPotion(
                    _owner,
                    startPosition,
                    _target.Pos,
                    delay,
                    PotionPoisonRadius,
                    _owner.SpecialAttackPower * DAMAGE_COEFFICIENT,
                    PoisonDuration);
        }

    }
}

