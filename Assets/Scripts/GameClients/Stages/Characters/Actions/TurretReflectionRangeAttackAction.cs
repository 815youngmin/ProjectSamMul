using UnityEngine;
using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.Monsters;
using Z.GameClients.Stages.CombatSystems;

namespace Z.GameClients.Stages.Characters.Actions
{
    public class TurretReflectionRangeAttackAction : ActionBase
    {
        private readonly Monster _owner = null;
        private readonly Character _target = null;

        private static readonly float ProjectileRadius = 0.5f;
        private static readonly float ProjectileSpeed = 10f;
        private static readonly float ProjectileLifeTime = 10f;
        private static readonly float ProjectileRotateSpeed = 0f;
        private static readonly float ProjectileKnobackPower = 0.1f;

        private readonly float _hitTimeOnAttackAnimation;
        private float _hitFrameAt;
        private bool _isHitFired;
        private Vector2 _attackDirection;
        private string _projectilePrefabPath;
        private float _startAngle;

        private new MonsterAnimationController _animationController => (MonsterAnimationController)base._animationController;

        public TurretReflectionRangeAttackAction(
            Monster owner,
            Character target,
            Vector2 attackDirection,
            float startAngle,
            string projectileBodyPrefabPath,
            MonsterAnimationController animationController) :
            base(ActionType.Attack, animationController.AttackAnimationDuration, animationController)
        {
            _owner = owner;
            _target = target;
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
            var rect = stage.FenceRect != null ? stage.FenceRect.Value : stage.StaticData.GetWalkableArea();

            for (int i = 0; i < 3; i++)
            {
                Vector2 attackDirection = Quaternion.Euler(0, 0, _startAngle + (i * 120f)) * _attackDirection;

                stage.CreateReflectionAreaEffectObject(
                    _owner,
                    ProjectileRadius,
                    _owner.CenterPos,
                    attackDirection,
                    ProjectileSpeed,
                    ProjectileRotateSpeed,
                    damage,
                    ProjectileKnobackPower,
                    ProjectileLifeTime,
                    rect,
                    _projectilePrefabPath
                    );

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

