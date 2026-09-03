using UnityEngine;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.Characters.Actions
{
    public class MultipleReflectionRangeAttackAction : ActionBase
    {
        private readonly Character _target = null;
        private readonly Monster _owner = null;

        private readonly float _hitTimeOnAttackAnimation;
        private float _hitFrameAt;
        private bool _isHitFired;
        private string _projectilePrefabPath;
        private int _projectileAmount;
        private float _projectileSpeed;
        private float _projectileLifeTime;
        private float _projectileRadius;
        private float _projectileRotateSpeed;

        private static readonly float ProjectileKnobackPower = 0.1f;

        private new MonsterAnimationController _animationController => (MonsterAnimationController)base._animationController;

        public MultipleReflectionRangeAttackAction(
            Character target,
            Monster owner,
            int projectileAmount,
            float projectileSpeed,
            float projectileLifeTime,
            float projectileRadius,
            float projectileRotateSpeed,
            string projectileBodyPrefabPath,
            MonsterAnimationController animationController) :
            base(ActionType.Attack, animationController.AttackAnimationDuration, animationController)
        {
            Debug.Assert(target != null);
            _owner = owner;
            _target = target;
            _projectileAmount = projectileAmount;
            _projectileSpeed = projectileSpeed;
            _projectileLifeTime = projectileLifeTime;
            _projectileRadius = projectileRadius;
            _projectileRotateSpeed = projectileRotateSpeed;
            _hitTimeOnAttackAnimation = animationController.FindHitTimeOnAttackAnimation();
            _hitFrameAt = 0f;
            _isHitFired = false;
            _projectilePrefabPath = projectileBodyPrefabPath;
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);

            _animationController.SkipHitAnimation(true);
            _hitFrameAt = now + _hitTimeOnAttackAnimation;


            _animationController.UpdateBodyDirectionByMoveDirection(_owner.MoveDir);
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

            for (int i = 0; i < _projectileAmount; i ++)
            {
                Vector2 attackDirection = Quaternion.Euler(0,0, 360 / _projectileAmount * i) *  Vector2.up;

                stage.CreateReflectionAreaEffectObject(
                    _owner,
                    _projectileRadius,
                    _owner.CenterPos,
                    attackDirection,
                    _projectileSpeed,
                    _projectileRotateSpeed,
                    damage,
                    ProjectileKnobackPower,
                    _projectileLifeTime,
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
