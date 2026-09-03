using UnityEngine;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;

namespace SamMul.GameClients.Stages.Characters.Actions.Boss.Western
{
    public class BillCodySectorRandomProjectileAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string ReadyAnimationName = "PrepareSingleExecutionBegin2";
        private static readonly string WaitAnimationName = "PrepareSingleExecutionRepeatd2";
        private static readonly string AttackAnimationName = "SingleExecutionAction2";
        private static readonly string EndAnimationName = "SingleExecutionEnd2";

        private static readonly float DAMAGE_COEFFICIENT = 1.0f;
        private static readonly float SectorAngle = 120f;
        private static readonly int ProjectileAmount = 15;
        private static readonly float ProjectileFireDelay = 0.1f;
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

        public override void Initialize(Monster owner, Character target, SpineMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
                animationController.FindAnimation(ReadyAnimationName).Duration +
                animationController.FindAnimation(WaitAnimationName).Duration +
                ProjectileAmount * ProjectileFireDelay +
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
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _endAnimation, false, ProjectileFireDelay * ProjectileAmount);

            for(int i = 0; i < ProjectileAmount; i++)
            {
                float time = now + _readyAnimation.Duration + _waitAnimation.Duration + ProjectileFireDelay * i;
                base.AddOneOffSubAction(time, (Stage stage, float deltaTime, float now) =>
                {
                    Vector2 firePosition = _owner.CenterPos;
                    Vector2 dir = (_target.Pos - _owner.CenterPos).normalized;
                    Vector2 fireDir = Quaternion.Euler(0, 0, Random.Range(-0.5f * SectorAngle, 0.5f * SectorAngle)) * dir;
                    stage.CreateProjectile(
                     ProjectileBodyPath,
                     _owner.Alliance,
                     _owner,
                     _owner.RangeAttackPower * DAMAGE_COEFFICIENT,
                     ProjectileKnobackPower,
                     firePosition,
                     fireDir,
                     ProjectileSpeed,
                     ProjectileAcceleration,
                     ProjectileRadius,
                     ProjectileAliveDistance,
                     hitChances: 1,
                     splitCount: 0,
                     isRemovableBySpinBladeObject: false,
                     hitSoundPrefabPath: string.Empty
                     );
                });
            }
        }

        public override ActionBase End(Stage stage)
        {
            return null;
        }
    }
}

