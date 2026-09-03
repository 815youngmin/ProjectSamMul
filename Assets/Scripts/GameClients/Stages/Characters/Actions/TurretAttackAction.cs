using UnityEngine;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;

namespace SamMul.GameClients.Stages.Characters.Actions
{
    public class TurretAttackAction : ActionBase
    {
        private readonly Monster _owner = null;

        private readonly float _hitTimeOnAttackAnimation;
        private float _hitFrameAt;
        private bool _isHitFired;
        private Vector2 _attackDirection;
        private string _projectilePrefabPath;
        private float _startAngle;

        private new MonsterAnimationController _animationController => (MonsterAnimationController)base._animationController;

        public TurretAttackAction(
            Monster owner,
            Vector2 attackDirection,
            float startAngle,
            string projectileBodyPrefabPath,
            MonsterAnimationController animationController) :
            base(ActionType.Attack, animationController.AttackAnimationDuration, animationController)
        {
            _owner = owner;
            _hitTimeOnAttackAnimation = animationController.FindHitTimeOnAttackAnimation();
            _hitFrameAt = 0f;
            _isHitFired = false;
            _startAngle = startAngle;
            _attackDirection = attackDirection;
            _projectilePrefabPath = projectileBodyPrefabPath;
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);

            _animationController.SkipHitAnimation(true);
            _hitFrameAt = now + _hitTimeOnAttackAnimation;

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
            float projectileSpeed = 10.0f;
            float aliveDistance = 100.0f;

            for (int i = 0; i < 3; i++)
            {
                Vector2 attackDirection = Quaternion.Euler(0, 0, _startAngle + (i * 120f)) * _attackDirection;

                var projectile = stage.CreateProjectile(
                    _projectilePrefabPath,
                    _owner.Alliance,
                    _owner,
                    damage,
                    knockBackPower: 0.2f,
                    _owner.Pos,
                    attackDirection,
                    projectileSpeed,
                    acceleration: 0.0f,
                    collidingRadius: 0.5f,
                    aliveDistance,
                    hitChances: 1,
                    splitCount: 0,
                    hitSoundPrefabPath: string.Empty);
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

