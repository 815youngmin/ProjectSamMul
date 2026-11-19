using UnityEngine;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;

namespace SamMul.GameClients.Stages.Characters.Actions.Boss.Demo
{
    /// <summary>제자리에서 special_attack 애니메이션을 한 번 재생하고, 타겟 방향 부채꼴로 울타리(맵 경계)에 튕기는 오브젝트를 쏜다.</summary>
    public class DemoThirdBossReflectionObjectAction : SmartAction<SpriteMonsterAnimationController>
    {
        private static readonly float DAMAGE_COEFFICIENT = 1.0f;

        private const string ProjectileBodyPath = "Stage/Common/Projectiles/MonsterProjectile_Orb2.prefab";
        private static readonly int ProjectileAmount = 4;
        private static readonly float SectorAngle = 150f;
        private static readonly float ProjectileSpeed = 8f;
        private static readonly float ProjectileLifeTime = 10f;
        private static readonly float ProjectileRadius = 0.5f;
        private static readonly float ProjectileKnobackPower = 0.1f;
        private static readonly float ProjectileRotateSpeed = 720f;

        private static readonly float FireAnimationDuration = 1f;
        private static readonly float FireAnimationHitTime = 0.5f;

        private Monster _owner;
        private Character _target;

        public override void Initialize(Monster owner, Character target, SpriteMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
                FireAnimationDuration,
                animationController);

            _owner = owner;
            _target = target;
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);

            AnimationController.UpdateBodyDirectionByMoveDirection(_target.Pos - _owner.Pos);
            AnimationController.PlaySpecialAttack(FireAnimationDuration);

            base.AddOneOffSubAction(now + FireAnimationHitTime, (Stage stage, float deltaTime, float now) =>
            {
                Vector2 firePos = _owner.CenterPos;
                Vector2 fireDirection = (_target.Pos - firePos).normalized;
                this.FireSectorReflectionObject(stage, firePos, fireDirection);
            });
        }

        public override ActionBase End(Stage stage)
        {
            return null;
        }

        private void FireSectorReflectionObject(Stage stage, Vector2 firePos, Vector2 fireDirection)
        {
            var rect = stage.FenceRect != null ? stage.FenceRect.Value : stage.StaticData.GetWalkableArea();

            for (int i = -(ProjectileAmount - 1); i <= ProjectileAmount - 1; i += 2)
            {
                Vector2 dir = Quaternion.Euler(0.0f, 0.0f, 0.5f * (float)i * SectorAngle / ProjectileAmount) * fireDirection;
                stage.CreateReflectionAreaEffectObject(
                    _owner,
                    ProjectileRadius,
                    firePos,
                    dir,
                    ProjectileSpeed,
                    ProjectileRotateSpeed,
                    DAMAGE_COEFFICIENT * _owner.SpecialAttackPower,
                    ProjectileKnobackPower,
                    ProjectileLifeTime,
                    rect,
                    ProjectileBodyPath
                    );
            }
        }
    }
}
