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
    public class PeribanouAreaEffectAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string ReadyAnimationName = "PeriodicExecutionBegin";
        private static readonly string AttackAnimationName = "PeriodicExecutionRepeat";
        private static readonly string EndAnimationName = "PeriodicExecutionEnd";

        private static readonly float DAMAGE_COEFFICIENT = 1.0f;
        private static readonly int AttackCount = 3;
        private static readonly float AttackDelay = 0.5f;
        private static readonly float AreaEffectRadius = 2.5f;
        private static readonly float IndicatorDuration = 1f;
        private static readonly float AreaEffectAttackPeriod = 0.25f;
        private static readonly float AreaEffectLifeTime = 5f;


        private Monster _owner;
        private Character _target;

        private Animation _readyAnimation;
        private Animation _attackAnimation;
        private Animation _endAnimation;

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

            float attackAt;
            for (int i = 0; i < AttackCount; i++)
            {
                attackAt = now + _readyAnimation.Duration + AnimationController.FindHitTime(_attackAnimation) + _attackAnimation.Duration * i;
                base.AddOneOffSubAction(attackAt, (Stage stage, float deltaTime, float now) =>
                {
                    this.CreateAreaEffect(stage);
                });
            }
        }

        public override ActionBase End(Stage stage)
        {
            return null;
        }

        private void CreateAreaEffect(Stage stage)
        {
            Vector2 pos = _target.Pos;

            stage.CreateSandAreaEffectObject(_owner, delay: 0f, IndicatorDuration, _owner.SpecialAttackPower * DAMAGE_COEFFICIENT,
                AreaEffectAttackPeriod, AreaEffectLifeTime, AreaEffectRadius, pos);
        }
    }
}

