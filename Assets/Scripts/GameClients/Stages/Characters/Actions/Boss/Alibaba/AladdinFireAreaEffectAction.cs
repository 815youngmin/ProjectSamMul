using UnityEngine;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;

namespace SamMul.GameClients.Stages.Characters.Actions.Boss.Alibaba
{
    public class AladdinFireAreaEffectAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string ReadyAnimationName = "PeriodicExecutionBegin";
        private static readonly string AttackAnimationName = "PeriodicExecutionRepeat";
        private static readonly string EndAnimationName = "PeriodicExecutionEnd";

        private static readonly int FireCount = 6;
        private static readonly float FireOffsetDistance = 5f;
        private static readonly float FireIndicatorDuration = 1.0f;
        private static readonly float FireRadius = 3f;
        private static readonly float BurnDuration = 1f;
        private static readonly float FIRE_DAMAGE_COEFFICIENT = 1.0f;
        private static readonly float BURN_DAMAGE_COEFFICIENT = 0.5f;

        private Monster _owner;
        private Character _target;

        private Animation _readyAnimation;
        private Animation _attackAnimation;
        private Animation _endAnimation;

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
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _attackAnimation, false, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _endAnimation, false, 0f);

            float time = now + _readyAnimation.Duration + AnimationController.FindHitTime(_attackAnimation);
            base.AddOneOffSubAction(time, (Stage stage, float deltaTime, float now) =>
            {
                for(int i = 0; i < FireCount; i++)
                {
                    Vector2 dir = Quaternion.Euler(0, 0, 360f / FireCount * i) * Vector2.up;
                    Vector2 pos = _owner.CenterPos + dir * FireOffsetDistance;
                    this.CreateFireAreaEffect(stage, _owner, pos);
                }
            });
        }

        public override ActionBase End(Stage stage)
        {
            return null;
        } 

        private void CreateFireAreaEffect(Stage stage, Monster owner, Vector2 position)
        {
            stage.CreateFirePillarAreaEffectObject(
                owner, 
                position,
                delay: 0f,
                FireIndicatorDuration, 
                FireRadius, 
                owner.SpecialAttackPower * FIRE_DAMAGE_COEFFICIENT, 
                owner.SpecialAttackPower * BURN_DAMAGE_COEFFICIENT, 
                BurnDuration);
        }
    }
}