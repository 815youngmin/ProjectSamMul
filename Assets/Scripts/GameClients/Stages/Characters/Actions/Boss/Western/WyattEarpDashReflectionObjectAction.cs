using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;
using UnityEngine;
using SamMul.GameClients.Stages.AreaEffectObjects;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.Characters.Actions.Boss.Western
{
    public class WyattEarpDashReflectionObjectAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string DashReadyAnimationName = "DashBegin";
        private static readonly string DashAnimationName = "DashRepeat";
        private static readonly string DashEndAnimationName = "DashEnd";

        private static readonly string AttackReadyAnimationName = "PrepareSingleExecutionBegin";
        private static readonly string AttackAnimationName = "SingleExecutionAction";
        private static readonly string AttackEndAnimationName = "SingleExecutionEnd";

        private static readonly float DashCount = 2;
        private static readonly float DashSpeed = 15f;
        private static readonly float DashDuration = 1.5f;

        private static readonly float DAMAGE_COEFFICIENT = 1.0f;
        private static readonly int ProjectileAmount = 4;
        private static readonly float SectorAngle = 90f;
        private static readonly float ProjectileSpeed = 10;
        private static readonly float ProjectileLifeTime = 8f;
        private static readonly float ProjectileRadius = 0.4f;
        private static readonly float ProjectileKnobackPower = 0.1f;
        private static readonly float ProjectileRotateSpeed = 0f;

        private Monster _owner;
        private Character _target;
        private Vector2 _dashDirection;
        private Vector2 _fireDirection;
        private Bone _fireBone;

        private Animation _dashReadyAnimation;
        private Animation _dashAnimation;
        private Animation _dashEndAnimation;

        private Animation _attackReadyAnimation;
        private Animation _attackAnimation;
        private Animation _attackEndAnimation;


        public override void Initialize(Monster owner, Character target, SpineMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
                (animationController.FindAnimation(DashReadyAnimationName).Duration +
                DashDuration +
                animationController.FindAnimation(DashEndAnimationName).Duration +
                animationController.FindAnimation(AttackReadyAnimationName).Duration +
                animationController.FindAnimation(AttackAnimationName).Duration +
                animationController.FindAnimation(AttackEndAnimationName).Duration) * DashCount,
                animationController);

            _owner = owner;
            _target = target;

            _dashReadyAnimation = AnimationController.FindAnimation(DashReadyAnimationName);
            _dashAnimation = AnimationController.FindAnimation(DashAnimationName);
            _dashEndAnimation = AnimationController.FindAnimation(DashEndAnimationName);

            _attackReadyAnimation = AnimationController.FindAnimation(AttackReadyAnimationName);
            _attackAnimation = AnimationController.FindAnimation(AttackAnimationName);
            _attackEndAnimation = AnimationController.FindAnimation(AttackEndAnimationName);
            
            _fireBone = AnimationController.Body.skeleton.FindBone("fire");
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);
            for (int i = 0; i < DashCount; i++)
            {
                if (i == 0)
                {
                    AnimationController.SetAnimation(BodyAnimationTrack.WholeBody, _dashReadyAnimation, false);
                }
                else
                {
                    AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _dashReadyAnimation, false, 0f);
                }
                AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _dashAnimation, true, 0f);
                AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _dashEndAnimation, false, DashDuration);

                AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _attackReadyAnimation, false, 0f);
                AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _attackAnimation, false, 0f);
                AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _attackEndAnimation, false, 0f);
            }

            for (int i = 0; i < DashCount; i++)
            {
                float indicatorAt = now + 
                                    (_dashReadyAnimation.Duration + _dashEndAnimation.Duration + DashDuration + 
                                    _attackReadyAnimation.Duration + _attackAnimation.Duration + _attackEndAnimation.Duration) * i;

                float dashAt = indicatorAt + _dashReadyAnimation.Duration;
                float dashEndAt = dashAt + DashDuration + _dashEndAnimation.Duration;
                float fireAt = dashEndAt + _attackReadyAnimation.Duration + AnimationController.FindHitTime(_attackAnimation);

                base.AddOneOffSubAction(indicatorAt, (Stage stage, float deltaTime, float now) =>
                {
                    stage.CreateDashAttackRangeIndicator(_owner, _target, _owner.CollisionAttackRadius + 2.0f, DashSpeed * DashDuration, _dashReadyAnimation.Duration);
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

                base.AddOneOffSubAction(dashEndAt, (Stage stage, float deltaTime, float now) =>
                {
                    Vector2 firePosition = _fireBone.GetWorldPosition(_owner.Body.transform);
                    _fireDirection = (_target.CenterPos - firePosition).normalized;
                    AnimationController.UpdateBodyDirectionByMoveDirection(_fireDirection);
                });

                base.AddOneOffSubAction(fireAt, (Stage stage, float deltaTime, float now) =>
                {
                    Vector2 firePosition = _fireBone.GetWorldPosition(_owner.Body.transform);
                    this.FireSectorReflectionObject(stage, firePosition, _fireDirection);
                });
            }
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
                    AreaEffectType.WesternBigBulletReflectionObject,
                    ProjectileRadius,
                    firePos,
                    dir,
                    ProjectileSpeed,
                    ProjectileRotateSpeed,
                    DAMAGE_COEFFICIENT * _owner.SpecialAttackPower,
                    ProjectileKnobackPower,
                    ProjectileLifeTime,
                    rect
                    );
            }
        }

    }
}

