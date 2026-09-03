using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;
using UnityEngine;
using SamMul.GameClients.Stages.AreaEffectObjects;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;

namespace SamMul.GameClients.Stages.Characters.Actions.Boss.Western
{
    public class KidSplitReflectionObjectAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string ReadyAnimationName = "PrepareSingleExecutionBegin";
        private static readonly string WaitAnimationName = "PrepareSingleExecutionRepeat";
        private static readonly string AttackAnimationName = "SingleExecutionAction";
        private static readonly string EndAnimationName = "SingleExecutionEnd";

        private static readonly float SPLITOBJECT_DAMAGE_COEFFICIENT = 1.0f;
        private static readonly float ProjectileSpeed = 20;
        private static readonly float ProjectileAliveDistance = 100f;
        private static readonly float ProjectileRadius = 0.4f;
        private static readonly float ProjectileKnobackPower = 0.1f;

        private static readonly float SplitObjectDamageCoefficient = 1.0f;
        private static readonly int SplitObjectAmount = 5;
        private static readonly float SplitObjectRadius = 0.2f;
        private static readonly float SplitObjectSpeed = 20f;
        private static readonly float SplitObjectRotatingSpeed = 0f;
        private static readonly float SplitObjectKnockbackPower = 0.1f;
        private static readonly float SplitObjectLifeTime = 6f;



        private Monster _owner;
        private Character _target;

        private Animation _readyAnimation;
        private Animation _waitAnimation;
        private Animation _attackAnimation;
        private Animation _endAnimation;
        private Bone _fireBone;
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

            _fireBone = AnimationController.Body.skeleton.FindBone("w_r");
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
                Vector2 firePosition = _fireBone.GetWorldPosition(_owner.Body.transform);
                Vector2 fireDirection = (_target.Pos - firePosition).normalized;
                this.Firing(stage, firePosition, fireDirection);
            });
        }


        public override ActionBase End(Stage stage)
        {
            return null;
        }

        private void Firing(Stage stage, Vector2 firePosition, Vector2 fireDirection)
        {
            var rect = stage.FenceRect != null ? stage.FenceRect.Value : stage.StaticData.GetWalkableArea();

            stage.CreateSplitReflectionObject(
                AreaEffectType.KidSplitReflectionObject,
                _owner,
                ProjectileRadius,
                firePosition,
                fireDirection,
                ProjectileSpeed,
                _owner.SpecialAttackPower * SPLITOBJECT_DAMAGE_COEFFICIENT,
                ProjectileAliveDistance,
                rect,
                AreaEffectType.KidReflectionObject,
                SplitObjectAmount, SplitObjectRadius, SplitObjectSpeed, SplitObjectRotatingSpeed, _owner.SpecialAttackPower * SplitObjectDamageCoefficient, SplitObjectKnockbackPower, SplitObjectLifeTime);
        }

    }
}

