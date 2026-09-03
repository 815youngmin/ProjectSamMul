using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.Characters.Monsters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class EraMixZeusSpecialReflectionHomingObject : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + LifeTime;

        private static readonly float MainBodyDamageCoefficient = 1.0f;
        private static readonly string MainBodyPath = "Stages/Projectiles/LightningBall2Radius2_0.prefab";
        private static readonly float ObjectRadius = 2f;
        private static readonly float MovingSpeed = 4f;
        private static readonly float LifeTime = 10f;
        private static readonly float AttackPeriod = 0.5f;
        private static readonly float RotatingSpeed = 720f;

        private static readonly float ProjectileDamageCoefficient = 0.2f;
        private static readonly string ProjectileBodyPath = "Stages/Projectiles/LightningRadius0_3.prefab";
        private static readonly int ProjectileAmount = 8;
        private static readonly float ProjectileDelay = 1f;
        private static readonly float ProjectileRadius = 0.3f;
        private static readonly float ProjectileSpeed = 12;
        private static readonly float ProjectileAcceleration = 0.1f;
        private static readonly float ProjectileKnobackPower = 0.1f;
        private static readonly float ProjectileAliveDistance = 100;
        private static readonly bool IsSpinBladeCollide = true;

        private Monster _owner;
        private Character _target;
        private Vector2 _movingDirection;
        private float _damage;
        private float _projectileDamage;
        private float _createdAt;

        private Rect _moveRect;

        private GameObject _bodyImage;

        private float _projectileFireAt;
        private float _hittedCharacterClearAt;
        private HashSet<Character> _hittedCharacters;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.EraMixZeusSpecialReflectionHomingObject);
            _bodyImage = ResourcePool.Instance.InstantiateFromResource(MainBodyPath);
            _bodyImage.transform.SetParent(this.gameObject.transform);
            _bodyImage.transform.localPosition = Vector3.zero;
        }

        public void Initialize(
            Monster owner,
            Character target,
            Vector2 startPosition,
            Vector2 movingDirection,
            Rect moveRect)
        {
            base.InitializeAreaObject(owner.Alliance);
            _owner = owner;
            _target = target;
            _movingDirection = movingDirection.normalized;
            _damage = _owner.SpecialAttackPower * MainBodyDamageCoefficient;
            _moveRect = moveRect;
            _projectileDamage = _owner.SpecialAttackPower * ProjectileDamageCoefficient;

            _createdAt = Time.time;
            _projectileFireAt = _createdAt + ProjectileDelay;
            this.transform.position = startPosition;
            _hittedCharacters = new HashSet<Character>();
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            this.MoveToCurrentPosition(deltaTime);
            this.ReflectionToRect();
            this.RotateBodyImageToMoveDirection(deltaTime);

            float now = Time.time;

            if (_hittedCharacterClearAt < now)
            {
                _hittedCharacters.Clear();
                _hittedCharacterClearAt = now + AttackPeriod;
            }

            CircularTargetArea area = new CircularTargetArea(this.transform.position, ObjectRadius);
            CombatSystem.HitOnTargetArea(
                stage, area, _owner, _damage, CombatSystem.KnockBackType.Pivot,
                _owner.Pos, 0f, _hittedCharacters, _hittedCharacters
                , null);

            if(_projectileFireAt < now)
            {
                this.CircleShapeFire(stage, area.Center);
                _projectileFireAt = now + ProjectileDelay;
            }

        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
        }

        private void MoveToCurrentPosition(float deltaTime)
        {
            Vector2 currentPosition = this.transform.position;
            Vector2 nextPosition = currentPosition + _movingDirection * MovingSpeed * deltaTime;
            this.transform.position = nextPosition;
        }

        private void ReflectionToRect()
        {

            Vector2 movePosition = this.transform.position;
            if (movePosition.x < _moveRect.xMin)
            {
                movePosition.x = _moveRect.xMin;
                _movingDirection = _target.Pos - movePosition;
            }
            else if (movePosition.x > _moveRect.xMax)
            {
                movePosition.x = _moveRect.xMax;
                _movingDirection = _target.Pos - movePosition;
            }
            else if (movePosition.y > _moveRect.yMax)
            {
                movePosition.y = _moveRect.yMax;
                _movingDirection = _target.Pos - movePosition;
            }
            else if (movePosition.y < _moveRect.yMin)
            {
                movePosition.y = _moveRect.yMin;
                _movingDirection = _target.Pos - movePosition;
            }

            _movingDirection.Normalize();
            this.transform.position = movePosition;
        }

        private void RotateBodyImageToMoveDirection(float deltaTime)
        {
            if (RotatingSpeed == 0.0f)
            {
                Vector2 direction = _movingDirection * MovingSpeed;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                _bodyImage.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
            else
            {
                _bodyImage.transform.Rotate(0.0f, 0.0f, deltaTime * RotatingSpeed);
            }

        }


        private void CircleShapeFire(Stage stage, Vector2 firePosition)
        {
            for (int i = 0; i < ProjectileAmount; i++)
            {
                Vector2 dir = Quaternion.Euler(0.0f, 0.0f, 360f / ProjectileAmount * i) * Vector2.up;
                this.FireProjectile(stage, firePosition, dir);
            }
        }


        private void FireProjectile(Stage stage, Vector2 firePosition, Vector2 fireDirection)
        {
            stage.CreateProjectile(
                ProjectileBodyPath,
                _owner.Alliance,
                _owner,
                _projectileDamage,
                ProjectileKnobackPower,
                firePosition,
                fireDirection,
                ProjectileSpeed,
                ProjectileAcceleration,
                ProjectileRadius,
                ProjectileAliveDistance,
                hitChances: 1,
                splitCount: 0,
                IsSpinBladeCollide,
                hitSoundPrefabPath: string.Empty
                );
        }



    }

}

