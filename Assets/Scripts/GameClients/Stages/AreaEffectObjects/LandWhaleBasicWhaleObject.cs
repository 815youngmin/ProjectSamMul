using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.GameClients.Stages.ProjectileObjects;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class LandWhaleBasicWhaleObject : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + _lifeTime;

        private static readonly float AttackPeriod = 0.25f; 
        private Character _owner;
        private float _damage;

        private float _createdAt;
        private float _lifeTime;

        private GameObject _body;

        private Vector2 _movingDirection;
        private float _movingSpeed;
        private float _knockbackPower;
        private float _objectRadius;
        private string _hitSoundPrefabPath;

        private HashSet<Character> _hittedCharactersInAttackPeriod;
        private float _lastClearedHittedCharactersAt;
        private SkeletonAnimation _skeletonAnimation;
        private MeshRenderer _meshRenderer;
        private Animation _startAnimation;
        private Animation _idleAnimation;
        private Animation _endAnimation;


        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.LandWhaleBasicWhaleObject);
            _body = ResourcePool.Instance.InstantiateFromResource("Stages/SkillEffects/LandWhale/LandWhaleBasicWhaleObject.prefab");
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;

            _hittedCharactersInAttackPeriod = new HashSet<Character>();
            _skeletonAnimation = _body.GetComponent<SkeletonAnimation>();
            _meshRenderer = _body.GetComponent<MeshRenderer>();

            _startAnimation = _skeletonAnimation.skeleton.Data.FindAnimation("start");
            _idleAnimation = _skeletonAnimation.skeleton.Data.FindAnimation("idle");
            _endAnimation = _skeletonAnimation.skeleton.Data.FindAnimation("end");

        }

        public void Initialize(
            Character owner,
            float damage,
            Vector2 startPosition,
            Vector2 movingDirection,
            float movingSpeed,
            float knobackPower,
            float objectRadius,
            float lifeTime,
            string hitSoundPrefabPath
            )
        {
            base.InitializeAreaObject(owner.Alliance);
            float now = Time.time;
            _createdAt = now;
            _owner = owner;
            _damage = damage;

            _movingDirection = movingDirection;
            _movingSpeed = movingSpeed;
            _knockbackPower = knobackPower;
            _objectRadius = objectRadius;
            _lifeTime = lifeTime;
            _hitSoundPrefabPath = hitSoundPrefabPath;

            this.transform.position = startPosition;
            this.transform.localScale = Vector2.one * objectRadius;

            _hittedCharactersInAttackPeriod.Clear();

            _skeletonAnimation.AnimationState.SetAnimation(0, _startAnimation, false);
            _skeletonAnimation.AnimationState.AddAnimation(0, _idleAnimation, true, 0f);
            _skeletonAnimation.AnimationState.AddAnimation(0, _endAnimation, false, lifeTime - _startAnimation.Duration - _endAnimation.Duration);

            _skeletonAnimation.skeleton.ScaleX = movingDirection.x > 0 ? 1 : -1;
            _meshRenderer.sortingOrder = (int)(startPosition.y * -100.0f);
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;
            if (now <= _createdAt + _lifeTime - _endAnimation.Duration)
            {
                this.Move(deltaTime);
            }
            this.RemoveToCollidingProjectile(stage);
            this.HitCheck(stage);
        }

        private void HitCheck(Stage stage)
        {
            if (_lastClearedHittedCharactersAt + AttackPeriod <= Time.time)
            {
                _hittedCharactersInAttackPeriod.Clear();
                _lastClearedHittedCharactersAt = Time.time;
            }

            var targetArea = new CircularTargetArea(this.transform.position, _objectRadius);
            CombatSystem.HitOnTargetArea(
                stage, targetArea, _owner, _damage,
                CombatSystem.KnockBackType.Pivot, targetArea.Center, _knockbackPower,
                _hittedCharactersInAttackPeriod, _hittedCharactersInAttackPeriod, _hitSoundPrefabPath);

        }
        private void Move(float deltaTime)
        {
            this.transform.Translate(deltaTime * _movingSpeed * _movingDirection);
        }

        private void RemoveToCollidingProjectile(Stage stage)
        {
            List<ProjectileObject> projectileObjects = new List<ProjectileObject>();
            stage.FindAliveProjectilesInArea(_owner.Alliance.ToEnemyAlliance(), new CircularTargetArea(this.transform.position, _objectRadius), projectileObjects);

            foreach (var projectile in projectileObjects)
            {
                if (projectile.IsRemovableBySpinBladeObject)
                {
                    stage.ReserveToRemoveProjectile(projectile);
                }
            }
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _hittedCharactersInAttackPeriod.Clear();
            _skeletonAnimation.AnimationState.SetEmptyAnimation(0, 0f);
        }
    }
}
