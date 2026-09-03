using UnityEngine;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;

namespace SamMul.GameClients.Stages.Characters.Actions.Boss.Alibaba
{
    public class SchaibarDashHomingObjectAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string ReadyAnimationName = "DashBegin";
        private static readonly string DashAnimationName = "DashRepeat";
        private static readonly string EndAnimationName = "DashEnd";

        private static readonly float DAMAGE_COEFFICIENT = 1.0f;

        private static readonly float DashDuration = 1f;
        private static readonly float DashSpeed = 20.0f;

        private const string HomingProjectilePath = "Stages/AreaEffects/Schaibar_Homing.prefab";
        private static readonly int HomingProjectileAmount = 8;
        private static readonly float HomingProjectileRadius = 0.6f;
        private static readonly float HomingProjectileSpeed = 12.0f;
        private static readonly float HomingPower = 1.0f;
        private static readonly float HomingProjectileLifeTime = 8.0f;
        private static readonly int HomingProjectileHitChance = 1;

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

            base.AddOneOffSubAction(dashAt + DashDuration, (Stage stage, float deltaTime, float now) =>
            {
                this.TryMultipleCircleFiring(stage, _owner.CenterPos);
            });

        }

        public override ActionBase End(Stage stage)
        {
            return null;
        }



        private void TryMultipleCircleFiring(Stage stage, Vector2 firePosition)
        {
            for (int i = 0; i < HomingProjectileAmount; i++)
            {
                Vector2 dir = Quaternion.Euler(0.0f, 0.0f, 360f / HomingProjectileAmount * i) * Vector2.up;
                stage.CreateHomingProjectileAreaEffectObject(
                HomingProjectilePath,
                _owner,
                _target,
                firePosition,
                dir,
                _owner.SpecialAttackPower * DAMAGE_COEFFICIENT,
                HomingProjectileRadius,
                HomingProjectileSpeed,
                HomingPower,
                HomingProjectileLifeTime,
                willRotateInMoveDirection: true,
                hitChances: HomingProjectileHitChance
                );
            }
        }

    }
}

