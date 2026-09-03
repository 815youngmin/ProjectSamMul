using UnityEngine;
using Z.GameClients.Stages.AreaEffectObjects;
using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.Monsters;
using Z.GameClients.Stages.CombatSystems;
using Z.Animations.Placeholder;
using Animation = Z.Animations.Placeholder.Animation;

namespace Z.GameClients.Stages.Characters.Actions.Boss.Western
{
    public class BillCodyDashProjectileAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string ReadyAnimationName = "DashBegin";
        private static readonly string DashAnimationName = "DashRepeat";
        private static readonly string EndAnimationName = "DashEnd";

        private static readonly float DAMAGE_COEFFICIENT = 1.0f;

        private static readonly float DashDuration = 1.5f;
        private static readonly float DashSpeed = 15.0f;
        private static readonly int DashCount = 2;

        private static readonly float ProjectileFireDelay = 0.5f;
        private static readonly float ProjectileSpeed = 12;
        private static readonly float ProjectileLifeTime = 8f;
        private static readonly float ProjectileRadius = 0.4f;
        private static readonly float ProjectileKnobackPower = 0.1f;
        private static readonly float ProjectileRotateSpeed = 0f;

        private Monster _owner;
        private Character _target;
        private Vector2 _dashDirection;

        private Animation _readyAnimation;
        private Animation _dashAnimation;
        private Animation _endAnimation;

        public override void Initialize(Monster owner, Character target, SpineMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
                (animationController.FindAnimation(ReadyAnimationName).Duration +
                DashDuration +
                animationController.FindAnimation(EndAnimationName).Duration) * DashCount,
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

            for (int i = 0; i < DashCount; i++)
            {
                if (i == 0)
                {
                    AnimationController.SetAnimation(BodyAnimationTrack.WholeBody, _readyAnimation, false);
                }
                else
                {
                    AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _readyAnimation, false, 0f);
                }
                AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _dashAnimation, true, 0f);
                AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _endAnimation, false, DashDuration);
            }


            for (int i = 0; i < DashCount; i++)
            {
                float indicatorAt = now + (_readyAnimation.Duration + _endAnimation.Duration + DashDuration) * i;
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
                        this.FireLeftRightReflectionObject(stage, _owner.CenterPos, _dashDirection);
                    });
                }

            }
        }


        public override ActionBase End(Stage stage)
        {
            return null;
        }

        private void FireLeftRightReflectionObject(Stage stage, Vector2 firePosition, Vector2 dashDir)
        {
            var rect = stage.FenceRect != null ? stage.FenceRect.Value : stage.StaticData.GetWalkableArea();

            Vector2 leftDir = Quaternion.Euler(0, 0, 90f) * dashDir;
            Vector2 rightDir = Quaternion.Euler(0, 0, -90f) * dashDir;

            stage.CreateReflectionAreaEffectObject(
            _owner,
            AreaEffectType.WesternBigBulletReflectionObject,
            ProjectileRadius,
            firePosition,
            leftDir,
            ProjectileSpeed,
            ProjectileRotateSpeed,
            _owner.SpecialAttackPower * DAMAGE_COEFFICIENT,
            ProjectileKnobackPower,
            ProjectileLifeTime,
            rect
            );

            stage.CreateReflectionAreaEffectObject(
            _owner,
            AreaEffectType.WesternBigBulletReflectionObject,
            ProjectileRadius,
            firePosition,
            rightDir,
            ProjectileSpeed,
            ProjectileRotateSpeed,
            _owner.SpecialAttackPower * DAMAGE_COEFFICIENT,
            ProjectileKnobackPower,
            ProjectileLifeTime,
            rect
            );

        }
    }
}

