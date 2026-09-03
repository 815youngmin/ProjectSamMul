using UnityEngine;
using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.Monsters;
using Z.Animations.Placeholder;
using Animation = Z.Animations.Placeholder.Animation;

namespace Z.GameClients.Stages.Characters.Actions.Boss.Steampunk
{
    public class NikolaTeslaCircleHomingAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string ReadyAnimationName = "PrepareSingleExecutionBegin";
        private static readonly string WaitAnimationName = "PrepareSingleExecutionRepeat";
        private static readonly string AttackAnimationName = "SingleExecutionAction";
        private static readonly string EndAnimationName = "SingleExecutionEnd";

        private static readonly float DAMAGE_COEFFICIENT = 1.0f;

        private const string HomingProjectilePath = "Stages/Projectiles/LightningRadius0_4.prefab";
        private static readonly int HomingProjectileAmount = 8;
        private static readonly float HomingProjectileRadius = 0.4f;
        private static readonly float HomingProjectileSpeed = 15.0f;
        private static readonly float HomingPower = 2.0f;
        private static readonly float HomingProjectileLifeTime = 8.0f;
        private static readonly int HomingProjectileHitChance = 1;

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
                this.TryMultipleCircleFiring(stage, _owner.CenterPos);
            });

        }


        public override ActionBase End(Stage stage)
        {
            return null;
        }

        private void TryMultipleCircleFiring(Stage stage, Vector2 firePosition)
        {
            for (int i = 0; i < HomingProjectileAmount; i++)
            {
                Vector2 dir = Quaternion.Euler(0.0f, 0.0f, 360f / HomingProjectileAmount * i) * Vector2.up;
                stage.CreateHomingProjectileAreaEffectObject(
                HomingProjectilePath,
                _owner,
                _target,
                firePosition,
                dir,
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
}

