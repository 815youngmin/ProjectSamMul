using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class ReflectionHomingAreaEffectObject : SmartAreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + _lifeTime;

        private Monster _owner;
        private Character _target;
        private float _objectRadius;
        private Vector2 _movingDirection;
        private float _movingSpeed;
        private float _damage;
        private float _lifeTime;
        private float _createdAt;

        private Rect _moveRect;
        private float _rotatingSpeed;

        private string _bodyPath;
        private GameObject _bodyImage;

        private float _hittedCharacterClearAt;
        private float _attackPeriod;
        private HashSet<Character> _hittedCharacters;

        public void AllocateSharedResources(AreaEffectType areaEffectType, string imagePath)
        {
            base.AllocateSharedResourcesForSmartBase(areaEffectType);
            _bodyPath = imagePath;
            _bodyImage = ResourcePool.Instance.InstantiateFromResource(imagePath);
            _bodyImage.transform.SetParent(this.gameObject.transform);
            _bodyImage.transform.localPosition = Vector3.zero;
        }

        public void Initialize(
            Monster owner,
            Character target,
            float objectRadius,
            Vector2 startPosition,
            Vector2 movingDirection,
            float movingSpeed,
            float damage,
            float lifeTime,
            float attackPeriod,
            Rect moveRect,
            float rotatingSpeed)
        {
            base.InitializeAreaObject(owner.Alliance);
            _owner = owner;
            _target = target;
            _objectRadius = objectRadius;
            _movingDirection = movingDirection.normalized;
            _movingSpeed = movingSpeed;
            _damage = damage;
            _lifeTime = lifeTime;
            _moveRect = moveRect;
            _rotatingSpeed = rotatingSpeed;

            _createdAt = Time.time;
            _attackPeriod = attackPeriod;
            this.transform.position = startPosition;
            _hittedCharacters = new HashSet<Character>();
        }

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForSmartBase(AreaEffectType.ReflectionHomingObject);
            _bodyImage = null;
            _bodyPath = null;
        }

        public void Initialize(
            Monster owner,
            Character target,
            string bodyPath,
            float objectRadius,
            Vector2 startPosition,
            Vector2 movingDirection,
            float movingSpeed,
            float damage,
            float lifeTime,
            float attackPeriod,
            Rect moveRect,
            float rotatingSpeed)
        {
            base.InitializeAreaObject(owner.Alliance);
            _owner = owner;
            _target = target;
            _objectRadius = objectRadius;
            _movingDirection = movingDirection.normalized;
            _movingSpeed = movingSpeed;
            _damage = damage;
            _lifeTime = lifeTime;
            _moveRect = moveRect;
            _rotatingSpeed = rotatingSpeed;

            if (_bodyImage != null)
            {
                Debug.LogWarning($"_bodyImage가 이미 생성되어있는데 또 구현하려 합니다. 로직이 잘못되었습니다. 확인이 필요합니다.");
            }
            _bodyPath = bodyPath;
            _bodyImage = ResourcePool.Instance.InstantiateFromResource(_bodyPath);
            _bodyImage.transform.SetParent(this.transform);
            _bodyImage.transform.localPosition = Vector2.zero;

            _createdAt = Time.time;
            _attackPeriod = attackPeriod;
            this.transform.position = startPosition;
            _hittedCharacters = new HashSet<Character>();
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            base.UpdateLogic(stage, deltaTime);
            this.MoveToCurrentPosition(deltaTime);
            this.ReflectionToRect();
            this.RotateBodyImageToMoveDirection(deltaTime);


            if (_hittedCharacterClearAt < Time.time)
            {
                _hittedCharacters.Clear();
                _hittedCharacterClearAt = Time.time + _attackPeriod;
            }

            CircularTargetArea area = new CircularTargetArea(this.transform.position, _objectRadius);
            CombatSystem.HitOnTargetArea(
                stage, area, _owner, _damage, CombatSystem.KnockBackType.Pivot,
                _owner.Pos, 0f, _hittedCharacters, _hittedCharacters
                , null);
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();

            if (AreaEffectObjectType == AreaEffectType.ReflectionHomingObject)
            {
                ResourcePool.Instance.PutBackInstance(_bodyPath, _bodyImage);
                _bodyPath = null;
                _bodyImage = null;
            }

        }

        private void MoveToCurrentPosition(float deltaTime)
        {
            Vector2 currentPosition = this.transform.position;
            Vector2 nextPosition = currentPosition + _movingDirection * _movingSpeed * deltaTime;
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
            if (_rotatingSpeed == 0.0f)
            {
                Vector2 direction = _movingDirection * _movingSpeed;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                _bodyImage.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
            else
            {
                _bodyImage.transform.Rotate(0.0f, 0.0f, deltaTime * _rotatingSpeed);
            }
        }
    }
}

