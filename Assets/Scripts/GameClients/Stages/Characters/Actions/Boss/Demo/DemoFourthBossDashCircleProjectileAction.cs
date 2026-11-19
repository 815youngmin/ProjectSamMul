using UnityEngine;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;

namespace SamMul.GameClients.Stages.Characters.Actions.Boss.Demo
{
    /// <summary>예고 후 타겟 방향으로 한 번 돌진하며, 돌진하는 동안 일정 간격으로 사방에 원형으로 투사체를 쏜다.</summary>
    public class DemoFourthBossDashCircleProjectileAction : SmartAction<SpriteMonsterAnimationController>
    {
        private static readonly float DAMAGE_COEFFICIENT = 1.0f;

        private static readonly float DashReadyDuration = 1f;
        private static readonly float DashDuration = 1.2f;
        private static readonly float DashSpeed = 18.0f;
        private static readonly float DashEnd = 0.2f;

        private static readonly float ProjectileFireDelay = 0.5f;
        private static readonly int ProjectileAmount = 8;
        private static readonly string ProjectileBodyPath = "Stage/Common/Projectiles/MonsterProjectile_Orb.prefab";
        private static readonly float ProjectileSpeed = 8f;
        private static readonly float ProjectileAcceleration = 1f;
        private static readonly float ProjectileRadius = 0.5f;
        private static readonly float ProjectileAliveDistance = 50;
        private static readonly float ProjectileKnobackPower = 0.1f;

        private Monster _owner;
        private Character _target;
        private Vector2 _dashDirection;

        public override void Initialize(Monster owner, Character target, SpriteMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
                DashReadyDuration +
                DashDuration +
                DashEnd,
                animationController);

            _owner = owner;
            _target = target;
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);

            float indicatorAt = now;
            float dashAt = indicatorAt + DashReadyDuration;

            base.AddOneOffSubAction(indicatorAt, (Stage stage, float deltaTime, float now) =>
            {
                stage.CreateDashAttackRangeIndicator(_owner, _target, _owner.CollisionAttackRadius + 2.0f, DashSpeed * DashDuration, DashReadyDuration);
                AnimationController.PlayIdleAttackActionInfinitely();
                AnimationController.UpdateBodyDirectionByMoveDirection(_target.Pos - _owner.Pos);
                _dashDirection = Vector2.zero;
            });

            base.AddDurationalSubAction(dashAt, DashDuration, (Stage stage, float deltaTime, float now) =>
            {
                if (_dashDirection == Vector2.zero)
                {
                    _dashDirection = (_target.Pos - _owner.Pos).normalized;
                    AnimationController.UpdateBodyDirectionByMoveDirection(_dashDirection);
                    AnimationController.PlaySpecialAttack(DashDuration);
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
