using UnityEngine;
using Z.GameClients.Stages.Characters.Monsters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class ReflectionHomingSplitObject : AreaEffectObjectBase
    {
        public override bool IsAlive => !_isSplited;
        private bool _isSplited;

        private Monster _owner;
        private Transform _targetTransform;
        private string _objectBodyPath;
        private float _objectRadius;
        private Vector2 _objectMovingDirection;
        private float _objectMovingSpeed;
        private float _objectRotatingSpeed;
        private float _objectDamage;
        private float _splitAt;

        private Rect _moveRect;

        private GameObject _bodyImage;
        private Collider2D[] _overlappedColliders;
        private RaycastHit2D[] _raycastHits;

        private string _splitedObjectBodyPath;
        private int _splitedObjectAmount;
        private float _splitedObjectRadius;
        private float _splitedObjectSpeed;
        private float _splitedObjectAcceleration;
        private float _splitedObjectDamage;
        private float _splitedObjectKnobackPower;
        private float _splitedObjectAliveDistance;
        private bool _isSpinBladeCollide;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.ReflectionHomingSplitObject);

            _bodyImage = null;
            _objectBodyPath = null;
            _splitedObjectBodyPath = null;

            _overlappedColliders = new Collider2D[16];
            _raycastHits = new RaycastHit2D[16];
        }

        public void Initialize(
            Monster owner,
            Transform targetTransform,
            float objectRadius,
            Vector2 startPosition,
            Vector2 objectMovingDirection,
            float objectMovingSpeed,
            float objectRotatingSpeed,
            float objectDamage,
            float objectLifeTime,
            string objectBodyPath,
            Rect moveRect,
            int splitedObjectAmount,
            float splitedObjectRadius,
            float splitedObjectSpeed,
            float splitedObjectAcceleration,
            float splitedObjectDamage,
            float splitedObjectKnobackPower,
            float splitedObjectAliveDistance,
            bool isSpinBladeCollide,
            string splitedObjectBodyPath
            )
        {
            base.InitializeAreaObject(owner.Alliance);
            _owner = owner;
            _targetTransform = targetTransform;
            _objectRadius = objectRadius;
            _objectMovingDirection = objectMovingDirection.normalized;
            _objectMovingSpeed = objectMovingSpeed;
            _objectRotatingSpeed = objectRotatingSpeed;
            _objectDamage = objectDamage;
            _moveRect = moveRect;

            if (_bodyImage != null)
            {
                Debug.LogWarning($"_bodyImage가 이미 생성되어있는데 또 구현하려 합니다. 로직이 잘못되었습니다. 확인이 필요합니다.");
            }
            _objectBodyPath = objectBodyPath;
            _bodyImage = ResourcePool.Instance.InstantiateFromResource(_objectBodyPath);
            _bodyImage.transform.SetParent(this.transform);
            _bodyImage.transform.localPosition = Vector2.zero;

            _splitedObjectAmount = splitedObjectAmount;
            _splitedObjectRadius = splitedObjectRadius;
            _splitedObjectSpeed = splitedObjectSpeed;
            _splitedObjectAcceleration = splitedObjectAcceleration;
            _splitedObjectDamage = splitedObjectDamage;
            _splitedObjectKnobackPower = splitedObjectKnobackPower;
            _splitedObjectAliveDistance = splitedObjectAliveDistance;
            _isSpinBladeCollide = isSpinBladeCollide;
            _splitedObjectBodyPath = splitedObjectBodyPath;

            _splitAt = Time.time + objectLifeTime;
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
                    this.CreateSplitedObject(stage);
                    _isSplited = true;
                });

            if (!IsAlive)
            {
                return;
            }

            this.MoveToCurrentPosition(deltaTime);
            this.ReflectionToRect();
            this.Rotate(deltaTime);

            if (_splitAt <= Time.time)
            {
                this.CreateSplitedObject(stage);
                _isSplited = true;
            }
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            if (AreaEffectObjectType == AreaEffectType.ReflectionHomingSplitObject)
            {
                ResourcePool.Instance.PutBackInstance(_objectBodyPath, _bodyImage);
                _bodyImage = null;
                _objectBodyPath = null;
            }
        }

        private void MoveToCurrentPosition(float deltaTime)
        {
            Vector2 currentPosition = this.transform.position;
            Vector2 nextPosition = currentPosition + _objectMovingDirection * _objectMovingSpeed * deltaTime;
            this.transform.position = nextPosition;
        }

        private void Rotate(float deltaTime)
        {
            if (_objectRotatingSpeed == 0.0f)
            {
                return;
            }

            _bodyImage.transform.Rotate(0.0f, 0.0f, deltaTime * _objectRotatingSpeed);
        }

        private void ReflectionToRect()
        {
            Vector2 movePosition = this.transform.position;
            bool isReflecting = false;

            if (movePosition.x < _moveRect.xMin)
            {
                movePosition.x = _moveRect.xMin;
                isReflecting = true;
            }
            else if (movePosition.x > _moveRect.xMax)
            {
                movePosition.x = _moveRect.xMax;
                isReflecting = true;
            }
            else if (movePosition.y > _moveRect.yMax)
            {
                movePosition.y = _moveRect.yMax;
                isReflecting = true;
            }
            else if (movePosition.y < _moveRect.yMin)
            {
                movePosition.y = _moveRect.yMin;
                isReflecting = true;
            }

            if (isReflecting)
            {
                this.transform.position = movePosition;
                _objectMovingDirection = (Vector2)_targetTransform.position - movePosition;
                _objectMovingDirection.Normalize();

                if (_objectRotatingSpeed == 0.0f)
                {
                    float angle = Mathf.Rad2Deg * Mathf.Atan2(_objectMovingDirection.y, _objectMovingDirection.x);
                    _bodyImage.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
                }
            }
        }

        private void CreateSplitedObject(Stage stage)
        {
            for (int i = 0; i < _splitedObjectAmount; i++)
            {
                Vector2 projectileDir = Quaternion.Euler(0, 0, 360f / _splitedObjectAmount * i) * Vector2.up;

                stage.CreateProjectile(
                    _splitedObjectBodyPath,
                    _owner.Alliance,
                    _owner,
                    _splitedObjectDamage,
                    _splitedObjectKnobackPower,
                    this.transform.position,
                    projectileDir,
                    _splitedObjectSpeed,
                    _splitedObjectAcceleration,
                    _splitedObjectRadius,
                    _splitedObjectAliveDistance,
                    hitChances: 1,
                    splitCount: 0,
                    _isSpinBladeCollide,
                    hitSoundPrefabPath: string.Empty
                    );
            }
        }

    }
}
