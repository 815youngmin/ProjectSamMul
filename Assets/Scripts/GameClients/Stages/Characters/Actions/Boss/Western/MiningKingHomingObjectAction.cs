using UnityEngine;
using SamMul.GameClients.Stages.AreaEffectObjects;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;

namespace SamMul.GameClients.Stages.Characters.Actions.Boss.Western
{
    public class MiningKingHomingObjectAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string ReadyAnimationName = "PrepareSingleExecutionBegin2";
        private static readonly string WaitAnimationName = "PrepareSingleExecutionRepeatd2";
        private static readonly string AttackAnimationName = "SingleExecutionAction2";
        private static readonly string EndAnimationName = "SingleExecutionEnd";

        private static readonly float ObjectDamageCoefficient = 1.0f;
        private static readonly float ObjectRadius = 1.35f;
        private static readonly float ObjectMoveSpeed = 10f;
        private static readonly float ObjectRotatingSpeed = 720f;
        private static readonly float ObjectLifeTime = 4f;

        private static readonly float SplitObjectDamageCoefficient = 1.0f;
        private static readonly int SplitObjectAmount = 8;
        private static readonly float SplitObjectRadius = 0.5f;
        private static readonly float SplitObjectSpeed = 10f;
        private static readonly float SplitObjectRotatingSpeed = 720f;
        private static readonly float SplitObjectKnockbackPower = 0.1f;
        private static readonly float SplitObjectLifeTime = 6f;

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
                Vector2 firePos = _owner.CenterPos;
                Vector2 fireDirection = _target.Pos - firePos;
                var rect = stage.FenceRect != null ? stage.FenceRect.Value : stage.StaticData.GetWalkableArea();

                stage.CreateReflectionHomingReflectionSplitObject(_owner, _target.transform, _owner.Alliance,
                    AreaEffectType.MiningKingHomingSplitObject,
                    ObjectRadius, firePos, fireDirection, ObjectMoveSpeed, ObjectRotatingSpeed, _owner.SpecialAttackPower * ObjectDamageCoefficient, ObjectLifeTime, rect,
                    AreaEffectType.GoldReflectionObject,
                    SplitObjectAmount, SplitObjectRadius, SplitObjectSpeed, SplitObjectRotatingSpeed, _owner.SpecialAttackPower * SplitObjectDamageCoefficient, SplitObjectKnockbackPower, SplitObjectLifeTime);
            });
        }

        public override ActionBase End(Stage stage)
        {
            return null;
        }
    }
}

