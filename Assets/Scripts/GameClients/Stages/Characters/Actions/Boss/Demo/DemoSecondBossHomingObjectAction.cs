using UnityEngine;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;

namespace SamMul.GameClients.Stages.Characters.Actions.Boss.Demo
{
    public class DemoSecondBossHomingObjectAction : SmartAction<SpriteMonsterAnimationController>
    {
        private static readonly float DAMAGE_COEFFICIENT = 1.0f;

        private const string HomingProjectilePath = "Stage/Common/Projectiles/MonsterProjectile_Rock.prefab";
        private static readonly int HomingProjectileAmount = 8;
        private static readonly float HomingProjectileRadius = 0.3f;
        private static readonly float HomingProjectileSpeed = 15.0f;
        private static readonly float HomingPower = 1.0f;
        private static readonly float HomingProjectileLifeTime = 8.0f;
        private static readonly int HomingProjectileHitChance = 1;

        private static readonly float ProjectileFireAnimationDuration = 2.0f;
        private static readonly float ProjectileFireAnimationHitTime = 0.5f;

        private Monster _owner;
        private Character _target;

        public override void Initialize(Monster owner, Character target, SpriteMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
                ProjectileFireAnimationDuration,
                animationController);

            _owner = owner;
            _target = target;
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);

            float attackDuration = ProjectileFireAnimationDuration;
            float hitTime = ProjectileFireAnimationHitTime;


            float fireDuration = ProjectileFireAnimationDuration - ProjectileFireAnimationHitTime;
            float time = now + ProjectileFireAnimationHitTime;

            AnimationController.PlaySpecialAttack(ProjectileFireAnimationDuration);
            for(int i = 0; i < HomingProjectileAmount; i++)
            {
                float angle = (i % 2 == 0 ? 1.0f : -1.0f) * (((i / 2) + 1) * 20.0f);
                base.AddOneOffSubAction(time + (fireDuration / HomingProjectileAmount) * i, (Stage stage, float deltaTime, float now) =>
                {
                    Vector2 firePosition = _owner.CenterPos;
                    Vector2 fireDirection = Quaternion.Euler(0.0f, 0.0f, angle) * (_target.CenterPos - firePosition).normalized;
                    this.TryMultipleCircleFiring(stage, firePosition, fireDirection);
                });
            }

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

        public override ActionBase End(Stage stage)
        {
            return null;
        }
    }
}
