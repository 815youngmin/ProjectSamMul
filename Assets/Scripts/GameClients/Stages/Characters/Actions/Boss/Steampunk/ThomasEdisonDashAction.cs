using UnityEngine;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;

namespace SamMul.GameClients.Stages.Characters.Actions.Boss.Steampunk
{
    public class ThomasEdisonDashAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string ReadyAnimationName = "DashBegin";
        private static readonly string DashAnimationName = "DashRepeat";
        private static readonly string EndAnimationName = "DashEnd";

        private static readonly float DAMAGE_COEFFICIENT = 1.0f;

        private static readonly float DashDuration = 1f;
        private static readonly float DashSpeed = 15.0f;

        private static readonly float AreaEffectCreateDelay = 0.3f;
        private static readonly float AreaEffectCreateDistance = 4f;
        private static readonly float AreaEffectRadius = 2f;
        private static readonly float IndicatorTime = 1f;


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

            for (float areaEffectAt = 0; areaEffectAt<= DashDuration; areaEffectAt += AreaEffectCreateDelay)
            {
                base.AddOneOffSubAction(dashAt + areaEffectAt, (Stage stage, float deltaTime, float now) =>
                {
                    this.CreateAreaEffect(stage, _dashDirection);
                });
            }
        }
        public override ActionBase End(Stage stage)
        {
            return null;
        }

        private void CreateAreaEffect(Stage stage, Vector2 dashDir)
        {
            Vector2 leftDir = Quaternion.Euler(0, 0, 90f) * dashDir;
            Vector2 rightDir = Quaternion.Euler(0, 0, -90f) * dashDir;

            stage.CreateZeusLightningAreaEffectObject(
                _owner, _owner.CenterPos + leftDir * AreaEffectCreateDistance, AreaEffectRadius, DAMAGE_COEFFICIENT * _owner.SpecialAttackPower, delay: 0f, IndicatorTime);

            stage.CreateZeusLightningAreaEffectObject(
                 _owner, _owner.CenterPos + rightDir * AreaEffectCreateDistance, AreaEffectRadius, DAMAGE_COEFFICIENT * _owner.SpecialAttackPower, delay: 0f, IndicatorTime);
        }
    }
}

