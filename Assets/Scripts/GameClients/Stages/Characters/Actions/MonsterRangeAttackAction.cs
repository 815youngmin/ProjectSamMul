using UnityEngine;
using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.Monsters;

namespace Z.GameClients.Stages.Characters.Actions
{

    // 몬스터 전용 액션. 원거리 공격.
    public sealed class MonsterRangeAttackAction : ActionBase
    {
        private readonly Character _target = null;
        private readonly Monster _owner = null;

        private readonly float _hitTimeOnAttackAnimation;
        private float _hitFrameAt;
        private bool _isHitFired;
        private Vector2 _attackDirection;
        private string _projectilePrefabPath;

        private new MonsterAnimationController _animationController => (MonsterAnimationController)base._animationController;

        public MonsterRangeAttackAction(
            Character target,
            Monster owner,
            string projectileBodyPrefabPath,
            MonsterAnimationController animationController) 
            : this(owner, target.CenterPos - owner.CenterPos, projectileBodyPrefabPath, animationController)
        {
            Debug.Assert(target != null);
            _target = target;
        }

        public MonsterRangeAttackAction(
            Monster owner,
            Vector2 attackDirection,
            string projectileBodyPrefabPath,
            MonsterAnimationController animationController) :
            base(ActionType.Attack, animationController.AttackAnimationDuration, animationController)
        {
            _owner = owner;
            _hitTimeOnAttackAnimation = animationController.FindHitTimeOnAttackAnimation();
            _hitFrameAt = 0f;
            _isHitFired = false;
            _attackDirection = attackDirection;
            _projectilePrefabPath = projectileBodyPrefabPath;
        }



        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);

            _animationController.SkipHitAnimation(true);
            _hitFrameAt = now + _hitTimeOnAttackAnimation;

            if(_target != null)
            {
                _attackDirection = (_target.CenterPos - _owner.CenterPos).normalized;
            }

            if (_owner.MoveDir == Vector2.zero)
            {
                // 이동하지 않고 있을 경우, 공격 방향을 바라보도록 한다.
                _animationController.UpdateBodyDirectionByMoveDirection(_attackDirection);
            }
            else
            {
                _animationController.UpdateBodyDirectionByMoveDirection(_owner.MoveDir);
            }
            _animationController.PlayAttackForce(Duration);

            if (_owner.IsElite)
            {
                float attackDistance = 70.0f;
                float attackAngle = Mathf.Rad2Deg * Mathf.Atan2(_attackDirection.y, _attackDirection.x);
                stage.AreaIndicators.CreateSquareAttackRangeIndicator(
                    _owner.Pos + 0.5f * attackDistance * _attackDirection,
                    attackDistance,
                    2.0f,
                    attackAngle,
                    _hitTimeOnAttackAnimation);
                stage.AreaIndicators.CreateDirectionalIndicator(_owner.Pos, _attackDirection, 15.0f, 2.0f, _hitTimeOnAttackAnimation);
            }
        }

        public override void Update(Stage stage, float deltaTime, float now)
        {
            if (_isHitFired ||
                (now < _hitFrameAt))
            {
                return;
            }

            _isHitFired = true;

            float damage = _owner.RangeAttackPower;
            float projectileSpeed = 7.0f;
            float aliveDistance = 70.0f;
            float collidingRadius = _owner.IsElite ? 1.0f : 0.5f;

            var projectile = stage.CreateProjectile(
                        _projectilePrefabPath,
                        _owner.Alliance,
                        _owner,
                        damage,
                        knockBackPower: 0.2f,
                        _owner.Pos,
                        _attackDirection,
                        projectileSpeed,
                        acceleration: 0.0f,
                        collidingRadius,
                        aliveDistance,
                        hitChances: 1,
                        splitCount: 0,
                        isRemovableBySpinBladeObject: !_owner.IsElite,
                        hitSoundPrefabPath: string.Empty);
        }

        public override ActionBase End(Stage stage)
        {
            _animationController.SkipHitAnimation(false);
            return null;
        }

        public override bool Cancel(Stage stage)
        {
            if (!base.Cancel(stage))
            {
                return false;
            }

            _animationController.SkipHitAnimation(false);
            _animationController.StopAttack();

            return true;
        }
    }

}
