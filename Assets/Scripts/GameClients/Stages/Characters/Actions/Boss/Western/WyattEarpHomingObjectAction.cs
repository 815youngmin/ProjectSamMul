using Z.Animations.Placeholder;
using Animation = Z.Animations.Placeholder.Animation;
using UnityEngine;
using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.Monsters;

namespace Z.GameClients.Stages.Characters.Actions.Boss.Western
{
    public class WyattEarpHomingObjectAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string ReadyAnimationName = "PrepareSingleExecutionBegin2";
        private static readonly string WaitAnimationName = "PrepareSingleExecutionRepeatd2";
        private static readonly string AttackAnimationName = "SingleExecutionAction2";
        private static readonly string EndAnimationName = "SingleExecutionEnd2";

        private static readonly float DAMAGE_COEFFICIENT = 1.0f;

        private const string HomingProjectilePath = "Stages/Projectiles/BulletSmallRadius0_2.prefab";
        private static readonly int HomingProjectileAmount = 10;
        private static readonly float HomingProjectileRadius = 0.2f;
        private static readonly float HomingProjectileSpeed = 15.0f;
        private static readonly float HomingPower = 1.0f;
        private static readonly float HomingProjectileLifeTime = 12.0f;
        private static readonly int HomingProjectileHitChance = 1;

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

            _fireBone = AnimationController.Body.skeleton.FindBone("fire");
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);
            AnimationController.SetAnimation(BodyAnimationTrack.WholeBody, _readyAnimation, false);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _waitAnimation, false, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _attackAnimation, false, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _endAnimation, false, 0f);


            float fireDuration = _attackAnimation.Duration - AnimationController.FindHitTime(_attackAnimation);
            float time = now + _readyAnimation.Duration + _waitAnimation.Duration + AnimationController.FindHitTime(_attackAnimation);

            for(int i = 0; i < HomingProjectileAmount; i++)
            {
                float angle = (i % 2 == 0 ? 1.0f : -1.0f) * ((float)((i / 2) + 1) * 20.0f);
                base.AddOneOffSubAction(time + (fireDuration / HomingProjectileAmount) * i, (Stage stage, float deltaTime, float now) =>
                {
                    Vector2 firePosition = _fireBone.GetWorldPosition(_owner.Body.transform);
                    Vector2 fireDirection = Quaternion.Euler(0.0f, 0.0f, angle) * (_target.CenterPos - firePosition).normalized;
                    this.TryMultipleCircleFiring(stage, firePosition, fireDirection);
                });
            }
        }

        public override ActionBase End(Stage stage)
        {
            return null;
        }

        private void TryMultipleCircleFiring(Stage stage, Vector2 firePosition, Vector2 fireDirection)
        {
            stage.CreateHomingProjectileAreaEffectObject(
            HomingProjectilePath,
            _owner,
            _target,
            firePosition,
            fireDirection,
            _owner.SpecialAttackPower * DAMAGE_COEFFICIENT,
            HomingProjectileRadius,
            HomingProjectileSpeed,
            HomingPower,
            HomingProjectileLifeTime,
            willRotateInMoveDirection: true,
            hitChances: HomingProjectileHitChance
            );
        }
    }
}

