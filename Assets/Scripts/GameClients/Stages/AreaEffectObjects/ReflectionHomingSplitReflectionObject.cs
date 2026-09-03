using UnityEngine;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class ReflectionHomingSplitReflectionObject : AreaEffectObjectBase
    {
        public override bool IsAlive => !_isSplited;
        private bool _isSplited;

        private Monster _owner;
        private Transform _targetTransform;
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

        private AreaEffectType _splitObjectType;
        private int _splitObjectAmount;
        private float _splitObjectRadius;
        private float _splitObjectSpeed;
        private float _splitObjectRotatingSpeed;
        private float _splitObjectDamage;
        private float _splitObjectKnobackPower;
        private float _splitObjectLifeTime;

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

        public void Initialize(
            Monster owner,
            Transform targetTransform,
            AllianceType alliance,
            float objectRadius,
            Vector2 startPosition,
            Vector2 objectMovingDirection,
            float objectMovingSpeed,
            float objectRotatingSpeed,
            float objectDamage,
            float objectLifeTime,
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
            base.InitializeAreaObject(alliance);
            _owner = owner;
            _targetTransform = targetTransform;
            _objectRadius = objectRadius;
            _objectMovingDirection = objectMovingDirection.normalized;
            _objectMovingSpeed = objectMovingSpeed;
            _objectRotatingSpeed = objectRotatingSpeed;
            _objectDamage = objectDamage;
            _moveRect = moveRect;

            _splitObjectType = splitObjectType;
            _splitObjectAmount = splitObjectAmount;
            _splitObjectRadius = splitObjectRadius;
            _splitObjectSpeed = splitObjectSpeed;
            _splitObjectRotatingSpeed = splitObjectRotatingSpeed;
            _splitObjectDamage = splitObjectDamage;
            _splitObjectKnobackPower = splitObjectKnobackPower;
            _splitObjectLifeTime = splitObjectLifeTime;

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
                    this.CreateReflectionProjectile(stage);
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
                this.CreateReflectionProjectile(stage);
                _isSplited = true;
            }
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
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

        private void CreateReflectionProjectile(Stage stage)
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
