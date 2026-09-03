using UnityEngine;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;

namespace SamMul.GameClients.Stages.Characters.Actions.Boss.Western
{
    public class JesseJamesReflectionHomingBoomerangAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string ReadyAnimationName = "PrepareSingleExecutionBegin";
        private static readonly string WaitAnimationName  = "PrepareSingleExecutionRepeat";
        private static readonly string AttackAnimationName = "SingleExecutionAction";
        private static readonly string EndAnimationName = "SingleExecutionEnd";

        private static readonly float DAMAGE_COEFFICIENT = 1.0f;


        private static readonly float AreaEffectMoveSpeed = 14f;
        private static readonly float AreaEffectRadius = 1f;
        private static readonly int ReflectionCount = 5;
        private static readonly float AreaEffectAttackPeriod = 0.5f;
        private static readonly float RotatingSpeed = 720f;

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
                animationController.FindAnimation(AttackAnimationName).Duration +
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
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _attackAnimation, false, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _endAnimation, false, 0f);

            float time = now + _readyAnimation.Duration + _waitAnimation.Duration + AnimationController.FindHitTime(_attackAnimation);
            base.AddOneOffSubAction(time, (Stage stage, float deltaTime, float now) =>
            {
                Vector2 startPosition = _owner.CenterPos;
                Vector2 dir = (_target.Pos -  startPosition).normalized;
                this.CreateReflectionHomingBoomerang(stage, startPosition, dir);
            });

        }


        public override ActionBase End(Stage stage)
        {
            return null;
        }

        private void CreateReflectionHomingBoomerang(Stage stage, Vector2 startPosition, Vector2 startDirection)
        {
            var rect = stage.FenceRect != null ? stage.FenceRect.Value : stage.StaticData.GetWalkableArea();
            stage.CreateReflectionHomingBoomerangObject(
                _owner,
                _target,
                AreaEffectObjects.AreaEffectType.JesseJameReflectionHomingBoomerangObject,
                AreaEffectRadius,
                startPosition,
                startDirection,
                AreaEffectMoveSpeed,
                RotatingSpeed,
                _owner.SpecialAttackPower * DAMAGE_COEFFICIENT,
                ReflectionCount,
                AreaEffectAttackPeriod,
                rect
                );
        }

    }
}

