using UnityEngine;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.UnityHelpers;

namespace SamMul.GameClients.Stages.Characters.Actions
{
    public class ForwardAreaAttackAction : SmartAction<SpriteMonsterAnimationController>
    {
        private Monster _owner;
        private Character _target;
        private float _attackHitTime;

        public override void Initialize(Monster owner, Character target, SpriteMonsterAnimationController animationController)
        {
            base.InitializeBase(
                ActionType.Attack,
                animationController.AttackAnimationDuration,
                animationController);

            _owner = owner;
            _target = target;
            _attackHitTime = AnimationController.FindHitTimeOnAttackAnimation();
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);
            _owner.StopMovement();

            float distance = _owner.StaticData.Param1;
            float width = _owner.StaticData.Param2;
            Vector2 size = new Vector2(distance, width);
            Vector2 direction = (_target.Pos - _owner.CenterPos).normalized;
            Vector2 center = _owner.CenterPos + 0.5f * distance * direction;
            float angle = Mathf.Rad2Deg * Mathf.Atan2(direction.y, direction.x);
            var targetArea = new SquareTargetArea(center, size, angle);

            AnimationController.PlayAttackForce(AnimationController.AttackAnimationDuration);
            AnimationController.UpdateBodyDirectionByMoveDirection(direction);

            base.AddOneOffSubAction(now + _attackHitTime, (Stage stage, float deltaTime, float now) =>
            {
                CombatSystem.HitOnTargetArea(stage, targetArea, _owner, _owner.SpecialAttackPower, CombatSystem.KnockBackType.Pivot, _owner.CenterPos, 0.0f, null, null, null);
                UnityGlobal.SpriteAnimations.CreateAndPlaySpriteAnimation(_owner.StaticData.SpecialAttack1ResourcePath, center, direction, Vector2.one, flipX: false, flipY: direction.x < 0.0f, null, null, null);
            });
        }
    }
}
