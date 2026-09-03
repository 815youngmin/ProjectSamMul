using UnityEngine;
using SamMul.GameClients.Stages.AreaEffectObjects;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;

namespace SamMul.GameClients.Stages.Characters.Actions.Boss.Western
{
    public class HeadMinerHardSectorSplitObjectAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string ReadyAnimationName = "PrepareSingleExecutionBegin";
        private static readonly string WaitAnimationName = "PrepareSingleExecutionRepeatd";
        private static readonly string AttackAnimationName = "SingleExecutionAction";
        private static readonly string EndAnimationName = "SingleExecutionEnd";

        private static readonly float SPLITOBJECT_DAMAGE_COEFFICIENT = 1.0f;
        private static readonly float SPLITEDOBJECT_DAMAGE_COEFFICIENT = 1.0f;

        private static readonly float WaitDuration = 0.7f;

        private static readonly float SectorAngle = 90f;
        private static readonly int ProjectileAmount = 3;
        private static readonly float ProjectileSpeed = 7;
        private static readonly float ProjectileAliveDistance = 30f;
        private static readonly float ProjectileRadius = 1.35f;
        private static readonly float ProjectileKnobackPower = 0.1f;

        private static readonly int SplitedProjectileAmount = 8;
        private static readonly float SplitedProjectileRadius = 0.5f;
        private static readonly float SplitedProjectileSpeed = 10f;
        private static readonly float SplitedProjectileAcceleration = 1f;
        private static readonly float SplitedProjectileKnobackPower = 0.1f;
        private static readonly float SplitedProjectileAliveDistance = 100;
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
                WaitDuration +
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
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _waitAnimation, true, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _attackAnimation, false, WaitDuration);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _endAnimation, false, 0f);

            float time = now + _readyAnimation.Duration + WaitDuration + AnimationController.FindHitTime(_attackAnimation);
            base.AddOneOffSubAction(time, (Stage stage, float deltaTime, float now) =>
            {
                Vector2 firePosition = _owner.CenterPos;
                Vector2 fireDirection = (_target.Pos - firePosition).normalized;
                this.TrySectorFiring(stage, firePosition, fireDirection);
            });
        }

        public override ActionBase End(Stage stage)
        {
            return null;
        }

        private void TrySectorFiring(Stage stage, Vector2 firePosition, Vector2 fireDirection)
        {
            var rect = stage.FenceRect != null ? stage.FenceRect.Value : stage.StaticData.GetWalkableArea();

            for (int i = -(ProjectileAmount - 1); i <= ProjectileAmount - 1; i += 2)
            {
                Vector2 dir = Quaternion.Euler(0.0f, 0.0f, 0.5f * (float)i * SectorAngle / ProjectileAmount) * fireDirection;
                stage.CreateSplitAreaEffectObject(
                    AreaEffectType.HeadMinerHardSplitObject,
                    _owner,
                    ProjectileRadius,
                    firePosition,
                    dir,
                    ProjectileSpeed,
                    _owner.SpecialAttackPower * SPLITOBJECT_DAMAGE_COEFFICIENT,
                    ProjectileAliveDistance,
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

