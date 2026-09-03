using UnityEngine;
using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.Monsters;
using Z.GameClients.Stages.ProjectileObjects;

namespace Z.GameClients.Stages.Characters.Actions
{
    // 몬스터 전용 액션. 원거리 공격.
    public sealed class MonsterPoisonousRangeAttackAction : ActionBase
    {
        private static readonly float PoisonousAttackPeriod = 0.5f;
        private readonly Character _target = null;
        private readonly Monster _owner = null;

        private readonly float _hitTimeOnAttackAnimation;
        private float _hitFrameAt;
        private bool _isHitFired;
        private Vector2 _attackDirection;
        private string _projectilePrefabPath;

        private float _poisonousAreaEffectLifeTime;
        private float _poisonousAreaEffectRadius;
        private float _projectileLifeTime;

        private new MonsterAnimationController _animationController => (MonsterAnimationController)base._animationController;

        public MonsterPoisonousRangeAttackAction(
            Character target,
            Monster owner,
            string projectileBodyPrefabPath,
            float poisonousAreaEffectLifeTime,
            float poisonousAreaEffectRadius,
            float projectileLifeTime,
            MonsterAnimationController animationController)  :
            base(ActionType.Attack, animationController.AttackAnimationDuration, animationController)
        {
            _target = target;
            _owner = owner;
            _hitTimeOnAttackAnimation = animationController.FindHitTimeOnAttackAnimation();
            _hitFrameAt = 0f;
            _isHitFired = false;
            _projectilePrefabPath = projectileBodyPrefabPath;
            _poisonousAreaEffectLifeTime = poisonousAreaEffectLifeTime;
            _poisonousAreaEffectRadius = poisonousAreaEffectRadius;
            _projectileLifeTime= projectileLifeTime;
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
            else
            {
                //타겟이 없으면 아무 값이나 넣어준다.
                _attackDirection = Vector2.up;
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
            float aliveDistance = _projectileLifeTime * projectileSpeed;
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
                        isRemovableBySpinBladeObject: false,
                        OnHitCharacterHandler,
                        null,
                        OnFinishedHandler,
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

        private bool OnHitCharacterHandler(Stage stage, Character target, Vector2 hitPosition, ProjectileObject attackerProjectile)
        {
            if (target == null)
            {
                return false;
            }

            this.CreateAreaEffect(stage, hitPosition);

            return true;
        }

        private void OnFinishedHandler(Stage stage, ProjectileObject finishedProjectile, bool isHit)
        {
            if(!isHit)
            {
                this.CreateAreaEffect(stage, finishedProjectile.transform.position);
            }
        }
        private void CreateAreaEffect(Stage stage, Vector2 position)
        {
            stage.CreatePoisonousAreaEffect(_owner, position, _poisonousAreaEffectLifeTime, PoisonousAttackPeriod, _owner.RangeAttackPower, _poisonousAreaEffectRadius);
        }

    }

}
