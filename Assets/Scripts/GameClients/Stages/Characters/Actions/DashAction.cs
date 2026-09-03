using UnityEngine;
using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.Monsters;

namespace Z.GameClients.Stages.Characters.Actions
{
    public class DashAction : ActionBase
    {
        private SpriteMonsterAnimationController AnimationController => (SpriteMonsterAnimationController)base._animationController;

        private readonly Monster _owner;
        private readonly Character _target;
        private readonly float _dashSpeed;
        private readonly float _dashPreDelay;
        private readonly float _dashDuration;

        private float _startDashAt;
        private float _endDashAt;
        private Vector2 _dashDirection;
        private bool _isDashing;

        public DashAction(
            Monster owner,
            Character target,
            SpriteMonsterAnimationController animationController,
            float dashSpeed,
            float dashPreDaly,
            float dashDuration,
            float dashPostDelay)
            : base(ActionType.Skill, dashPreDaly + dashDuration + dashPostDelay, animationController)
        {
            _owner = owner;
            _target = target;
            _dashSpeed = dashSpeed;
            _dashPreDelay = dashPreDaly;
            _dashDuration = dashDuration;
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);

            _startDashAt = now + _dashPreDelay;
            _endDashAt = _startDashAt + _dashDuration;
            _dashDirection = (_target.Pos - _owner.Pos).normalized;
            _isDashing = false;

            AnimationController.UpdateBodyDirectionByMoveDirection(_dashDirection);
            AnimationController.PlayIdleAttackActionInfinitely();

            float dashDistance = _dashSpeed * _dashDuration;
            float dashAngle = Mathf.Rad2Deg * Mathf.Atan2(_dashDirection.y, _dashDirection.x);
            stage.AreaIndicators.CreateSquareAttackRangeIndicator(
                _owner.Pos + 0.5f * dashDistance * _dashDirection,
                dashDistance,
                2.0f * _owner.CollisionAttackRadius,
                dashAngle,
                _dashPreDelay);
            stage.AreaIndicators.CreateDirectionalIndicator(_owner.Pos, _dashDirection, dashDistance, 2.0f * _owner.CollisionAttackRadius, _dashPreDelay);
        }

        public override void Update(Stage stage, float deltaTime, float now)
        {
            if (_startDashAt < now && now < _endDashAt)
            {
                if (!_isDashing)
                {
                    AnimationController.PlayAttackForce(AnimationController.AttackAnimationDuration);
                    _isDashing = true;
                }

                Vector2 deltaMovement = deltaTime * _dashSpeed * _dashDirection;
                _owner.transform.Translate(deltaMovement);
            }
            else if (_endDashAt <= now)
            {
                if (_isDashing)
                {
                    _owner.StopMovement();
                    _isDashing = false;
                }
            }
        }

        public override ActionBase End(Stage stage)
        {
            return null;
        }
    }
}
