using UnityEngine;
using Z.GameClients.Stages.AreaEffectObjects;
using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.Monsters;
using Z.Animations.Placeholder;
using Animation = Z.Animations.Placeholder.Animation;

namespace Z.GameClients.Stages.Characters.Actions.Boss.Western
{
    public class WyattEarpCircleSplitObjectAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string ReadyAnimationName = "PrepareSingleExecutionBegin3";
        private static readonly string WaitAnimationName = "PrepareSingleExecutionRepeatd3";
        private static readonly string AttackAnimationName = "SingleExecutionAction3";
        private static readonly string EndAnimationName = "SingleExecutionEnd3";

        private static readonly float SPLITOBJECT_DAMAGE_COEFFICIENT = 1.0f;
        private static readonly float SPLITEDOBJECT_DAMAGE_COEFFICIENT = 1.0f;

        private static readonly int ProjectileAmount = 8;
        private static readonly float ProjectileOffsetDistance = 2f;
        private static readonly float ProjectileSpeed = 8;
        private static readonly float ProjectileRotatingSpeed = 0f;
        private static readonly float ProjectileLifeTime = 4f;
        private static readonly float ProjectileRadius = 0.4f;
        private static readonly float ProjectileKnobackPower = 0.1f;

        private static readonly int SplitedProjectileAmount = 4;
        private static readonly float SplitedProjectileRadius = 0.2f;
        private static readonly float SplitedProjectileSpeed = 10;
        private static readonly float SplitedProjectileAcceleration = 1f;
        private static readonly float SplitedProjectileKnobackPower = 0.1f;
        private static readonly float SplitedProjectileAliveDistance = 60f;
        private static readonly bool IsSpinBladeCollide = true;
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
                this.TryCircleFiring(stage, _owner.CenterPos);
            });

        }


        public override ActionBase End(Stage stage)
        {
            return null;
        }

        private void TryCircleFiring(Stage stage, Vector2 firePosition)
        {
            var rect = stage.FenceRect != null ? stage.FenceRect.Value : stage.StaticData.GetWalkableArea();

            for (int i = 0; i < ProjectileAmount; i++)
            {
                Vector2 dir = Quaternion.Euler(0.0f, 0.0f, 360f / ProjectileAmount * i) * Vector2.up;
                stage.CreateReflectionSplitAreaEffectObject(
                    AreaEffectType.WyattEarpReflectionSplitObject,
                    _owner,
                    ProjectileRadius,
                    firePosition + dir * ProjectileOffsetDistance,
                    dir,
                    ProjectileSpeed,
                    ProjectileRotatingSpeed,
                    _owner.SpecialAttackPower * SPLITOBJECT_DAMAGE_COEFFICIENT,
                    ProjectileLifeTime,
                    rect,
                    SplitedProjectileAmount,
                    SplitedProjectileRadius,
                    SplitedProjectileSpeed,
                    SplitedProjectileAcceleration,
                    _owner.SpecialAttackPower * SPLITEDOBJECT_DAMAGE_COEFFICIENT,
                    SplitedProjectileKnobackPower,
                    SplitedProjectileAliveDistance,
                    IsSpinBladeCollide
                    );
            }
        }

    }
}

