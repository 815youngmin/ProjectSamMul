using UnityEngine;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class ReflectionSplitSpinMoveObjectAreaEffectObject : AreaEffectObjectBase
    {
        public override bool IsAlive => !_isSplited;
        private bool _isSplited;

        private Monster _owner;
        private string _reflectionObjectBodyPath;
        private float _reflectionObjectRadius;
        private Vector2 _reflectionObjectMovingDirection;
        private float _reflectionObjectMovingSpeed;
        private float _reflectionObjectRotatingSpeed;
        private float _reflectionObjectDamage;
        private float _splitAt;

        private Rect _moveRect;

        private GameObject _bodyImage;
        private Collider2D[] _overlappedColliders;
        private RaycastHit2D[] _raycastHits;

        private string _spinMoveObjectBodyPath;
        private int _spinMoveObjectAmount;
        private float _spinMoveObjectDamage;
        private float _spinMoveObjectCreateDistance;
        private float _spinMoveObjectRadius;
        private float _spinMoveObjectLifeTime;
        private float _spinMoveObjectRadiusUpSpeed;
        private float _spinMoveObjectAngleUpSpeed;
        private float _spinMoveObjectRadiusUpAcceleration;
        private float _spinMoveObjectAngleUpAcceleration;
        private float _spinMoveObjectBodyRotationSpeed;
        private float _spinMoveObjectMaxRadius;
        private float _spinMoveObjectMaxRadiusUpSpeed;
        private float _spinMoveObjectMaxAngleUpSpeed;
        private bool _isSpinMoveObjectLeftRotate;

        //초기화 단계에서 리소스 경로 입력받아서 처리하기 위해 처리
        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.ReflectionSplitSpinMoveObjectAreaEffectObject);

            _bodyImage = null;
            _spinMoveObjectBodyPath = null;

            _overlappedColliders = new Collider2D[16];
            _raycastHits = new RaycastHit2D[16];
        }

        public void Initialize(
           Monster owner,
           float reflectionObjectRadius,
           Vector2 startPosition,
           Vector2 reflectionObjectMovingDirection,
           float reflectionObjectMovingSpeed,
           float reflectionObjectRotatingSpeed,
           float reflectionObjectDamage,
           float reflectionObjectLifeTime,
           string reflectionObjectBodyPath,
           Rect moveRect,
           int spinMoveObjectAmount,
           float spinMoveObjectDamage,
           float spinMoveObjectCreateDistance,
           float spinMoveObjectRadius,
           float spinMoveObjectLifeTime,
           float spinMoveObjectRadiusUpSpeed,
           float spinMoveObjectAngleUpSpeed,
           float spinMoveObjectRadiusUpAcceleration,
           float spinMoveObjectAngleUpAcceleration,
           float spinMoveObjectBodyRotationSpeed,
           float spinMoveObjectMaxRadius,
           float spinMoveObjectMaxRadiusUpSpeed,
           float spinMoveObjectMaxAngleUpSpeed,
           bool isSpinMoveObjectLeftRotate,
           string spinMoveObjectBodyPath
           )
        {
            base.InitializeAreaObject(owner.Alliance);
            _owner = owner;
            _reflectionObjectRadius = reflectionObjectRadius;
            _reflectionObjectMovingDirection = reflectionObjectMovingDirection.normalized;
            _reflectionObjectMovingSpeed = reflectionObjectMovingSpeed;
            _reflectionObjectRotatingSpeed = reflectionObjectRotatingSpeed;
            _reflectionObjectDamage = reflectionObjectDamage;
            _moveRect = moveRect;

            if (_bodyImage != null)
            {
                Debug.LogWarning($"_bodyImage가 이미 생성되어있는데 또 구현하려 합니다. 로직이 잘못되었습니다. 확인이 필요합니다.");
            }
            _reflectionObjectBodyPath = reflectionObjectBodyPath;
            _bodyImage = ResourcePool.Instance.InstantiateFromResource(_reflectionObjectBodyPath);
            _bodyImage.transform.SetParent(this.transform);
            _bodyImage.transform.localPosition = Vector2.zero;

           _spinMoveObjectAmount = spinMoveObjectAmount;
           _spinMoveObjectDamage = spinMoveObjectDamage;
           _spinMoveObjectCreateDistance = spinMoveObjectCreateDistance;
           _spinMoveObjectRadius = spinMoveObjectRadius;
           _spinMoveObjectLifeTime = spinMoveObjectLifeTime;
           _spinMoveObjectRadiusUpSpeed = spinMoveObjectRadiusUpSpeed;
           _spinMoveObjectAngleUpSpeed = spinMoveObjectAngleUpSpeed;
           _spinMoveObjectRadiusUpAcceleration = spinMoveObjectRadiusUpAcceleration;
           _spinMoveObjectAngleUpAcceleration = spinMoveObjectAngleUpAcceleration;
           _spinMoveObjectBodyRotationSpeed = spinMoveObjectBodyRotationSpeed;
           _spinMoveObjectMaxRadius = spinMoveObjectMaxRadius;
           _spinMoveObjectMaxRadiusUpSpeed = spinMoveObjectMaxRadiusUpSpeed;
           _spinMoveObjectMaxAngleUpSpeed = spinMoveObjectMaxAngleUpSpeed;
           _isSpinMoveObjectLeftRotate = isSpinMoveObjectLeftRotate;
           _spinMoveObjectBodyPath = spinMoveObjectBodyPath;

            _splitAt = Time.time + reflectionObjectLifeTime;
            _isSplited = false;
            this.transform.position = startPosition;

            float angle = Mathf.Rad2Deg * Mathf.Atan2(_reflectionObjectMovingDirection.y, _reflectionObjectMovingDirection.x);
            _bodyImage.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }


        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            CombatSystem.HandleAreaEffectCollisionWithCharacter(
                areaEffectObject: this,
                position: transform.position,
                radius: _reflectionObjectRadius,
                direction: _reflectionObjectMovingDirection,
                speed: _reflectionObjectMovingSpeed,
                deltaTime: deltaTime,
                overlappedColliders: _overlappedColliders,
                raycastHits: _raycastHits,
                onHitCharacter: (character) =>
                {
                    character.Hitted(stage, _owner, _reflectionObjectDamage, _reflectionObjectMovingDirection, transform.position, hitSoundPrefabPath: string.Empty);
                    this.FireCircleShape(stage, this.transform.position);
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
                this.FireCircleShape(stage, this.transform.position);
                _isSplited = true;
            }
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();

            if (AreaEffectObjectType == AreaEffectType.ReflectionSplitSpinMoveObjectAreaEffectObject)
            {
                ResourcePool.Instance.PutBackInstance(_reflectionObjectBodyPath, _bodyImage);
                _bodyImage = null;
            }
        }

        private void MoveToCurrentPosition(float deltaTime)
        {
            Vector2 currentPosition = this.transform.position;
            Vector2 nextPosition = currentPosition + _reflectionObjectMovingDirection * _reflectionObjectMovingSpeed * deltaTime;
            this.transform.position = nextPosition;
        }

        private void Rotate(float deltaTime)
        {
            if (_reflectionObjectRotatingSpeed == 0.0f)
            {
                return;
            }

            _bodyImage.transform.Rotate(0.0f, 0.0f, deltaTime * _reflectionObjectRotatingSpeed);
        }

        private void ReflectionToRect()
        {
            Vector2 movePosition = this.transform.position;
            bool isReflecting = false;

            if (movePosition.x < _moveRect.xMin)
            {
                movePosition.x = _moveRect.xMin;
                _reflectionObjectMovingDirection = Vector2.Reflect(_reflectionObjectMovingDirection, Vector2.right);
                isReflecting = true;
            }
            else if (movePosition.x > _moveRect.xMax)
            {
                movePosition.x = _moveRect.xMax;
                _reflectionObjectMovingDirection = Vector2.Reflect(_reflectionObjectMovingDirection, Vector2.left);
                isReflecting = true;
            }
            else if (movePosition.y > _moveRect.yMax)
            {
                movePosition.y = _moveRect.yMax;
                _reflectionObjectMovingDirection = Vector2.Reflect(_reflectionObjectMovingDirection, Vector2.down);
                isReflecting = true;
            }
            else if (movePosition.y < _moveRect.yMin)
            {
                movePosition.y = _moveRect.yMin;
                _reflectionObjectMovingDirection = Vector2.Reflect(_reflectionObjectMovingDirection, Vector2.up);
                isReflecting = true;
            }

            if (isReflecting)
            {
                this.transform.position = movePosition;
                _reflectionObjectMovingDirection.Normalize();

                if (_reflectionObjectRotatingSpeed == 0.0f)
                {
                    float angle = Mathf.Rad2Deg * Mathf.Atan2(_reflectionObjectMovingDirection.y, _reflectionObjectMovingDirection.x);
                    _bodyImage.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
                }
            }
        }

        private void FireCircleShape(Stage stage, Vector2 firePosition)
        {
            for (int i = 0; i < _spinMoveObjectAmount; i++)
            {
                CreateSpinMoveObject(stage, firePosition, 360f / _spinMoveObjectAmount * i, Vector2.zero, _isSpinMoveObjectLeftRotate);
            }
        }

        private void CreateSpinMoveObject(Stage stage, Vector2 firePos, float startAngle, Vector2 offsetPosition, bool isLeftRotate)
        {
            float angleDirection = isLeftRotate ? 1 : -1;
            stage.CreateSpinMoveObject(
            _owner,
            _spinMoveObjectDamage,
            firePos,
            _spinMoveObjectBodyPath,
            _spinMoveObjectLifeTime,
            _spinMoveObjectCreateDistance* angleDirection,
            startAngle * angleDirection,
            _spinMoveObjectRadiusUpSpeed * angleDirection,
            _spinMoveObjectAngleUpSpeed * angleDirection,
            _spinMoveObjectRadiusUpAcceleration * angleDirection,
            _spinMoveObjectAngleUpAcceleration* angleDirection,
            _spinMoveObjectRadius,
            _spinMoveObjectBodyRotationSpeed,
            offsetPosition,
            _spinMoveObjectMaxRadius,
            _spinMoveObjectMaxRadiusUpSpeed,
            _spinMoveObjectMaxAngleUpSpeed);
        }


    }
}
