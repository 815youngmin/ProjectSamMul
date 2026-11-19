using UnityEngine;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;

namespace SamMul.GameClients.Stages.Characters.Actions.Boss.Demo
{
    public class DemoSecondBossDashAction : SmartAction<SpriteMonsterAnimationController>
    {
        private static readonly float DAMAGE_COEFFICIENT = 1.0f;

        private static readonly float DashReadyDuration = 0.75f;
        private static readonly float DashSpeed = 35.0f;
        private static readonly float DashDuration = 1f;
        private static readonly float DashEnd = 0.2f;

        private static readonly float DashCount = 2;

        private Monster _owner;
        private Character _target;
        private Vector2 _dashDirection;



        public override void Initialize(Monster owner, Character target, SpriteMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
                (DashReadyDuration+
                DashDuration) * DashCount +
                DashEnd,
                animationController);

            _owner = owner;
            _target = target;


        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);


            for (int i = 0; i < DashCount; i++)
            {
                float indicatorAt = now + (DashReadyDuration + DashDuration) * i;
                float dashAt = indicatorAt + DashReadyDuration;

                base.AddOneOffSubAction(indicatorAt, (Stage stage, float deltaTime, float now) =>
                {
                    stage.CreateDashAttackRangeIndicator(_owner, _target, _owner.CollisionAttackRadius + 2.0f, DashSpeed * DashDuration, DashReadyDuration);
                    AnimationController.PlayIdleAttackActionInfinitely();
                    AnimationController.UpdateBodyDirectionByMoveDirection(_target.Pos - _owner.Pos);

                    _dashDirection = Vector2.zero;
                });

                base.AddDurationalSubAction(dashAt, DashDuration, (Stage stage, float deltaTime, float now) =>
                {
                    if (_dashDirection == Vector2.zero)
                    {
                        _dashDirection = (_target.Pos - _owner.Pos).normalized;
                        AnimationController.PlaySpecialAttack(DashDuration);
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

