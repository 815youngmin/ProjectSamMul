using UnityEngine;
using Z.GameClients.Stages.Characters.Monsters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    //벽이나 플레이어에게 부딪히면 분열하는 투사체입니다.
    public class SplitAreaEffectObject : AreaEffectObjectBase
    {
        public override bool IsAlive => !_isSplited;
        private bool _isSplited;

        private Monster _owner;
        private string _objectBodyPath;
        private float _objectRadius;
        private Vector2 _objectMovingDirection;
        private float _objectMovingSpeed;
        private float _objectDamage;
        private float _objectLifeTime;
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

        public void AllocateSharedResources(AreaEffectType areaEffectType, string bodyPath, string projectilePath)
        {
            base.AllocateSharedResourcesForBase(areaEffectType);

            _objectBodyPath = bodyPath;
            _bodyImage = ResourcePool.Instance.InstantiateFromResource(bodyPath);
            _bodyImage.transform.SetParent(this.transform);
            _bodyImage.transform.localPosition = Vector2.zero;

            _projectilePath = projectilePath;

            _overlappedColliders = new Collider2D[16];
            _raycastHits = new RaycastHit2D[16];
        }

        //초기화 단계에서 리소스 경로 입력받아서 처리하기 위해 처리
        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.SplitObject);

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
            float objectDamage,
            float objectAliveDistance,
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
            _objectDamage = objectDamage;
            _objectLifeTime = objectAliveDistance / objectMovingSpeed;
            _moveRect = moveRect;

            _splitAt = Time.time + _objectLifeTime;

            _projectileAmount = projectileAmount;
            _projectileRadius = projectileRadius;
            _projectileSpeed = projectileSpeed;
            _projectileAcceleration = projectileAcceleration;
            _projectileDamage = projectileDamage;
            _projectileKnobackPower = projectileKnobackPower;
            _projectileAliveDistance = projectileAliveDistance;
            _isSpinBladeCollide = isSpinBladeCollide;

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
            _objectDamage = objectDamage;
            _objectLifeTime = objectAliveDistance / objectMovingSpeed;
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

            _splitAt = Time.time + _objectLifeTime;

            _projectileAmount = projectileAmount;
            _projectileRadius = projectileRadius;
            _projectileSpeed = projectileSpeed;
            _projectileAcceleration = projectileAcceleration;
            _projectileDamage = projectileDamage;
            _projectileKnobackPower = projectileKnobackPower;
            _projectileAliveDistance = projectileAliveDistance;
            _isSpinBladeCollide = isSpinBladeCollide;

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
                    this.CreateSplitProjectile(stage);
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
                this.CreateSplitProjectile(stage);
                _isSplited = true;
            }
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();

            if (AreaEffectObjectType == AreaEffectType.SplitObject)
            {
                ResourcePool.Instance.PutBackInstance(_objectBodyPath, _bodyImage);
                _objectBodyPath = null;
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

        private void CreateSplitProjectile(Stage stage)
        {
            for (int i = 0; i < _projectileAmount; i++)
            {
                Vector2 projectileDir = Quaternion.Euler(0, 0, 360f / _projectileAmount * i) * Vector2.up;
                stage.CreateProjectile(
                    _projectilePath,
                    _owner.Alliance,
                    _owner,
                    _projectileDamage,
                    _projectileKnobackPower,
                    this.transform.position,
                    projectileDir,
                    _projectileSpeed,
                    _projectileAcceleration,
                    _projectileRadius,
                    _projectileAliveDistance,
                    hitChances: 1,
                    splitCount: 0,
                    isRemovableBySpinBladeObject: _isSpinBladeCollide,
                    hitSoundPrefabPath: string.Empty);
            }
        }
    }
}
