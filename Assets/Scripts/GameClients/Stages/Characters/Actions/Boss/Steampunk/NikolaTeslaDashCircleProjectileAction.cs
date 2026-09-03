using UnityEngine;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;

namespace SamMul.GameClients.Stages.Characters.Actions.Boss.Steampunk
{
    public class NikolaTeslaDashCircleProjectileAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string ReadyAnimationName = "DashBegin";
        private static readonly string DashAnimationName = "DashRepeat";
        private static readonly string EndAnimationName = "DashEnd";

        private static readonly float DAMAGE_COEFFICIENT = 1.0f;

        private static readonly float DashDuration = 1.2f;
        private static readonly float DashSpeed = 18.0f;

        private static readonly float ProjectileFireDelay = 0.5f;
        private static readonly int ProjectileAmount = 8;
        private static readonly string ProjectileBodyPath = "Stages/Projectiles/LightningBallRedRadius0_5.prefab";
        private static readonly float ProjectileSpeed = 8f;
        private static readonly float ProjectileAcceleration = 1f;
        private static readonly float ProjectileRadius = 0.5f;
        private static readonly float ProjectileAliveDistance = 50;
        private static readonly float ProjectileKnobackPower = 0.1f;


        private Monster _owner;
        private Character _target;
        private Vector2 _dashDirection;

        private Animation _readyAnimation;
        private Animation _dashAnimation;
        private Animation _endAnimation;

        public override void Initialize(Monster owner, Character target, SpineMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
                animationController.FindAnimation(ReadyAnimationName).Duration +
                DashDuration +
                animationController.FindAnimation(EndAnimationName).Duration,
                animationController);

            _owner = owner;
            _target = target;

            _readyAnimation = AnimationController.FindAnimation(ReadyAnimationName);
            _dashAnimation = AnimationController.FindAnimation(DashAnimationName);
            _endAnimation = AnimationController.FindAnimation(EndAnimationName);
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);
            AnimationController.SetAnimation(BodyAnimationTrack.WholeBody, _readyAnimation, false);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _dashAnimation, true, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _endAnimation, false, DashDuration);


            float indicatorAt = now;
            float dashAt = indicatorAt + _readyAnimation.Duration;

            base.AddOneOffSubAction(indicatorAt, (Stage stage, float deltaTime, float now) =>
            {
                stage.CreateDashAttackRangeIndicator(_owner, _target, _owner.CollisionAttackRadius + 2.0f, DashSpeed * DashDuration, _readyAnimation.Duration);
                AnimationController.UpdateBodyDirectionByMoveDirection(_target.Pos - _owner.Pos);
                _dashDirection = Vector2.zero;
            });

            base.AddDurationalSubAction(dashAt, DashDuration, (Stage stage, float deltaTime, float now) =>
            {
                if (_dashDirection == Vector2.zero)
                {
                    _dashDirection = (_target.Pos - _owner.Pos).normalized;
                    AnimationController.UpdateBodyDirectionByMoveDirection(_dashDirection);
                }
                _owner.transform.Translate(deltaTime * DashSpeed * _dashDirection, Space.World);
            });

            for (float projectileAt = 0; projectileAt <= DashDuration; projectileAt += ProjectileFireDelay)
            {
                base.AddOneOffSubAction(dashAt + projectileAt, (Stage stage, float deltaTime, float now) =>
                {
                    this.TryMultipleCircleFiring(stage, _owner.CenterPos);
                });
            }

        }

        public override ActionBase End(Stage stage)
        {
            return null;
        }

        private void TryMultipleCircleFiring(Stage stage, Vector2 firePosition)
        {
            for (int i = 0; i < ProjectileAmount; i++)
            {
                Vector2 dir = Quaternion.Euler(0.0f, 0.0f, 360f / ProjectileAmount * i) * Vector2.up;
                stage.CreateProjectile(
                    ProjectileBodyPath,
                    _owner.Alliance,
                    _owner,
                    _owner.RangeAttackPower * DAMAGE_COEFFICIENT,
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

