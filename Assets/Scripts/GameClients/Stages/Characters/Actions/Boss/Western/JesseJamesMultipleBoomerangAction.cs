using UnityEngine;
using SamMul.GameClients.Stages.AreaEffectObjects;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;

namespace SamMul.GameClients.Stages.Characters.Actions.Boss.Western
{
    public class JesseJamesMultipleBoomerangAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string ReadyAnimationName = "PrepareSingleExecutionBegin";
        private static readonly string WaitAnimationName = "PrepareSingleExecutionRepeat";
        private static readonly string AttackAnimationName = "SingleExecutionAction";
        private static readonly string EndAnimationName = "SingleExecutionEnd";

        private static readonly float DAMAGE_COEFFICIENT = 1.0f;
        private static readonly int FireCount = 3;
        private static readonly float BoomerangDuration = 2f;
        private static readonly float BoomerangSpeed = 15f;
        private static readonly float BoomerangRadius = 1f;
        private static readonly float BoomerangRotateSpeed = 720f;

        private Monster _owner;
        private Character _target;

        private Animation _readyAnimation;
        private Animation _waitAnimation;
        private Animation _attackAnimation;
        private Animation _endAnimation;

        public override void Initialize(Monster owner, Character target, SpineMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
                animationController.FindAnimation(ReadyAnimationName).Duration +
                animationController.FindAnimation(WaitAnimationName).Duration +
                animationController.FindAnimation(AttackAnimationName).Duration * FireCount +
                animationController.FindAnimation(EndAnimationName).Duration,
                animationController);

            _owner = owner;
            _target = target;

            _readyAnimation = AnimationController.FindAnimation(ReadyAnimationName);
            _waitAnimation = AnimationController.FindAnimation(WaitAnimationName);
            _attackAnimation = AnimationController.FindAnimation(AttackAnimationName);
            _endAnimation = AnimationController.FindAnimation(EndAnimationName);
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);
            AnimationController.SetAnimation(BodyAnimationTrack.WholeBody, _readyAnimation, false);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _waitAnimation, false, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _attackAnimation, true, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _endAnimation, false, _attackAnimation.Duration * FireCount);

            for(int i = 0; i < FireCount; i++)
            {
                float time = now + _readyAnimation.Duration + _waitAnimation.Duration + AnimationController.FindHitTime(_attackAnimation) + _attackAnimation.Duration * i;
                base.AddOneOffSubAction(time, (Stage stage, float deltaTime, float now) =>
                {
                    this.Fire(stage, _owner.CenterPos);
                });
            }
        }

        public override ActionBase End(Stage stage)
        {
            return null;
        }


        private void Fire(Stage stage, Vector2 firePos)
        {
            Vector2 dir = (_target.Pos - firePos).normalized;
            Vector2 endPosition = firePos + dir * (BoomerangSpeed * BoomerangDuration * 0.5f);

            stage.CreateBoomerangObject(
                AreaEffectType.JesseJameBoomerangObject,
                _owner,
                BoomerangDuration,
                firePos,
                endPosition,
                _owner.RangeAttackPower * DAMAGE_COEFFICIENT,
                BoomerangRadius,
                BoomerangRotateSpeed);
        }

    }
}

