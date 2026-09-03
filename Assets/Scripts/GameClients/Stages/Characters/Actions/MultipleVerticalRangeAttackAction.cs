using UnityEngine;
using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.Monsters;

namespace Z.GameClients.Stages.Characters.Actions
{
    public class MultipleVerticalRangeAttackAction : ActionBase
    {
        private readonly float ProjectileSpeed = 10.0f;
        private readonly float AliveDistance = 100.0f;


        private readonly Character _target = null;
        private readonly Monster _owner = null;

        private readonly float _hitTimeOnAttackAnimation;
        private float _hitFrameAt;
        private Vector2 _attackDirection;
        private string _projectilePrefabPath;
        private int _currentFireCount;

        private int _projectileAmount;      //3
        private float _firePeriod;          //0.2f
        private bool _isRemovableBySpinBladeObject;
        private bool _withIndicator;

        private new MonsterAnimationController _animationController => (MonsterAnimationController)base._animationController;

        public MultipleVerticalRangeAttackAction(
            Character target,
            Monster owner,
            int projectileAmount,
            float firePeriod,
            bool isRemovableBySpinBladeObject,
            bool withIndicator,
            string projectileBodyPrefabPath,
            MonsterAnimationController animationController)
            : this(owner, target.CenterPos - owner.CenterPos, projectileAmount, firePeriod, isRemovableBySpinBladeObject, withIndicator, projectileBodyPrefabPath, animationController)
        {
            Debug.Assert(target != null);
            _target = target;
        }

        public MultipleVerticalRangeAttackAction(
            Monster owner,
            Vector2 attackDirection,
            int projectileAmount,
            float firePeriod,
            bool isRemovableBySpinBladeObject,
            bool withIndicator,
            string projectileBodyPrefabPath,
            MonsterAnimationController animationController)
            : base(ActionType.Attack, -1, animationController)
        {
            _owner = owner;
            _hitTimeOnAttackAnimation = animationController.FindHitTimeOnAttackAnimation();
            _hitFrameAt = 0f;
            _attackDirection = attackDirection;
            _projectilePrefabPath = projectileBodyPrefabPath;
            _currentFireCount = 0;

            _projectileAmount = projectileAmount;
            _firePeriod = firePeriod;
            _isRemovableBySpinBladeObject = isRemovableBySpinBladeObject;
            _withIndicator = withIndicator;
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);

            _animationController.SkipHitAnimation(true);
            _hitFrameAt = now + _hitTimeOnAttackAnimation;

            if (_target != null)
            {
                _attackDirection = _target.CenterPos - _owner.CenterPos;
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

            if (_withIndicator)
            {
                float attackAngle = Mathf.Rad2Deg * Mathf.Atan2(_attackDirection.y, _attackDirection.x);
                stage.CreateSquareAttackRangeIndicator(
                    center: _owner.CenterPos + 0.5f * AliveDistance * _attackDirection,
                    width: AliveDistance,
                    height: 1.0f,
                    angle: attackAngle,
                    duration: 1.0f);
                stage.CreateDirectionalSquareRangeIndicator(
                    startPosition: _owner.CenterPos,
                    direction: _attackDirection,
                    thickness: 1.0f,
                    distance: AliveDistance,
                    duration: 1.0f);
            }
        }

        public override void Update(Stage stage, float deltaTime, float now)
        {
            if (_hitFrameAt <= now)
            {
                float damage = _owner.RangeAttackPower;
                _currentFireCount++;

                var projectile = stage.CreateProjectile(
                    _projectilePrefabPath,
                    _owner.Alliance,
                    _owner,
                    damage,
                    knockBackPower: 0.2f,
                    _owner.CenterPos,
                    _attackDirection,
                    ProjectileSpeed,
                    acceleration: 0.0f,
                    collidingRadius: 0.5f,
                    AliveDistance,
                    hitChances: 1,
                    splitCount: 0,
                    _isRemovableBySpinBladeObject,
                    hitSoundPrefabPath: string.Empty);

                if (_projectileAmount <= _currentFireCount)
                {
                    _hitFrameAt = float.MaxValue;
                    _owner.Action.ChangeTo(stage, new IdleAction(_animationController));
                }
                else
                {
                    _hitFrameAt = now + _firePeriod;
                }
            }
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

