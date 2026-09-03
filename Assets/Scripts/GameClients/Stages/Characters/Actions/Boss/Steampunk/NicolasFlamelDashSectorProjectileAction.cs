using UnityEngine;
using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.Monsters;
using Z.Animations.Placeholder;
using Animation = Z.Animations.Placeholder.Animation;

namespace Z.GameClients.Stages.Characters.Actions.Boss.Steampunk
{
    public class NicolasFlamelDashSectorProjectileAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string ReadyAnimationName = "DashBegin";
        private static readonly string DashAnimationName = "DashRepeat";
        private static readonly string EndAnimationName = "DashEnd";

        private static readonly float DAMAGE_COEFFICIENT = 1.0f;

        private static readonly float DashCount = 3;
        private static readonly float DashSpeed = 15.0f;
        private static readonly float DashDuration = 1f;

        private static readonly float SectorAngle = 150f;
        private static readonly int ProjectileAmount = 7;
        private static readonly string ProjectileBodyPath = "Stages/Projectiles/LightningRadius0_4.prefab";
        private static readonly float ProjectileSpeed = 15f;
        private static readonly float ProjectileAcceleration = 1f;
        private static readonly float ProjectileRadius = 0.4f;
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
                float dashEndAt = dashAt + DashDuration;

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

                base.AddOneOffSubAction(dashEndAt, (Stage stage, float deltaTime, float now) =>
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
                     isRemovableBySpinBladeObject: false,
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

