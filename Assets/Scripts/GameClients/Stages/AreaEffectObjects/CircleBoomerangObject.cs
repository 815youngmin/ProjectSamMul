using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class CircleBoomerangObject : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + _lifeTime;

        private static readonly float AttackPeriod = 0.5f;

        private Character _owner;
        private float _damage;

        private float _createdAt;
        private float _lifeTime;

        private float _moveSpeed;
        private float _bodyRotatingSpeed;
        private float _attackRadius;

        private GameObject _bodyImage;
        private string _bodyPath;

        private float _currentDegree;
        private float _degreeSpeed;

        private Vector2 _startPosition;
        private Vector2 _targetPosition;
        private float _circleRadius;
        private Vector2 _circleCenter;
        private Vector2 _directionTargetToStartPos;

        private HashSet<Character> _hittedCharactersInAttackPeriod = new HashSet<Character>();
        private float _lastClearedHittedCharactersAt;

        private bool _isMoveRight;

        public void AllocateSharedResources(AreaEffectType areaEffectType, string imagePath)
        {
            base.AllocateSharedResourcesForBase(areaEffectType);
            _bodyPath = imagePath;
            _bodyImage = ResourcePool.Instance.InstantiateFromResource(imagePath);
            _bodyImage.transform.SetParent(this.gameObject.transform);
            _bodyImage.transform.localPosition = Vector3.zero;
        }

        public void Initialize(
            Character owner,
            float damage,
            float moveSpeed,
            float bodyRotatingSpeed,
            float attackRadius,
            Vector2 startPosition,
            Vector2 targetPosition,
            bool isMoveRight
            )
        {
            base.InitializeAreaObject(owner.Alliance);
            float now = Time.time;
            _owner = owner;
            _createdAt = now;

            _damage = damage;
            _moveSpeed = moveSpeed;
            _bodyRotatingSpeed = bodyRotatingSpeed;
            _attackRadius = attackRadius;
            _startPosition = startPosition;
            _targetPosition = targetPosition;
            _isMoveRight = isMoveRight;

            _circleRadius = Vector2.Distance(startPosition, targetPosition) * 0.5f;
            _circleCenter = (startPosition + targetPosition) * 0.5f;
            _directionTargetToStartPos = (startPosition - targetPosition).normalized;

            _lifeTime = (2 * Mathf.PI * _circleRadius) / moveSpeed;
            _degreeSpeed = 360f / _lifeTime;
            _currentDegree = 0f;

            this.transform.position = startPosition;

            _hittedCharactersInAttackPeriod.Clear();
            _lastClearedHittedCharactersAt = 0.0f;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;
            this.MoveToCurrentPosition(deltaTime);
            this.Rotate(deltaTime);
            this.Attack(stage, now);
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
        }

        private void MoveToCurrentPosition(float deltaTime)
        {
            if(_isMoveRight)
            {
                _currentDegree += _degreeSpeed * deltaTime;
            }
            else
            {
                _currentDegree -= _degreeSpeed * deltaTime;
            }
            Vector2 offset = Quaternion.Euler(0, 0, _currentDegree) * _directionTargetToStartPos * _circleRadius;
            Vector2 nextPosition = _circleCenter + offset; 

            if (_bodyRotatingSpeed == 0.0f)
            {
                Vector2 currentDirection = (nextPosition - (Vector2)this.transform.position).normalized;
                float angle = Mathf.Rad2Deg * Mathf.Atan2(currentDirection.y, currentDirection.x);
                _bodyImage.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
            this.transform.position = nextPosition;
        }
        private void Rotate(float deltaTime)
        {
            if (_bodyRotatingSpeed == 0.0f)
            {
                return;
            }
            _bodyImage.transform.Rotate(0.0f, 0.0f, deltaTime * _bodyRotatingSpeed);
        }

        private void Attack(Stage stage, float now)
        {
            //공격 맞은 리스트 초기화
            if (_lastClearedHittedCharactersAt < now)
            {
                _lastClearedHittedCharactersAt = now+ AttackPeriod;
                _hittedCharactersInAttackPeriod.Clear();
            }

            CircularTargetArea attackArea = new CircularTargetArea(this.transform.position, _attackRadius);
            CombatSystem.HitOnTargetArea(stage, attackArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, attackArea.Center, 0.0f, _hittedCharactersInAttackPeriod, _hittedCharactersInAttackPeriod, hitSoundPrefabPath: string.Empty);
        }

    }

}
