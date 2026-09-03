using UnityEngine;
using Z.GameClients.Stages.Characters.Monsters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    //벽에 반사되며 이동하다 시간이 되면 갈라지는 투사체입니다.
    public class ReflectionSplitAreaEffectObject : AreaEffectObjectBase
    {
        public override bool IsAlive => !_isSplited;
        private bool _isSplited;

        private Monster _owner;
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

        private string _projectilePath;
        private int _projectileAmount;
        private float _projectileRadius;
        private float _projectileSpeed;
        private float _projectileAcceleration;
        private float _projectileDamage;
        private float _projectileKnobackPower;
        private float _projectileAliveDistance;
        private bool _isSpinBladeCollide;

        public void AllocateSharedResources(AreaEffectType areaEffectType, string imagePath, string projectilePath)
        {
            base.AllocateSharedResourcesForBase(areaEffectType);

            _objectBodyPath = imagePath;
            _bodyImage = ResourcePool.Instance.InstantiateFromResource(imagePath);
            _bodyImage.transform.SetParent(this.transform);
            _bodyImage.transform.localPosition = Vector2.zero;

            _projectilePath = projectilePath;

            _overlappedColliders = new Collider2D[16];
            _raycastHits = new RaycastHit2D[16];
        }

        //초기화 단계에서 리소스 경로 입력받아서 처리하기 위해 처리
        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.ReflectionSplitObject);

            _bodyImage = null;
            _projectilePath = null;

            _overlappedColliders = new Collider2D[16];
            _raycastHits = new RaycastHit2D[16];
        }

        public void Initialize(
            Monster owner,
            float objectRadius,
            Vector2 startPosition,
            Vector2 objectMovingDirection,
            float objectMovingSpeed,
            float objectRotatingSpeed,
            float objectDamage,
            float objectLifeTime,
            Rect moveRect,
            int projectileAmount,
            float projectileRadius,
            float projectileSpeed,
            float projectileAcceleration,
            float projectileDamage,
            float projectileKnobackPower,
            float projectileAliveDistance,
            bool isSpinBladeCollide
            )
        {
            base.InitializeAreaObject(owner.Alliance);
            _owner = owner;
            _objectRadius = objectRadius;
            _objectMovingDirection = objectMovingDirection.normalized;
            _objectMovingSpeed = objectMovingSpeed;
            _objectRotatingSpeed = objectRotatingSpeed;
            _objectDamage = objectDamage;
            _moveRect = moveRect;

            _projectileAmount = projectileAmount;
            _projectileRadius = projectileRadius;
            _projectileSpeed = projectileSpeed;
            _projectileAcceleration = projectileAcceleration;
            _projectileDamage = projectileDamage;
            _projectileKnobackPower = projectileKnobackPower;
            _projectileAliveDistance = projectileAliveDistance;
            _isSpinBladeCollide = isSpinBladeCollide;

            _splitAt = Time.time + objectLifeTime;
            _isSplited = false;
            this.transform.position = startPosition;

            float angle = Mathf.Rad2Deg * Mathf.Atan2(_objectMovingDirection.y, _objectMovingDirection.x);
            _bodyImage.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        public void Initialize(
           Monster owner,
           float objectRadius,
           Vector2 startPosition,
           Vector2 objectMovingDirection,
           float objectMovingSpeed,
           float objectRotatingSpeed,
           float objectDamage,
           float objectLifeTime,
           string objectBodyPath,
           Rect moveRect,
           int projectileAmount,
           float projectileRadius,
           float projectileSpeed,
           float projectileAcceleration,
           float projectileDamage,
           float projectileKnobackPower,
           float projectileAliveDistance,
           bool isSpinBladeCollide,
           string projectileBodyPath
           )
        {
            base.InitializeAreaObject(owner.Alliance);
            _owner = owner;
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

            _projectilePath = projectileBodyPath;

            _projectileAmount = projectileAmount;
            _projectileRadius = projectileRadius;
            _projectileSpeed = projectileSpeed;
            _projectileAcceleration = projectileAcceleration;
            _projectileDamage = projectileDamage;
            _projectileKnobackPower = projectileKnobackPower;
            _projectileAliveDistance = projectileAliveDistance;
            _isSpinBladeCollide = isSpinBladeCollide;

            _splitAt = Time.time + objectLifeTime;
            _isSplited = false;
            this.transform.position = startPosition;

            float angle = Mathf.Rad2Deg * Mathf.Atan2(_objectMovingDirection.y, _objectMovingDirection.x);
            _bodyImage.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
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
                    this.CreateSplitProjectile(stage);
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
                this.CreateSplitProjectile(stage);
                _isSplited = true;
            }
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();

            if (AreaEffectObjectType == AreaEffectType.ReflectionSplitObject)
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
                _objectMovingDirection = Vector2.Reflect(_objectMovingDirection, Vector2.right);
                isReflecting = true;
            }
            else if (movePosition.x > _moveRect.xMax)
            {
                movePosition.x = _moveRect.xMax;
                _objectMovingDirection = Vector2.Reflect(_objectMovingDirection, Vector2.left);
                isReflecting = true;
            }
            else if (movePosition.y > _moveRect.yMax)
            {
                movePosition.y = _moveRect.yMax;
                _objectMovingDirection = Vector2.Reflect(_objectMovingDirection, Vector2.down);
                isReflecting = true;
            }
            else if (movePosition.y < _moveRect.yMin)
            {
                movePosition.y = _moveRect.yMin;
                _objectMovingDirection = Vector2.Reflect(_objectMovingDirection, Vector2.up);
                isReflecting = true;
            }

            if (isReflecting)
            {
                this.transform.position = movePosition;
                _objectMovingDirection.Normalize();

                if (_objectRotatingSpeed == 0.0f)
                {
                    float angle = Mathf.Rad2Deg * Mathf.Atan2(_objectMovingDirection.y, _objectMovingDirection.x);
                    _bodyImage.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
                }
            }
        }

        private void CreateSplitProjectile(Stage stage)
        {
            for (int i = 0; i < _projectileAmount; i++)
            {
                Vector2 projectileDir = Quaternion.Euler(0, 0, 360f / _projectileAmount * i) * Vector2.up;
                stage.CreateProjectile(_projectilePath, _owner.Alliance, _owner, _projectileDamage, _projectileKnobackPower, this.transform.position,
                    projectileDir, _projectileSpeed, _projectileAcceleration, _projectileRadius, _projectileAliveDistance, hitChances: 1, splitCount: 0, _isSpinBladeCollide, hitSoundPrefabPath: string.Empty);
            }
        }
    }
}
