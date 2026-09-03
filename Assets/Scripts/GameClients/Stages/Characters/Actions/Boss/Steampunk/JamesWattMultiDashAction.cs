using UnityEngine;
using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.Monsters;
using Z.Animations.Placeholder;
using Animation = Z.Animations.Placeholder.Animation;

namespace Z.GameClients.Stages.Characters.Actions.Boss.Steampunk
{
    public class JamesWattMultiDashAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string ReadyAnimationName = "DashBegin";
        private static readonly string DashAnimationName = "Dashrepeat";
        private static readonly string EndAnimationName = "DashEnd";

        private static readonly float DashCount = 3;
        private static readonly float DashSpeed = 15.0f;
        private static readonly float DashDuration = 1.5f;

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
            }
        }

        public override ActionBase End(Stage stage)
        {
            return null;
        }
    }
}

