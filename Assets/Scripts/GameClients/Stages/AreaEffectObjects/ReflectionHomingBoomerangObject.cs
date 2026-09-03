using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class ReflectionHomingBoomerangObject : AreaEffectObjectBase
    {
        public override bool IsAlive => _isAlive && !_owner.Action.IsDead;

        private Monster _owner;
        private Character _target;
        private float _objectRadius;
        private Vector2 _movingDirection;
        private float _movingSpeed;
        private float _rotatingSpeed;
        private float _damage;
        private int  _reflectionCount;

        private Rect _moveRect;

        private GameObject _bodyImage;

        private float _hittedCharacterClearAt;
        private float _attackPeriod;
        private HashSet<Character> _hittedCharacters;
        private int _currentReflectionCount;
        private bool _isAlive;
        private string _bodyPath;


        public void AllocateSharedResources(AreaEffectType areaEffectType, string imagePath)
        {
            base.AllocateSharedResourcesForBase(areaEffectType);
            _bodyPath = imagePath;
            _bodyImage = ResourcePool.Instance.InstantiateFromResource(imagePath);
            _bodyImage.transform.SetParent(this.gameObject.transform);
            _bodyImage.transform.localPosition = Vector3.zero;
        }

        //전달한 reflectionCount 만큼 반사&유도 기믹을 수행하고 보스한테 돌아간다.
        public void Initialize(
            Monster owner,
            Character target,
            float objectRadius,
            Vector2 startPosition,
            Vector2 movingDirection,
            float movingSpeed,
            float rotatingSpeed,
            float damage,
            int reflectionCount,
            float attackPeriod,
            Rect moveRect)
        {
            base.InitializeAreaObject(owner.Alliance);
            _owner = owner;
            _target = target;
            _objectRadius = objectRadius;
            _movingDirection = movingDirection.normalized;
            _movingSpeed = movingSpeed;
            _rotatingSpeed = rotatingSpeed;
            _damage = damage;
            _reflectionCount = reflectionCount;
            _moveRect = moveRect;
            _attackPeriod = attackPeriod;
            this.transform.position = startPosition;
            _hittedCharacters = new HashSet<Character>();

            _currentReflectionCount = 0;
            _isAlive = true;

            if (_rotatingSpeed == 0.0f)
            {
                float angle = Mathf.Rad2Deg * Mathf.Atan2(_movingDirection.y, _movingDirection.x);
                _bodyImage.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.ReflectionHomingBoomerangObject);
            _bodyPath = null;
            _bodyImage = null;
        }

        //전달한 reflectionCount 만큼 반사&유도 기믹을 수행하고 보스한테 돌아간다.
        public void Initialize(
            Monster owner,
            Character target,
            float objectRadius,
            Vector2 startPosition,
            Vector2 movingDirection,
            float movingSpeed,
            float rotatingSpeed,
            float damage,
            int reflectionCount,
            float attackPeriod,
            Rect moveRect,
            string bodyPath)
        {
            base.InitializeAreaObject(owner.Alliance);
            _owner = owner;
            _target = target;
            _objectRadius = objectRadius;
            _movingDirection = movingDirection.normalized;
            _movingSpeed = movingSpeed;
            _rotatingSpeed = rotatingSpeed;
            _damage = damage;
            _reflectionCount = reflectionCount;
            _moveRect = moveRect;
            _attackPeriod = attackPeriod;
            this.transform.position = startPosition;
            _hittedCharacters = new HashSet<Character>();

            _currentReflectionCount = 0;
            _isAlive = true;

            if (_bodyImage != null)
            {
                Debug.LogWarning($"_bodyImage가 이미 생성되어있는데 또 구현하려 합니다. 로직이 잘못되었습니다. 확인이 필요합니다.");
            }
            _bodyPath = bodyPath;
            _bodyImage = ResourcePool.Instance.InstantiateFromResource(bodyPath);
            _bodyImage.transform.SetParent(this.gameObject.transform);
            _bodyImage.transform.localPosition = Vector3.zero;

            if (_rotatingSpeed == 0.0f)
            {
                float angle = Mathf.Rad2Deg * Mathf.Atan2(_movingDirection.y, _movingDirection.x);
                _bodyImage.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            if(_currentReflectionCount < _reflectionCount)
            {
                this.MoveToCurrentPosition(deltaTime);
                this.ReflectionToRect();
            }
            else
            {
                this.MoveToOwnerPosition(deltaTime);
                if(Vector2.Distance(_owner.CenterPos, this.transform.position) < _objectRadius)
                {
                    //오브젝트 충돌 범위내에 owner가 있으면 오브젝트를 죽인다.
                    _isAlive = false;
                }

            }
            this.Rotate(deltaTime);


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
            //해당 타입은 Initialilze단계에서 몸체 이미지를 생성하기 때문에 바디 이미지를 제거 해줘야된다.
            if (AreaEffectObjectType == AreaEffectType.ReflectionHomingBoomerangObject)
            {
                ResourcePool.Instance.PutBackInstance(_bodyPath, _bodyImage);
                _bodyPath = null;
                _bodyImage = null;
            }
            base.PuttingBackToPool();
        }

        private void MoveToCurrentPosition(float deltaTime)
        {
            Vector2 currentPosition = this.transform.position;
            Vector2 nextPosition = currentPosition + _movingDirection * _movingSpeed * deltaTime;
            this.transform.position = nextPosition;
        }

        private void MoveToOwnerPosition(float deltaTime)
        {
            Vector2 currentPosition = this.transform.position;
            Vector2 ownerDir = (_owner.CenterPos - currentPosition).normalized;
            Vector2 nextPosition;

            if(Vector2.Distance(_owner.CenterPos, currentPosition) < _movingSpeed * deltaTime)
            {
                nextPosition = _owner.CenterPos;
            }
            else
            {
                nextPosition = currentPosition + ownerDir * _movingSpeed * deltaTime;
            }

            this.transform.position = nextPosition;
        }

        private void Rotate(float deltaTime)
        {
            if (_rotatingSpeed == 0.0f)
            {
                return;
            }

            _bodyImage.transform.Rotate(0.0f, 0.0f, deltaTime * _rotatingSpeed);
        }

        private void ReflectionToRect()
        {

            Vector2 movePosition = this.transform.position;
            bool isReflecting = false;
            if (movePosition.x < _moveRect.xMin)
            {
                movePosition.x = _moveRect.xMin;
                _movingDirection = _target.Pos - movePosition;
                isReflecting = true;
            }
            else if (movePosition.x > _moveRect.xMax)
            {
                movePosition.x = _moveRect.xMax;
                _movingDirection = _target.Pos - movePosition;
                isReflecting = true;
            }
            else if (movePosition.y > _moveRect.yMax)
            {
                movePosition.y = _moveRect.yMax;
                _movingDirection = _target.Pos - movePosition;
                isReflecting = true;
            }
            else if (movePosition.y < _moveRect.yMin)
            {
                movePosition.y = _moveRect.yMin;
                _movingDirection = _target.Pos - movePosition;
                isReflecting = true;
            }

            if (isReflecting)
            {
                this.transform.position = movePosition;
                _currentReflectionCount++;
                if (_currentReflectionCount >= _reflectionCount)
                {
                    _movingDirection = _owner.CenterPos - movePosition;
                }
                _movingDirection.Normalize();

                if (_rotatingSpeed == 0.0f)
                {
                    float angle = Mathf.Rad2Deg * Mathf.Atan2(_movingDirection.y, _movingDirection.x);
                    _bodyImage.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
                }
            }
        }

    }

}
