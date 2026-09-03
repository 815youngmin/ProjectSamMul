using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.Characters.PCs;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;
using SamMul.UnityHelpers;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class ShootingStarAreaEffectObject : AreaEffectObjectBase
    {
        private static readonly string BOOM_SFX_PATH = "Sounds/SoundEffects/PCs/ShootingStarBoom_SFX.prefab";

        public override bool IsAlive => !_isExplosion;

        private PlayerCharacter _owner;
        private float _explosionDamage;
        private float _collisionDamage;
        private float _explosionRadius;
        private float _aliveDistance;
        private float _collisionKnockbackPower;
        private float _explosionKnockbackPower;
        private float _explosionDelay;

        private bool _isHit;
        private bool _isExplosion;

        private GameObject _normalBody;
        private GameObject _transcendBody;
        private Vector2 _movingDirection;
        private float _moveSpeed;
        private float _acceleration;
        private float _movingDistance;

        private float _activateAt;
        private float _hitAt;
        private float _lastClearedHittedCharactersAt;
        private float _attackPeriod;
        private HashSet<Character> _hittedCharactersInAttackPeriod = new HashSet<Character>();

        private float _normalRadius;
        private float _transcendRadius;

        private float _scaleUp;
        private bool _isTranscend;

        private SkeletonAnimation _normalSkeletonAnimation;
        private Animation _normalBeginAnimation;
        private Animation _normalRepeatAnimation;
        private TrailRenderer _normalTrailRenderer;

        private SkeletonAnimation _transcendSkeletonAnimation;
        private Animation _transcendBeginAnimation;
        private Animation _transcendRepeatAnimation;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.ShootingStar);
            _normalBody = ResourcePool.Instance.InstantiateFromResource("Stages/AreaEffects/ShootingStar/ShootingStar_N.prefab");
            _normalBody.transform.SetParent(this.transform);
            _normalBody.transform.localPosition = Vector2.zero;
            _normalBody.transform.localScale = Vector2.one;
            _normalSkeletonAnimation = _normalBody.GetComponentInChildren<SkeletonAnimation>();
            _normalBeginAnimation = _normalSkeletonAnimation.Skeleton.Data.FindAnimation("Begin");
            _normalRepeatAnimation = _normalSkeletonAnimation.Skeleton.Data.FindAnimation("Repeat");
            _normalTrailRenderer = _normalBody.GetComponentInChildren<TrailRenderer>();

            _normalSkeletonAnimation.AnimationState.Data.DefaultMix = 0f;


            _transcendBody = ResourcePool.Instance.InstantiateFromResource("Stages/AreaEffects/ShootingStar/ShootingStar_S.prefab");
            _transcendBody.transform.SetParent(this.transform);
            _transcendBody.transform.localPosition = Vector2.zero;
            _transcendBody.transform.localScale = Vector3.one;
            _transcendSkeletonAnimation = _transcendBody.GetComponentInChildren<SkeletonAnimation>();
            _transcendBeginAnimation = _transcendSkeletonAnimation.Skeleton.Data.FindAnimation("Begin");
            _transcendRepeatAnimation = _transcendSkeletonAnimation.Skeleton.Data.FindAnimation("Repeat");

            _transcendSkeletonAnimation.AnimationState.Data.DefaultMix = 0f;

            _attackPeriod = 0.25f;
            _normalRadius = 0.8f;
            _transcendRadius = 3.5f;

        }

        public void Initialize(
            PlayerCharacter owner,
            Vector2 movingDirection,
            float moveSpeed,
            float acceleration,
            float aliveDistance,
            float collisionDamage,
            float explosionDamage,
            float explosionRadius,
            float explosionDelay,
            float collisionKnockbackPower,
            float explosionKnockbackPower,
            float scaleUp,
            bool isTranscend
            )
        {
            base.InitializeAreaObject(owner.Alliance);
            float now = Time.time;

            _owner = owner;
            _movingDirection = movingDirection.normalized;
            _moveSpeed = moveSpeed;
            _acceleration = acceleration;   
            _aliveDistance = aliveDistance;
            _collisionDamage = collisionDamage;
            _explosionDamage = explosionDamage;
            _explosionRadius = explosionRadius;
            _explosionDelay = explosionDelay;

            _collisionKnockbackPower = collisionKnockbackPower;
            _explosionKnockbackPower = explosionKnockbackPower;

            _isTranscend = isTranscend;

            _isExplosion = false;
            _isHit = false;
            _movingDistance = 0;
            _scaleUp = scaleUp;

            this.transform.localScale = Vector2.one * scaleUp;
            this.transform.position = _owner.CenterPos;

            if(_isTranscend)
            {
                _transcendSkeletonAnimation.AnimationState.SetAnimation(0, _transcendBeginAnimation, false);
                _transcendSkeletonAnimation.AnimationState.AddAnimation(0, _transcendRepeatAnimation, true, 0f);
                _normalBody.gameObject.SetActive(false);
                _transcendBody.gameObject.SetActive(true);

                _activateAt = now + _transcendBeginAnimation.Duration;
            }
            else
            {
                _normalSkeletonAnimation.AnimationState.SetAnimation(0, _normalBeginAnimation, false);
                _normalSkeletonAnimation.AnimationState.AddAnimation(0, _normalRepeatAnimation, false, 0f);

                _normalTrailRenderer.Clear();

                _normalBody.gameObject.SetActive(true);
                _transcendBody.gameObject.SetActive(false);

                _activateAt = now + _normalBeginAnimation.Duration;
            }

            this.RotateBodyImageToMoveDirection();
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;

            if(_activateAt > now)
            {
                return;
            }

            this.MoveToCurrentPosition(deltaTime);
            this.RotateBodyImageToMoveDirection();

            if(_isTranscend)
            {
                this.UpdateTranscendLogic(stage, now);
            }
            else
            {
                this.UpdateNormalLogic(stage, now);
            }

        }

        private void UpdateTranscendLogic(Stage stage, float now)
        {
            if (_lastClearedHittedCharactersAt + _attackPeriod <= now)
            {
                _hittedCharactersInAttackPeriod.Clear();
                _lastClearedHittedCharactersAt = now;
            }

            Vector2 pos = this.transform.position;
            var targetArea = new CircularSectorTargetArea(pos + _movingDirection *  _scaleUp, _movingDirection * -1f, _transcendRadius * _scaleUp, 100f);
            CombatSystem.HitOnTargetArea(
                stage, targetArea, _owner, _collisionDamage,
                CombatSystem.KnockBackType.Direction, _movingDirection, _collisionKnockbackPower,
                _hittedCharactersInAttackPeriod, _hittedCharactersInAttackPeriod, null);

            if (_hittedCharactersInAttackPeriod.Count > 0)
            {
                if (!_isHit)
                {
                    _hitAt = now;
                    _isHit = true;
                }
            }
            if (_isHit)
            {
                //잠시 대기 후 폭발
                if (now >= _hitAt + _explosionDelay)
                {
                    this.Explosion(stage);
                }
            }

            if (_movingDistance >= _aliveDistance && !_isExplosion)
            {
                this.Explosion(stage);
            }
        }

        private void UpdateNormalLogic(Stage stage, float now)
        {
            List<Character> aliveCharacters = new List<Character>();
            List<Character> hitCharacters= new List<Character>();
            
            float radius = _normalRadius * _scaleUp;
            stage.FindAliveCharactersInArea(_owner.Alliance.ToEnemyAlliance(), this.transform.position, radius, radius + 4f, aliveCharacters);

            foreach (var target in aliveCharacters)
            {
                if (!target.Action.IsDead && !target.IsImmuneToHit && IsTargetHit(target, radius))
                {
                    hitCharacters.Add(target);
                }
            }

            if (hitCharacters.Count > 0)
            {
                this.Explosion(stage);
            }
            else
            { 
                var item = stage.FindClosestBreakableItemObjectExceptFence(this.transform.position);
                if (item != null && Vector2.SqrMagnitude(item.transform.position - this.transform.position) <= radius * radius) 
                {
                    this.Explosion(stage);
                }
            }

            if (_movingDistance >= _aliveDistance && !_isExplosion)
            {
                this.Explosion(stage);
            }
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _hittedCharactersInAttackPeriod.Clear();

            _transcendSkeletonAnimation.AnimationState.ClearTracks();
            _transcendSkeletonAnimation.skeleton.SetToSetupPose();
            _transcendSkeletonAnimation.Update(0);

            _normalSkeletonAnimation.AnimationState.ClearTracks();
            _normalSkeletonAnimation.skeleton.SetToSetupPose();
            _normalSkeletonAnimation.Update(0);

        }

        private void MoveToCurrentPosition(float deltaTime)
        {
            _moveSpeed += _acceleration * deltaTime;

            Vector2 currentPosition = this.transform.position;
            Vector2 nextPosition = currentPosition + _movingDirection * deltaTime * _moveSpeed;
            _movingDistance += deltaTime * _moveSpeed;
            this.transform.position = nextPosition;
        }

        private void RotateBodyImageToMoveDirection()
        {
            Vector2 direction = (_movingDirection * _moveSpeed).normalized;
            transform.right = direction;
        }

        private void Explosion(Stage stage)
        {
            _isExplosion = true;

            stage.CreateShootingStarExplosionAreaEffect(_owner,
                _explosionDamage,
                _explosionKnockbackPower,
                _explosionRadius,
                this.transform.position,
                _movingDirection,
                _isTranscend);

            UnityGlobal.Sounds.PlayBySoundPrefab(BOOM_SFX_PATH, transform.position);
        }

        public bool IsTargetHit(Character target, float radius)
        {
            Vector2 hitBoxCenter = target.Pos + target.GetHitBoxOffset();
            Vector2 hitBoxSize = target.GetHitBoxSize();

            Vector2 bouncingClawPos = this.transform.position;

            Vector2 distance = bouncingClawPos - hitBoxCenter;

            float closestX = Mathf.Clamp(distance.x, -hitBoxSize.x / 2, hitBoxSize.x / 2);
            float closestY = Mathf.Clamp(distance.y, -hitBoxSize.y / 2, hitBoxSize.y / 2);

            Vector2 closestPoint = new Vector2(closestX, closestY);
            Vector2 closestDistance = distance - closestPoint;

            return closestDistance.sqrMagnitude <= radius * radius;
        }

    }

}
