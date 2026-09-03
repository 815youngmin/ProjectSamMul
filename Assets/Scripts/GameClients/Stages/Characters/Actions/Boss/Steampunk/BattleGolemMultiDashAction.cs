using UnityEngine;
using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.Monsters;
using Z.Animations.Placeholder;
using Animation = Z.Animations.Placeholder.Animation;

namespace Z.GameClients.Stages.Characters.Actions.Boss.Steampunk
{
    public class BattleGolemMultiDashAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string ReadyAnimationName = "DashBegin";
        private static readonly string Ready2AnimationName = "DashBegin2";
        private static readonly string DashAnimationName = "DashRepeat";
        private static readonly string EndAnimationName = "DashEnd";
        private static readonly string End2AnimationName = "DashEnd2";

        private static readonly float DashSpeed = 15.0f;
        private static readonly float DashDuration = 1.5f;

        private Monster _owner;
        private Character _target;
        private Vector2 _dashDirection;

        private Animation _readyAnimation;
        private Animation _ready2Animation;
        private Animation _dashAnimation;
        private Animation _endAnimation;
        private Animation _end2Animation;

        public override void Initialize(Monster owner, Character target, SpineMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
                animationController.FindAnimation(ReadyAnimationName).Duration + DashDuration + animationController.FindAnimation(EndAnimationName).Duration + 
                animationController.FindAnimation(Ready2AnimationName).Duration + DashDuration + animationController.FindAnimation(EndAnimationName).Duration + 
                animationController.FindAnimation(Ready2AnimationName).Duration + DashDuration + animationController.FindAnimation(End2AnimationName).Duration,
                animationController);

            _owner = owner;
            _target = target;

            _readyAnimation = AnimationController.FindAnimation(ReadyAnimationName);
            _ready2Animation = AnimationController.FindAnimation(Ready2AnimationName);
            _dashAnimation = AnimationController.FindAnimation(DashAnimationName);
            _endAnimation = AnimationController.FindAnimation(EndAnimationName);
            _end2Animation = AnimationController.FindAnimation(End2AnimationName);
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);
            AnimationController.SetAnimation(BodyAnimationTrack.WholeBody, _readyAnimation, false);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _dashAnimation, true, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _endAnimation, false, DashDuration);

            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _ready2Animation, false, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _dashAnimation, true, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _endAnimation, false, DashDuration);            
            
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _ready2Animation, false, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _dashAnimation, true, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _end2Animation, false, DashDuration);

            float dash1IndicatorAt = now;
            base.AddOneOffSubAction(dash1IndicatorAt, (Stage stage, float deltaTime, float now) =>
            {
                stage.CreateDashAttackRangeIndicator(_owner, _target, _owner.CollisionAttackRadius + 2.0f, DashSpeed * DashDuration, _readyAnimation.Duration);
                AnimationController.UpdateBodyDirectionByMoveDirection(_target.Pos - _owner.Pos);
                _dashDirection = Vector2.zero;
            });

            float dash1At = dash1IndicatorAt + _readyAnimation.Duration;
            base.AddDurationalSubAction(dash1At, DashDuration, (Stage stage, float deltaTime, float now) =>
            {
                if (_dashDirection == Vector2.zero)
                {
                    _dashDirection = (_target.Pos - _owner.Pos).normalized;
                    AnimationController.UpdateBodyDirectionByMoveDirection(_dashDirection);
                }
                _owner.transform.Translate(deltaTime * DashSpeed * _dashDirection, Space.World);
            });

            float dash2IndicatorAt = dash1At + DashDuration + _endAnimation.Duration;
            base.AddOneOffSubAction(dash2IndicatorAt, (Stage stage, float deltaTime, float now) =>
            {
                stage.CreateDashAttackRangeIndicator(_owner, _target, _owner.CollisionAttackRadius + 2.0f, DashSpeed * DashDuration, _ready2Animation.Duration);
                AnimationController.UpdateBodyDirectionByMoveDirection(_target.Pos - _owner.Pos);
                _dashDirection = Vector2.zero;
            });

            float dash2At = dash2IndicatorAt + _ready2Animation.Duration;
            base.AddDurationalSubAction(dash2At, DashDuration, (Stage stage, float deltaTime, float now) =>
            {
                if (_dashDirection == Vector2.zero)
                {
                    _dashDirection = (_target.Pos - _owner.Pos).normalized;
                    AnimationController.UpdateBodyDirectionByMoveDirection(_dashDirection);
                }
                _owner.transform.Translate(deltaTime * DashSpeed * _dashDirection, Space.World);
            });

            float dash3IndicatorAt = dash2At + DashDuration + _endAnimation.Duration;
            base.AddOneOffSubAction(dash3IndicatorAt, (Stage stage, float deltaTime, float now) =>
            {
                stage.CreateDashAttackRangeIndicator(_owner, _target, _owner.CollisionAttackRadius + 2.0f, DashSpeed * DashDuration, _ready2Animation.Duration);
                AnimationController.UpdateBodyDirectionByMoveDirection(_target.Pos - _owner.Pos);
                _dashDirection = Vector2.zero;
            });

            float dash3At = dash3IndicatorAt + _ready2Animation.Duration;
            base.AddDurationalSubAction(dash3At, DashDuration, (Stage stage, float deltaTime, float now) =>
            {
                if (_dashDirection == Vector2.zero)
                {
                    _dashDirection = (_target.Pos - _owner.Pos).normalized;
                    AnimationController.UpdateBodyDirectionByMoveDirection(_dashDirection);
                }
                _owner.transform.Translate(deltaTime * DashSpeed * _dashDirection, Space.World);
            });

        }

        public override ActionBase End(Stage stage)
        {
            return null;
        }
    }
}
