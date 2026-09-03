using UnityEngine;
using SamMul.GameClients.Stages.AreaEffectObjects;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;

namespace SamMul.GameClients.Stages.Characters.Actions.Boss.Western
{
    public class BombMasterDashAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string DashReadyAnimationName = "DashBegin";
        private static readonly string DashRepeatAnimationName = "DashRepeat";
        private static readonly string DashEndAnimationName = "DashEnd";
        private static readonly string BombThrowAnimation = "PeriodicExecutionRepeat";
        private static readonly string BombThrowEndAnimation = "PeriodicExecutionEnd";

        private static readonly float DashDuration = 1f;
        private static readonly float DashSpeed = 20.0f;

        private static readonly float BOMB_DAMAGE_COEFFICIENT = 1.0f;
        private static readonly int BombThrowAmount = 8;
        private static readonly float BombThrowRadius = 5f;
        private static readonly float BombAttackRadius = 1.5f;
        private static readonly float BombExplosionWaitDuration = 1.5f;

        private Monster _owner;
        private Character _target;
        private Vector2 _dashDirection;

        private Animation _dashReadyAnimation;
        private Animation _dashRepeatAnimation;
        private Animation _dashEndAnimation;
        private Animation _bombThrowAnimation;
        private Animation _bombThrowEndAnimation;


        public override void Initialize(Monster owner, Character target, SpineMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
                animationController.FindAnimation(DashReadyAnimationName).Duration +
                DashDuration +
                animationController.FindAnimation(DashEndAnimationName).Duration + 
                animationController.FindAnimation(BombThrowAnimation).Duration + 
                animationController.FindAnimation(BombThrowEndAnimation).Duration,
                animationController);

            _owner = owner;
            _target = target;

            _dashReadyAnimation = AnimationController.FindAnimation(DashReadyAnimationName);
            _dashRepeatAnimation = AnimationController.FindAnimation(DashRepeatAnimationName);
            _dashEndAnimation = AnimationController.FindAnimation(DashEndAnimationName);
            _bombThrowAnimation = AnimationController.FindAnimation(BombThrowAnimation);
            _bombThrowEndAnimation = AnimationController.FindAnimation(BombThrowEndAnimation);
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);
            AnimationController.SetAnimation(BodyAnimationTrack.WholeBody, _dashReadyAnimation, false);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _dashRepeatAnimation, true, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _dashEndAnimation, false, DashDuration);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _bombThrowAnimation, false, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _bombThrowEndAnimation, false, 0f);

            float indicatorAt = now;
            float dashAt = indicatorAt + _dashReadyAnimation.Duration;
            float bombThrowAt = dashAt + DashDuration + _dashEndAnimation.Duration + AnimationController.FindHitTime(_bombThrowAnimation);

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

            base.AddOneOffSubAction(bombThrowAt, (Stage stage, float deltaTime, float now) =>
            {
                for(int i = 0; i < BombThrowAmount; i++)
                {
                    Vector2 throwPosition = _owner.CenterPos + Random.insideUnitCircle * BombThrowRadius;
                    stage.CreateExplosiveAreaEffectObject(AreaEffectType.DynamiteAreaEffectObject,_owner, _owner.CenterPos, throwPosition, BombExplosionWaitDuration, BombAttackRadius, _owner.SpecialAttackPower * BOMB_DAMAGE_COEFFICIENT);
                }
            });
        }


        public override ActionBase End(Stage stage)
        {
            return null;
        }
    }
}

