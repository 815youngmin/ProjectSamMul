using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    /// 왼쪽 방향 (시계 반대방향) 으로 돌아간다. 
    /// 첫 시작은 0도 기준 플레이어 왼쪽
    public class SpinMoveObject : SmartAreaEffectObjectBase
    {
        public override bool IsAlive => !_isHit && Time.time <= _createdAt + _lifeTime;

        private Character _owner;
        private float _damage;

        private float _createdAt;
        private float _lifeTime;
        private bool _isHit;

        private GameObject _body;

        private Vector2 _startPosition;

        private float _currentRadius;
        private float _currentAngle;

        private float _nextRadius;
        private float _nextAngle;

        private float _radiusUpSpeed;
        private float _angleUpSpeed;

        private float _radiusUpAcceleration;
        private float _angleUpAcceleration;

        private float _attackRadius;
        private float _bodyRotatingSpeed;

        private float _maxRadius;
        private float _maxRadiusUpSpeed;
        private float _maxAngleUpSpeed;


        private HashSet<Character> _hittedCharacters;
        private Vector2 _offsetPosition;

        private string _bodyPath;

        public void AllocateSharedResources(AreaEffectType areaEffectType, string bodyPath)
        {
            base.AllocateSharedResourcesForSmartBase(areaEffectType);
            _bodyPath = bodyPath;
            _body = ResourcePool.Instance.InstantiateFromResource(bodyPath);
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;
        }

        public void Initialize(
            Character owner,
            float damage,
            Vector2 startPosition,
            float lifeTime,
            float startRadius,
            float startAngle,
            float radiusUpSpeed,
            float angleUpSpeed,
            float radiusUpAccelration,
            float angleUpAccelration,
            float attackRadius,
            float bodyRotatingSpeed,
            Vector2 offsetPosition,
            float maxRadius,
            float maxRadiusUpSpeed,
            float maxAngleUpSpeed)
        {
            base.InitializeAreaObject(owner.Alliance);

            _owner = owner;
            _damage = damage;
            _startPosition = startPosition;
            _radiusUpSpeed = radiusUpSpeed;
            _angleUpSpeed = angleUpSpeed;
            _lifeTime = lifeTime;
            _currentRadius = startRadius;
            _currentAngle = startAngle;
            _radiusUpAcceleration = radiusUpAccelration;
            _angleUpAcceleration = angleUpAccelration;
            _attackRadius = attackRadius;
            _bodyRotatingSpeed = bodyRotatingSpeed;

            _maxRadius = maxRadius;
            _maxRadiusUpSpeed = maxRadiusUpSpeed;
            _maxAngleUpSpeed = maxAngleUpSpeed;

            _offsetPosition = offsetPosition;

            float now = Time.time;
            _createdAt = now;

            _hittedCharacters = new HashSet<Character>();
            _isHit = false;

            Vector2 anglePos = Quaternion.Euler(0, 0, _currentAngle) * Vector2.left * _currentRadius;
            Vector2 offsetAnglePos = Quaternion.Euler(0, 0, _currentAngle) * _offsetPosition;

            Vector2 nextAnglePos = Quaternion.Euler(0, 0, _nextAngle) * Vector2.left * _nextRadius;
            Vector2 nextOffsetAnglePos = Quaternion.Euler(0, 0, _nextAngle) * _offsetPosition;
            this.RotateBody(_startPosition + anglePos + offsetAnglePos, _startPosition + nextAnglePos + nextOffsetAnglePos, Time.deltaTime);
            this.transform.position = _startPosition + anglePos + offsetAnglePos;
        }

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForSmartBase(AreaEffectType.SpinMoveObject);
            _bodyPath = null;
        }

        public void Initialize(
          Character owner,
          float damage,
          Vector2 startPosition,
          string bodyPath,
          float lifeTime,
          float startRadius,
          float startAngle,
          float radiusUpSpeed,
          float angleUpSpeed,
          float radiusUpAccelration,
          float angleUpAccelration,
          float attackRadius,
          float bodyRotatingSpeed,
          Vector2 offsetPosition,
          float maxRadius,
          float maxRadiusUpSpeed,
          float maxAngleUpSpeed)
        {
            base.InitializeAreaObject(owner.Alliance);

            _owner = owner;
            _damage = damage;
            _startPosition = startPosition;
            _radiusUpSpeed = radiusUpSpeed;
            _angleUpSpeed = angleUpSpeed;
            _lifeTime = lifeTime;
            _currentRadius = startRadius;
            _currentAngle = startAngle;
            _radiusUpAcceleration = radiusUpAccelration;
            _angleUpAcceleration = angleUpAccelration;
            _attackRadius = attackRadius;
            _bodyRotatingSpeed = bodyRotatingSpeed;

            if (_body != null)
            {
                Debug.LogWarning($"_body 이미 생성되어있는데 또 구현하려 합니다. 로직이 잘못되었습니다. 확인이 필요합니다.");
            }
            _bodyPath = bodyPath;
            _body = ResourcePool.Instance.InstantiateFromResource(_bodyPath);
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;

            _maxRadius = maxRadius;
            _maxRadiusUpSpeed = maxRadiusUpSpeed;
            _maxAngleUpSpeed = maxAngleUpSpeed;

            _offsetPosition = offsetPosition;

            float now = Time.time;
            _createdAt = now;

            _hittedCharacters = new HashSet<Character>();
            _isHit = false;


            Vector2 anglePos = Quaternion.Euler(0, 0, _currentAngle) * Vector2.left * _currentRadius;
            Vector2 offsetAnglePos = Quaternion.Euler(0, 0, _currentAngle) * _offsetPosition;

            Vector2 nextAnglePos = Quaternion.Euler(0, 0, _nextAngle) * Vector2.left * _nextRadius;
            Vector2 nextOffsetAnglePos = Quaternion.Euler(0, 0, _nextAngle) * _offsetPosition;
            this.RotateBody(_startPosition + anglePos + offsetAnglePos, _startPosition + nextAnglePos + nextOffsetAnglePos, Time.deltaTime);
            this.transform.position = _startPosition + anglePos + offsetAnglePos;
        }


        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            base.UpdateLogic(stage, deltaTime);

            _currentAngle += _angleUpSpeed * deltaTime;
            _currentRadius += _radiusUpSpeed * deltaTime;
            _nextAngle = _currentAngle + _angleUpSpeed * deltaTime;
            _nextRadius = _currentRadius + _radiusUpSpeed * deltaTime;
            _angleUpSpeed += _angleUpAcceleration * deltaTime;
            _radiusUpSpeed += _radiusUpAcceleration * deltaTime;

            float maxRadius = Mathf.Abs(_maxRadius);
            _currentRadius = Mathf.Clamp(_currentRadius, -maxRadius, maxRadius);

            float maxAngleUpSpeed = Mathf.Abs(_maxAngleUpSpeed);
            _angleUpSpeed = Mathf.Clamp(_angleUpSpeed, -maxAngleUpSpeed, maxAngleUpSpeed);

            float maxRadiusUpSpeed = Mathf.Abs(_maxRadiusUpSpeed);
            _radiusUpSpeed = Mathf.Clamp(_radiusUpSpeed, -maxRadiusUpSpeed, maxRadiusUpSpeed);

            Vector2 anglePos = Quaternion.Euler(0, 0, _currentAngle) * Vector2.left * _currentRadius;
            Vector2 offsetAnglePos = Quaternion.Euler(0, 0, _currentAngle) * _offsetPosition;

            Vector2 nextAnglePos = Quaternion.Euler(0,0, _nextAngle) * Vector2.left * _nextRadius; 
            Vector2 nextOffsetAnglePos = Quaternion.Euler(0, 0, _nextAngle) * _offsetPosition;

            this.RotateBody(_startPosition + anglePos + offsetAnglePos, _startPosition + nextAnglePos + nextOffsetAnglePos, deltaTime);
            this.transform.position = _startPosition + anglePos + offsetAnglePos;

            CircularTargetArea area = new CircularTargetArea(this.transform.position, _attackRadius);
            CombatSystem.HitOnTargetArea(
                stage, area, _owner, _damage, CombatSystem.KnockBackType.Pivot,
                _owner.Pos, 0f, _hittedCharacters, _hittedCharacters, null);

            if(_hittedCharacters.Count > 0 )
            {
                _isHit = true;
            }
        }

        private void RotateBody(Vector2 currentPosition, Vector2 nextPosition, float deltaTime)
        {
            if(_bodyRotatingSpeed == 0.0f)
            {
                Vector2 dir = nextPosition - currentPosition;
                float angle = Mathf.Rad2Deg * Mathf.Atan2(dir.y, dir.x);
                _body.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
            else
            {
                _body.transform.Rotate(0.0f, 0.0f, deltaTime * _bodyRotatingSpeed);
            }
        }


        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            if (AreaEffectObjectType == AreaEffectType.SpinMoveObject)
            {
                ResourcePool.Instance.PutBackInstance(_bodyPath, _body);
                _bodyPath = null;
                _body = null;
            }
        }
    }
}
