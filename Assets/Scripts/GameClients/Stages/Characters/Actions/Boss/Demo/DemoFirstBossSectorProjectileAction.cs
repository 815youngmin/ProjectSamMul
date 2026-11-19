using UnityEngine;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;

namespace SamMul.GameClients.Stages.Characters.Actions.Boss.Demo
{
    /// <summary>제자리에서 special_attack 애니메이션을 FireCount 번 재생하며, HitFrame 마다 타겟 방향 부채꼴로 투사체를 쏜다.</summary>
    public class DemoFirstBossSectorProjectileAction : SmartAction<SpriteMonsterAnimationController>
    {
        private static readonly float DAMAGE_COEFFICIENT = 1.0f;

        private static readonly int FireCount = 3;
        private static readonly float SectorAngle = 60f;
        private static readonly int ProjectileAmount = 3;
        private static readonly string ProjectileBodyPath = "Stage/Common/Projectiles/MonsterProjectile_Fireball.prefab";
        private static readonly float ProjectileSpeed = 16f;
        private static readonly float ProjectileAcceleration = 1f;
        private static readonly float ProjectileRadius = 0.4f;
        private static readonly float ProjectileAliveDistance = 50;
        private static readonly float ProjectileKnobackPower = 0.1f;

        private static readonly float ProjectileFireAnimationDuration = 1f;
        private static readonly float ProjectileFireAnimationHitTime = 0.5f;

        private Monster _owner;
        private Character _target;

        public override void Initialize(Monster owner, Character target, SpriteMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
                ProjectileFireAnimationDuration * FireCount,
                animationController);

            _owner = owner;
            _target = target;
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);

            float attackDuration = ProjectileFireAnimationDuration;
            float hitTime = ProjectileFireAnimationHitTime;

            for (int i = 0; i < FireCount; i++)
            {
                float startAt = now + attackDuration * i;

                base.AddOneOffSubAction(startAt, (Stage stage, float deltaTime, float now) =>
                {
                    AnimationController.UpdateBodyDirectionByMoveDirection(_target.Pos - _owner.Pos);
                    AnimationController.PlaySpecialAttack(attackDuration);
                });

                base.AddOneOffSubAction(startAt + hitTime, (Stage stage, float deltaTime, float now) =>
                {
                    Vector2 firePos = _owner.CenterPos;
                    Vector2 fireDirection = (_target.Pos - firePos).normalized;
                    this.TryMultipleDirectionFiring(stage, firePos, fireDirection);
                });
            }
        }

        private void TryMultipleDirectionFiring(Stage stage, Vector2 firePosition, Vector2 fireDirection)
        {
            for (int i = -(ProjectileAmount - 1); i <= ProjectileAmount - 1; i += 2)
            {
                Vector2 dir = Quaternion.Euler(0.0f, 0.0f, 0.5f * (float)i * SectorAngle / ProjectileAmount) * fireDirection;
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
                     isRemovableBySpinBladeObject: true,
                     hitSoundPrefabPath: string.Empty
                     );
            }
        }

        public override ActionBase End(Stage stage)
        {
            return null;
        }
    }
}
