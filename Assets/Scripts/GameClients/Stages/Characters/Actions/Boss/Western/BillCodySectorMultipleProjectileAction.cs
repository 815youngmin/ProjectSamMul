using UnityEngine;
using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.Monsters;
using Z.Animations.Placeholder;
using Animation = Z.Animations.Placeholder.Animation;

namespace Z.GameClients.Stages.Characters.Actions.Boss.Western
{
    public class BillCodySectorMultipleProjectileAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string ReadyAnimationName = "PrepareSingleExecutionBegin";
        private static readonly string WaitAnimationName = "PrepareSingleExecutionRepeatd";
        private static readonly string AttackAnimationName = "SingleExecutionAction";
        private static readonly string EndAnimationName = "SingleExecutionEnd";

        private static readonly float PROJECTILE_DAMAGE_COEFFICIENT = 1.0f;

        private static readonly int FireCount = 3;
        private static readonly float SectorAngle = 150f;
        private static readonly float AddRotationToFire = 5f;
        private static readonly int ProjectileAmount = 6;
        private static readonly string ProjectileBodyPath = "Stages/Projectiles/BulletSmallRadius0_2.prefab";
        private static readonly float ProjectileSpeed = 10f;
        private static readonly float ProjectileAcceleration = 1f;
        private static readonly float ProjectileRadius = 0.2f;
        private static readonly float ProjectileAliveDistance = 50;
        private static readonly float ProjectileKnobackPower = 0.1f;

        private Monster _owner;
        private Character _target;

        private Animation _readyAnimation;
        private Animation _waitAnimation;
        private Animation _attackAnimation;
        private Animation _endAnimation;

        private int _currentFireCount = 0;

        public override void Initialize(Monster owner, Character target, SpineMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
                animationController.FindAnimation(ReadyAnimationName).Duration +
                animationController.FindAnimation(WaitAnimationName).Duration +
                animationController.FindAnimation(AttackAnimationName).Duration * FireCount+
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

            float hitTimeOnAttackAnimation = AnimationController.FindHitTime(_attackAnimation);
            _currentFireCount = 0;

            for(int i = 0; i < FireCount; i++)
            {
                float time = now + _readyAnimation.Duration + _waitAnimation.Duration + hitTimeOnAttackAnimation + _attackAnimation.Duration * i;
                base.AddOneOffSubAction(time, (Stage stage, float deltaTime, float now) =>
                {
                    Vector2 dir = (_target.Pos - _owner.CenterPos).normalized;
                    this.TryMultipleDirectionFiring(stage, _owner.CenterPos, dir, _currentFireCount++);
                });
            }
        }

        public override ActionBase End(Stage stage)
        {
            return null;
        }

        private void TryMultipleDirectionFiring(Stage stage, Vector2 firePosition, Vector2 fireDirection, int fireCount)
        {
            for (int i = -(ProjectileAmount - 1); i <= ProjectileAmount - 1; i += 2)
            {
                Vector2 dir = Quaternion.Euler(
                    0.0f, 0.0f, 
                    (0.5f * (float)i * SectorAngle / ProjectileAmount) + fireCount * AddRotationToFire) * fireDirection;
                stage.CreateProjectile(
                     ProjectileBodyPath,
                     _owner.Alliance,
                     _owner,
                     _owner.RangeAttackPower * PROJECTILE_DAMAGE_COEFFICIENT,
                     ProjectileKnobackPower,
                     firePosition,
                     dir,
                     ProjectileSpeed,
                     ProjectileAcceleration,
                     ProjectileRadius,
                     ProjectileAliveDistance,
                     hitChances: 1,
                     splitCount: 0,
                     isRemovableBySpinBladeObject: false,
                     hitSoundPrefabPath: string.Empty
                     );
            }
        }
    }
}

