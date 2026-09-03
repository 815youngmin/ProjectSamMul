using System;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class MjolnirBoomerangAreaEffectObject : AreaEffectObjectBase
    {
        private const string _axePrefabPath = "Stages/AreaEffects/MjolnirBoomerang.prefab";
        private static readonly string ProjectilePath = "Stages/Projectiles/LightningRadius0_4.prefab";
        private static readonly float ProjectileRadius = 0.4f;

        private bool _isAlive;
        public override bool IsAlive => _isAlive;

        private HashSet<Character> _hittedCharactersInAttackPeriod = new HashSet<Character>();
        private float _lastClearedHittedCharactersAt;

        private float _createProjectileAngleDegreeAt;

        private GameObject _bodyImage;

        private Character _owner;
        private float _attackPeriod;

        private Vector2 _pivotPosition;
        private float _remainAngle;
        private float _moveSpeed;
        private float _distanceFromPivot;
        private float _currentAngleDegree;
        private float _projectileDamage;
        private float _projectileAnglePeriod;
        private Action _returnCallBack;
        private float _damage;

        private const float _attackRadius = 1.4f;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.MjolnirBoomerang);
            _bodyImage = ResourcePool.Instance.InstantiateFromResource(_axePrefabPath);
            _bodyImage.transform.SetParent(this.gameObject.transform);
            _bodyImage.transform.localPosition = Vector3.zero;
            _bodyImage.transform.localScale = Vector3.one;
            _bodyImage.transform.localRotation = Quaternion.identity;

            _hittedCharactersInAttackPeriod.Clear();
        }

        public void Initialize(
            Character owner,
            Vector2 startPosition,
            Vector2 targetPosition,
            float moveSpeed,
            float damage,
            float attackPeriod,
            float projectileDamage,
            float projectileAnglePeriod,
            Action returnCallBack
            )
        {
            base.InitializeAreaObject(owner.Alliance);
            _owner = owner;
            _attackPeriod = attackPeriod;
            _moveSpeed = moveSpeed;
            _damage = damage;
            _projectileDamage = projectileDamage;
            _projectileAnglePeriod = projectileAnglePeriod;

            _pivotPosition = (startPosition + targetPosition) * 0.5f;

            Vector2 dirctionFromPivot = (startPosition - _pivotPosition);
            _distanceFromPivot = (dirctionFromPivot).magnitude;

            _remainAngle = 360.0f;
            _currentAngleDegree = Vector3.SignedAngle(Vector3.right, dirctionFromPivot.normalized, Vector3.forward);

            _returnCallBack = returnCallBack;
            _isAlive = true;
            _lastClearedHittedCharactersAt = 0.0f;
            _createProjectileAngleDegreeAt = _currentAngleDegree;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            if (_lastClearedHittedCharactersAt < Time.time)
            {
                _lastClearedHittedCharactersAt = Time.time + _attackPeriod;
                _hittedCharactersInAttackPeriod.Clear();
            }

            this.Attack(stage);

            if (!_isAlive)
            {
                return;
            }
            this.MoveToCurrentPosition(deltaTime);

            if (_createProjectileAngleDegreeAt <= _currentAngleDegree)
            {
                _createProjectileAngleDegreeAt += _projectileAnglePeriod;
                Vector2 boomerangPos = this.transform.position;
                Vector2 projectileDirection = boomerangPos - _pivotPosition;
                projectileDirection.Normalize();

                //투사체 생성
                stage.CreateProjectile(
                  bodyResourcePath: ProjectilePath,
                  alliance: _owner.Alliance,
                  owner: _owner,
                  baseDamage: _projectileDamage,
                  knockBackPower: 0f,
                  spawnPosition: this.transform.position,
                  direction: projectileDirection,
                  speed: 10f,
                  acceleration: 1f,
                  ProjectileRadius,
                  aliveDistance: 30f,
                  hitChances: 1,
                  splitCount: 0,
                  isRemovableBySpinBladeObject: false,
                  hitSoundPrefabPath: string.Empty
                  );

                 stage.CreateProjectile(
                  bodyResourcePath: ProjectilePath,
                  alliance: _owner.Alliance,
                  owner: _owner,
                  baseDamage: _projectileDamage,
                  knockBackPower: 0f,
                  spawnPosition: this.transform.position,
                  direction: -projectileDirection,
                  speed: 10f,
                  acceleration: 1f,
                  ProjectileRadius,
                  aliveDistance: 30f,
                  hitChances: 1,
                  splitCount: 0,
                  isRemovableBySpinBladeObject: false,
                  hitSoundPrefabPath: string.Empty
                  );
            }

            _bodyImage.transform.Rotate(0.0f, 0.0f, 360.0f * 5.0f * deltaTime);

            if (_remainAngle <= 0.0f)
            {
                _isAlive = false;
                _returnCallBack?.Invoke();
            }
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
        }

        private void MoveToCurrentPosition(float deltaTime)
        {
            float dtDistance = deltaTime * _moveSpeed;
            float circumference = _distanceFromPivot * 2.0f * Mathf.PI;

            float deltaDegree = (dtDistance / circumference) * 360.0f;
            _remainAngle -= Mathf.Abs(deltaDegree);
            if (_remainAngle < 0.0f)
            {
                deltaDegree -= (deltaDegree < 0 ? _remainAngle : -_remainAngle);
            }

            _currentAngleDegree += deltaDegree;

            float radian = _currentAngleDegree * Mathf.Deg2Rad;
            var offset = new Vector2(_distanceFromPivot * Mathf.Cos(radian), _distanceFromPivot * Mathf.Sin(radian));
            var nextPosition = _pivotPosition + offset;

            this.transform.position = nextPosition;
        }

        private void Attack(Stage stage)
        {
            CircularTargetArea attackArea = new CircularTargetArea(this.transform.position, _attackRadius);
            CombatSystem.HitOnTargetArea(stage, attackArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, attackArea.Center, 0.0f, _hittedCharactersInAttackPeriod, _hittedCharactersInAttackPeriod, hitSoundPrefabPath: string.Empty);
        }
    }
}
