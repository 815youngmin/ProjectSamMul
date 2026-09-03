using UnityEngine;
using Z.GameClients.Stages.Characters.Monsters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    //충돌 또는 시간이 지나면 반사 투사체로 분열하는 오브젝트
    public class SplitReflectionObject : AreaEffectObjectBase
    {
        public override bool IsAlive => !_isSplited;
        private bool _isSplited;

        private Monster _owner;
        private float _objectRadius;
        private Vector2 _objectMovingDirection;
        private float _objectMovingSpeed;
        private float _objectDamage;
        private float _objectLifeTime;
        private float _splitAt;
        private string _objectBodyPath;

        private Rect _moveRect;

        private GameObject _bodyImage;
        private Collider2D[] _overlappedColliders;
        private RaycastHit2D[] _raycastHits;

        private AreaEffectType _splitObjectType;
        private int _splitObjectAmount;
        private float _splitObjectRadius;
        private float _splitObjectSpeed;
        private float _splitObjectRotatingSpeed;
        private float _splitObjectDamage;
        private float _splitObjectKnobackPower;
        private float _splitObjectLifeTime;
        private string _splitObjectBodyPath;

        public void AllocateSharedResources(AreaEffectType areaEffectType, string imagePath)
        {
            base.AllocateSharedResourcesForBase(areaEffectType);

            _bodyImage = ResourcePool.Instance.InstantiateFromResource(imagePath);
            _bodyImage.transform.SetParent(this.transform);
            _bodyImage.transform.localPosition = Vector2.zero;
            _bodyImage.transform.localScale = Vector2.one;

            _overlappedColliders = new Collider2D[16];
            _raycastHits = new RaycastHit2D[16];
        }

        //Initialize 단계에서 이미지 경로 받아와서 처리해야되는 경우가 생겨 아래 함수 추가 
        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.SplitReflectionObject);

            _bodyImage = null;
            _overlappedColliders = new Collider2D[16];
            _raycastHits = new RaycastHit2D[16];
        }

        public void Initialize(
          Monster owner,
          Vector2 startPosition,
          Vector2 objectMovingDirection,
          float projectileRadius,
          float projectileSpeed,
          float projectileDamage,
          float projectileAliveDistance,
          string projectileBodyPath,
          Rect moveRect,
          int splitAmount,
          float reflectionObjectRadius,
          float reflectionObjectMovingSpeed,
          float reflectionObjectRotatingSpeed,
          float reflectionObjectDamage,
          float reflectionObjectKnobackPower,
          float reflectionObjectLifeTime,
          string reflectionObjectBodyPath
            )
        {
            base.InitializeAreaObject(owner.Alliance);

            _owner = owner;
            _objectRadius = projectileRadius;
            _objectMovingDirection = objectMovingDirection.normalized;
            _objectMovingSpeed = projectileSpeed;
            _objectDamage = projectileDamage;
            _objectLifeTime = projectileAliveDistance / projectileSpeed;

            if (_bodyImage != null)
            {
                Debug.LogWarning($"_bodyImage가 이미 생성되어있는데 또 구현하려 합니다. 로직이 잘못되었습니다. 확인이 필요합니다.");
            }
            _objectBodyPath = projectileBodyPath;
            _bodyImage = ResourcePool.Instance.InstantiateFromResource(_objectBodyPath);
            _bodyImage.transform.SetParent(this.transform);
            _bodyImage.transform.localPosition = Vector2.zero;


            _moveRect = moveRect;
            _splitAt = Time.time + _objectLifeTime;

            _splitObjectType = AreaEffectType.ReflectionObject;
            _splitObjectAmount = splitAmount;
            _splitObjectRadius = reflectionObjectRadius;
            _splitObjectSpeed = reflectionObjectMovingSpeed;
            _splitObjectRotatingSpeed = reflectionObjectRotatingSpeed;
            _splitObjectDamage = reflectionObjectDamage;
            _splitObjectKnobackPower = reflectionObjectKnobackPower;
            _splitObjectLifeTime = reflectionObjectLifeTime;
            _splitObjectBodyPath = reflectionObjectBodyPath;

            _isSplited = false;
            this.transform.position = startPosition;
        }

        public void Initialize(
            Monster owner,
            float objectRadius,
            Vector2 startPosition,
            Vector2 objectMovingDirection,
            float objectMovingSpeed,
            float objectDamage,
            float objectAliveDistance,
            Rect moveRect,
            AreaEffectType splitObjectType,
            int splitObjectAmount,
            float splitObjectRadius,
            float splitObjectSpeed,
            float splitObjectRotatingSpeed,
            float splitObjectDamage,
            float splitObjectKnobackPower,
            float splitObjectLifeTime
            )
        {
            base.InitializeAreaObject(owner.Alliance);

            _owner = owner;
            _objectRadius = objectRadius;
            _objectMovingDirection = objectMovingDirection.normalized;
            _objectMovingSpeed = objectMovingSpeed;
            _objectDamage = objectDamage;
            _objectLifeTime = objectAliveDistance / objectMovingSpeed;
            _moveRect = moveRect;

            _splitAt = Time.time + _objectLifeTime;

            _splitObjectType = splitObjectType;
            _splitObjectAmount = splitObjectAmount;
            _splitObjectRadius = splitObjectRadius;
            _splitObjectSpeed = splitObjectSpeed;
            _splitObjectRotatingSpeed = splitObjectRotatingSpeed;
            _splitObjectDamage = splitObjectDamage;
            _splitObjectKnobackPower = splitObjectKnobackPower;
            _splitObjectLifeTime = splitObjectLifeTime;

            _isSplited = false;
            this.transform.position = startPosition;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            CombatSystem.HandleAreaEffectCollisionWithCharacter(
                areaEffectObject: this,
                position: transform.position,
                radius: _objectRadius,
                direction: _objectMovingDirection,
                speed: _objectMovingSpeed,
                deltaTime: deltaTime,
                overlappedColliders: _overlappedColliders,
                raycastHits: _raycastHits,
                onHitCharacter: (character) =>
                {
                    character.Hitted(stage, _owner, _objectDamage, _objectMovingDirection, transform.position, hitSoundPrefabPath: string.Empty);
                    this.CreateReflectionProjectile(stage);
                    _isSplited = true;
                });

            if (!IsAlive)
            {
                return;
            }

            this.MoveToCurrentPosition(deltaTime);
            this.RotateBodyImageToMoveDirection();
            this.SplitToRect();

            if (_splitAt <= Time.time)
            {
                this.CreateReflectionProjectile(stage);
                _isSplited = true;
            }
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            if (AreaEffectObjectType == AreaEffectType.SplitReflectionObject)
            {
                ResourcePool.Instance.PutBackInstance(_objectBodyPath, _bodyImage);
                _bodyImage = null;
            }
        }

        private void MoveToCurrentPosition(float deltaTime)
        {
            Vector2 currentPosition = this.transform.position;
            Vector2 nextPosition = currentPosition + _objectMovingDirection * _objectMovingSpeed * deltaTime;
            this.transform.position = nextPosition;
        }

        private void RotateBodyImageToMoveDirection()
        {
            Vector2 direction = _objectMovingDirection * _objectMovingSpeed;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _bodyImage.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        /// <summary>
        /// 벽에 부딪히는 경우 분열 투사체를 생성해준다.
        /// </summary>
        private void SplitToRect()
        {
            Vector2 movePosition = this.transform.position;
            bool split = false;
            if (movePosition.x < _moveRect.xMin)
            {
                movePosition.x = _moveRect.xMin;
                split = true;

            }
            else if (movePosition.x > _moveRect.xMax)
            {
                movePosition.x = _moveRect.xMax;
                split = true;
            }
            else if (movePosition.y > _moveRect.yMax)
            {
                movePosition.y = _moveRect.yMax;
                split = true;
            }
            else if (movePosition.y < _moveRect.yMin)
            {
                movePosition.y = _moveRect.yMin;
                split = true;
            }
            this.transform.position = movePosition;

            if (split)
            {
                _splitAt = Time.time;
            }
        }

        private void CreateReflectionProjectile(Stage stage)
        {

            if(_splitObjectType == AreaEffectType.ReflectionObject)
            {
                for (int i = 0; i < _splitObjectAmount; i++)
                {
                    Vector2 projectileDir = Quaternion.Euler(0, 0, 360f / _splitObjectAmount * i) * Vector2.up;

                    stage.CreateReflectionAreaEffectObject(_owner, _splitObjectRadius,
                        this.transform.position, projectileDir, _splitObjectSpeed,
                        _splitObjectRotatingSpeed, _splitObjectDamage, _splitObjectKnobackPower,
                        _splitObjectLifeTime, _moveRect, _splitObjectBodyPath);
                }
            }
            else
            {
                for (int i = 0; i < _splitObjectAmount; i++)
                {
                    Vector2 projectileDir = Quaternion.Euler(0, 0, 360f / _splitObjectAmount * i) * Vector2.up;

                    stage.CreateReflectionAreaEffectObject(_owner, _splitObjectType, _splitObjectRadius,
                        this.transform.position, projectileDir, _splitObjectSpeed,
                        _splitObjectRotatingSpeed, _splitObjectDamage, _splitObjectKnobackPower,
                        _splitObjectLifeTime, _moveRect);
                }
            }


        }
    }
}
