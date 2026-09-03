using UnityEngine;
using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.Monsters;
using Z.Animations.Placeholder;
using Animation = Z.Animations.Placeholder.Animation;

namespace Z.GameClients.Stages.Characters.Actions.Boss.Steampunk
{
    public class ThomasEdisonRandomLightningAction : SmartAction<SpineMonsterAnimationController>
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

        private static readonly int AreaEffectAmount = 12;
        private static readonly float AreaEffectCreateDelay = 0.2f;
        private static readonly float AreaEffectCreateRadius = 10f;
        private static readonly float AreaEffectRadius = 2f;
        private static readonly float IndicatorTime = 3f;


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
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);
            AnimationController.SetAnimation(BodyAnimationTrack.WholeBody, _readyAnimation, false);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _attackAnimation, true, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _endAnimation, false, AreaEffectCreateDelay * AreaEffectAmount);

            float attackAt = now + _readyAnimation.Duration;
            for(int i = 0; i < AreaEffectAmount; i++)
            {
                base.AddOneOffSubAction(attackAt + i * AreaEffectCreateDelay, (Stage stage, float deltaTime, float now) =>
                {
                    Vector2 createPos = _target.Pos + Random.insideUnitCircle * AreaEffectCreateRadius;
                    stage.CreateZeusLightningAreaEffectObject(
                        _owner, createPos, AreaEffectRadius, DAMAGE_COEFFICIENT * _owner.SpecialAttackPower, delay: 0f, IndicatorTime);
                });
            }
        }

        public override ActionBase End(Stage stage)
        {
            return null;
        }
    }
}

