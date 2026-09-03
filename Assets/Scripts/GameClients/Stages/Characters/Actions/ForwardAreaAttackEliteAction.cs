using UnityEngine;
using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.Monsters;
using Z.GameClients.Stages.CombatSystems;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.Characters.Actions
{
    public class ForwardAreaAttackEliteAction : SmartAction<SpriteMonsterAnimationController>
    {
        private Monster _owner;
        private Character _target;
        private float _attackHitTime;
        private Vector2 _direction;
        public readonly static float ATTACKDURATION = 2.0f;
        private int _attackCount;

        public override void Initialize(Monster owner, Character target, SpriteMonsterAnimationController animationController)
        {
            _attackCount = (int)(ATTACKDURATION / animationController.AttackAnimationDuration);
            base.InitializeBase(
                ActionType.Attack,
                animationController.AttackAnimationDuration * _attackCount,
                animationController);

            _owner = owner;
            _target = target;
            _attackHitTime = AnimationController.FindHitTimeOnAttackAnimation();
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);
            _owner.StopMovement();

            for(int i = 0; i < _attackCount; i++)
            {
                float attackAnimationPlayAt = now + AnimationController.AttackAnimationDuration * i;
                base.AddOneOffSubAction(attackAnimationPlayAt, (Stage stage, float deltaTime, float now) =>
                {
                    _direction = (_target.Pos - _owner.CenterPos).normalized;
                    AnimationController.PlayAttackForce(AnimationController.AttackAnimationDuration);
                    AnimationController.UpdateBodyDirectionByMoveDirection(_direction);
                });

                float attackAt = attackAnimationPlayAt + _attackHitTime;
                base.AddOneOffSubAction(attackAt, (Stage stage, float deltaTime, float now) =>
                {
                    float distance = _owner.StaticData.Param1;
                    float width = _owner.StaticData.Param2;
                    Vector2 size = new Vector2(distance, width);

                    Vector2 center = _owner.CenterPos + 0.5f * distance * _direction;
                    float angle = Mathf.Rad2Deg * Mathf.Atan2(_direction.y, _direction.x);
                    var targetArea = new SquareTargetArea(center, size, angle);

                    CombatSystem.HitOnTargetArea(stage, targetArea, _owner, _owner.SpecialAttackPower, CombatSystem.KnockBackType.Pivot, _owner.CenterPos, 0.0f, null, null, null);
                    UnityGlobal.SpriteAnimations.CreateAndPlaySpriteAnimation(_owner.StaticData.SpecialAttack1ResourcePath, center, _direction, Vector2.one, flipX: false, flipY: _direction.x < 0.0f, null, null, null);
                });
            }
        }
    }
}
