using UnityEngine;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.Characters.Actions.Boss.Demo
{
    /// <summary>
    /// 타겟 쪽 바닥에 원형 범위를 예고한 뒤 special_attack 애니메이션으로 내려찍는다.
    /// 범위 안의 적을 타격하고, 그 중심에서 사방으로 분열 오브젝트를 쏜다. 분열 오브젝트는 적이나 울타리에 닿거나 일정 거리를 가면 다시 사방으로 투사체를 뿌린다.
    /// </summary>
    public class DemoFifthBossSplitObjectAction : SmartAction<SpriteMonsterAnimationController>
    {
        private static readonly float SPLITOBJECT_DAMAGE_COEFFICIENT = 1.0f;
        private static readonly float SPLITEDOBJECT_DAMAGE_COEFFICIENT = 1.0f;

        // 예고만 보여 주는 시간. 이 시간이 지나면 공격 애니메이션을 재생한다.
        private static readonly float WaitDuration = 1.5f;
        private static readonly float AttackAnimationDuration = 1f;
        private static readonly float AttackAnimationHitTime = 0.5f;

        private static readonly float MELEE_DAMAGE_COEFFICIENT = 1.0f;
        private static readonly float MeleeAttackRadius = 7f;
        private static readonly Vector2 MeleeAttackOffset = new Vector2(6f, -3f);

        private const string SplitObjectBodyPath = "Stage/Common/Projectiles/MonsterProjectile_Rock.prefab";
        private static readonly int ProjectileAmount = 6;
        private static readonly float ProjectileSpeed = 10;
        private static readonly float ProjectileAliveDistance = 20f;
        private static readonly float ProjectileRadius = 0.8f;

        private const string SplitedProjectileBodyPath = "Stage/Common/Projectiles/MonsterProjectile_Rock2.prefab";
        private static readonly int SplitedProjectileAmount = 6;
        private static readonly float SplitedProjectileRadius = 0.4f;
        private static readonly float SplitedProjectileSpeed = 15f;
        private static readonly float SplitedProjectileAcceleration = 1f;
        private static readonly float SplitedProjectileKnobackPower = 0.1f;
        private static readonly float SplitedProjectileAliveDistance = 45f;
        private static readonly bool IsSpinBladeCollide = true;

        private Monster _owner;
        private Character _target;
        private Vector2 _attackPosition;

        public override void Initialize(Monster owner, Character target, SpriteMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
                WaitDuration + AttackAnimationDuration,
                animationController);

            _owner = owner;
            _target = target;
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);

            float hitAt = now + WaitDuration + AttackAnimationHitTime;

            base.AddOneOffSubAction(now, (Stage stage, float deltaTime, float now) =>
            {
                bool isTargetOnLeft = _target.Pos.x <= _owner.Pos.x;
                _attackPosition = _owner.CenterPos + new Vector2(isTargetOnLeft ? -MeleeAttackOffset.x : MeleeAttackOffset.x, MeleeAttackOffset.y);

                AnimationController.PlayIdleAttackActionInfinitely();
                AnimationController.UpdateBodyDirectionByMoveDirection(_target.Pos - _owner.Pos);
                stage.CreateCircularAttackRangeIndicator(_attackPosition, MeleeAttackRadius, WaitDuration + AttackAnimationHitTime);
            });

            base.AddOneOffSubAction(now + WaitDuration, (Stage stage, float deltaTime, float now) =>
            {
                AnimationController.PlaySpecialAttack(AttackAnimationDuration);
            });

            base.AddOneOffSubAction(hitAt, (Stage stage, float deltaTime, float now) =>
            {
                CircularTargetArea attackArea = new CircularTargetArea(_attackPosition, MeleeAttackRadius);
                CombatSystem.HitOnTargetArea(
                    stage, attackArea, _owner, _owner.SpecialAttackPower * MELEE_DAMAGE_COEFFICIENT,
                    CombatSystem.KnockBackType.Pivot, Vector2.zero,
                    knockBackPower: 0, null, null, hitSoundPrefabPath: string.Empty);

                this.TryCircleFiring(stage, attackArea.Center);
            });
        }

        public override ActionBase End(Stage stage)
        {
            return null;
        }

        private void TryCircleFiring(Stage stage, Vector2 firePosition)
        {
            var rect = stage.FenceRect != null ? stage.FenceRect.Value : stage.StaticData.GetWalkableArea();

            for (int i = 0; i < ProjectileAmount; i++)
            {
                Vector2 dir = Quaternion.Euler(0.0f, 0.0f, 360f / ProjectileAmount * i) * Vector2.up;
                stage.CreateSplitAreaEffectObject(
                    _owner,
                    ProjectileRadius,
                    firePosition,
                    dir,
                    ProjectileSpeed,
                    _owner.SpecialAttackPower * SPLITOBJECT_DAMAGE_COEFFICIENT,
                    ProjectileAliveDistance,
                    SplitObjectBodyPath,
                    rect,
                    SplitedProjectileAmount,
                    SplitedProjectileRadius,
                    SplitedProjectileSpeed,
                    SplitedProjectileAcceleration,
                    _owner.SpecialAttackPower * SPLITEDOBJECT_DAMAGE_COEFFICIENT,
                    SplitedProjectileKnobackPower,
                    SplitedProjectileAliveDistance,
                    IsSpinBladeCollide,
                    SplitedProjectileBodyPath
                    );
            }
        }
    }
}
