using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class MonsterInfinitySpinBladeObject : SmartAreaEffectObjectBase
    {
        public override bool IsAlive => !_owner.Action.IsDead && Time.time <= _createdAt + _lifeTime;

        private Character _owner;
        private float _damage;

        private float _createdAt;
        private float _lifeTime;

        private GameObject _body;

        private Vector2 _startLocalPosition;

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

        private readonly float _hittedCharactersClearPeriod = 0.25f;
        private float _hittedCharactersClearAt;
        private HashSet<Character> _hittedCharacters;
        private Vector2 _offsetPosition;

        private string _bodyPath;

        //생상 종류가 여러 종류라 초기화 단계에서 몸통을 생성해준다.
        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForSmartBase(AreaEffectType.MonsterInfinitySpinBladeObject);
            _bodyPath = null;
            _body = null;
        }

        public void Initialize(
            Character owner,
            string bodyPath,
            float damage,
            Vector2 startLocalPosition,
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
            _bodyPath = bodyPath;
            _damage = damage;
            _startLocalPosition = startLocalPosition;
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

            _createdAt = Time.time;

            _hittedCharacters = new HashSet<Character>();
            _hittedCharactersClearAt = 0f;

            if(_body == null)
            {
                _body = ResourcePool.Instance.InstantiateFromResource(_bodyPath);
                _body.transform.SetParent(this.transform);
                _body.transform.localPosition = _startLocalPosition;
            }
        }
        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            base.UpdateLogic(stage, deltaTime);

            float now = Time.time;
            _currentAngle += _angleUpSpeed * deltaTime;
            _currentRadius += _radiusUpSpeed * deltaTime;
            _nextAngle = _currentAngle + _angleUpSpeed * deltaTime;
            _nextRadius = _currentRadius + _radiusUpSpeed * deltaTime;
            _angleUpSpeed += _angleUpAcceleration * deltaTime;
            _radiusUpSpeed += _radiusUpAcceleration * deltaTime;

            if (_currentRadius > _maxRadius)
            {
                _currentRadius = _maxRadius;
            }
            if (_angleUpSpeed > _maxAngleUpSpeed)
            {
                _angleUpSpeed = _maxAngleUpSpeed;
            }
            if (_radiusUpSpeed > _maxRadiusUpSpeed)
            {
                _radiusUpSpeed = _maxRadiusUpSpeed;
            }

            Vector2 anglePos = Quaternion.Euler(0, 0, _currentAngle) * Vector2.left * _currentRadius;
            Vector2 offsetAnglePos = Quaternion.Euler(0, 0, _currentAngle) * _offsetPosition;

            Vector2 nextAnglePos = Quaternion.Euler(0, 0, _nextAngle) * Vector2.left * _nextRadius;
            Vector2 nextOffsetAnglePos = Quaternion.Euler(0, 0, _nextAngle) * _offsetPosition;

            this.RotateBody(_startLocalPosition + anglePos + offsetAnglePos, _startLocalPosition + nextAnglePos + nextOffsetAnglePos, deltaTime);
            this.transform.position = _owner.CenterPos + _startLocalPosition + anglePos + offsetAnglePos;

            if(_hittedCharactersClearAt + _hittedCharactersClearPeriod < now)
            {
                _hittedCharactersClearAt = now;
                _hittedCharacters.Clear();
            }

            CircularTargetArea area = new CircularTargetArea(this.transform.position, _attackRadius);
            CombatSystem.HitOnTargetArea(
                stage, area, _owner, _damage, CombatSystem.KnockBackType.Pivot,
                _owner.Pos, 0f, _hittedCharacters, _hittedCharacters, null);
        }

        private void RotateBody(Vector2 currentPosition, Vector2 nextPosition, float deltaTime)
        {
            if (_bodyRotatingSpeed == 0.0f)
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
            if(_body != null )
            {
                ResourcePool.Instance.PutBackInstance(_bodyPath, _body);
                _bodyPath = null;
                _body = null;
            }
        }
    }

}
