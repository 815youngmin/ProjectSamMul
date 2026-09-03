using UnityEngine;
using Z.GameClients.Stages.Characters.Monsters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class HomingSplitObject : AreaEffectObjectBase
    {
        public override bool IsAlive => !_isSplited;

        private Monster _owner;
        private Transform _targetTransform;
        private float _objectRadius;
        private float _objectMovingSpeed;
        private float _objectDamage;
        private float _homingPower;
        private bool _willRotateInMoveDirection;

        private GameObject _body;
        private Collider2D[] _overlappedColliders;
        private RaycastHit2D[] _raycastHits;

        private string _projectilePath;
        private float _projectileRadius;
        private float _projectileSpeed;
        private float _projectileAcceleration;
        private float _projectileDamage;
        private float _projectileKnobackPower;
        private float _projectileAliveDistance;
        private int _projectileAmount;

        private float _splitAt;
        private bool _isSplited;

        private string _bodyPath;
        private Vector2 _movingDirection;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.HomingSplitObject);
            _body = null;
            _bodyPath = null;
            _projectilePath = null;

            _overlappedColliders = new Collider2D[16];
            _raycastHits = new RaycastHit2D[16];
        }

        public void Initialize(
            Monster owner,
            Transform targetTransform,
            string objectPath,
            Vector2 startPosition,
            Vector2 startDirection,
            float objectRadius,
            float objectMovingSpeed,
            float objectDamage,
            float objectLifeTime,
            float homingPower,
            bool willRotateInMoveDirection,
            string projectilePath,
            int projectileAmount,
            float projectileRadius,
            float projectileSpeed,
            float projectileAcceleration,
            float projectileDamage,
            float projectileKnobackPower,
            float projectileAliveDistance)
        {
            base.InitializeAreaObject(owner.Alliance);

            _owner = owner;
            _bodyPath = objectPath;
            _targetTransform = targetTransform;
            _objectRadius = objectRadius;
            _objectMovingSpeed = objectMovingSpeed;
            _objectDamage = objectDamage;
            _homingPower = homingPower;
            _movingDirection = startDirection;
            _willRotateInMoveDirection = willRotateInMoveDirection;

            _projectilePath = projectilePath;
            _projectileAmount = projectileAmount;
            _projectileRadius = projectileRadius;
            _projectileSpeed = projectileSpeed;
            _projectileAcceleration = projectileAcceleration;
            _projectileDamage = projectileDamage;
            _projectileKnobackPower = projectileKnobackPower;
            _projectileAliveDistance = projectileAliveDistance;

            if (_body != null)
            {
                Debug.LogWarning($"_body 이미 생성되어있는데 또 구현하려 합니다. 로직이 잘못되었습니다. 확인이 필요합니다.");
            }
            _body = ResourcePool.Instance.InstantiateFromResource(_bodyPath);
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector3.zero;

            _splitAt = Time.time + objectLifeTime;

            _isSplited = false;
            this.transform.position = startPosition;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            this.RotateMoveDirectionToHomingDirection(deltaTime);
            this.RotateBodyImageToMoveDirection();
            this.MoveToCurrentPosition(deltaTime);

            CombatSystem.HandleAreaEffectCollisionWithCharacter(
                areaEffectObject: this,
                position: transform.position,
                radius: _objectRadius,
                direction: _movingDirection,
                speed: _objectMovingSpeed,
                deltaTime: deltaTime,
                overlappedColliders: _overlappedColliders,
                raycastHits: _raycastHits,
                onHitCharacter: (character) =>
                {
                    character.Hitted(stage, _owner, _objectDamage, _movingDirection, this.transform.position, hitSoundPrefabPath: string.Empty);
                    this.CreateSplitProjectiles(stage);
                    _isSplited = true;
                });

            if (!IsAlive)
            {
                return;
            }

            if (_splitAt <= Time.time)
            {
                this.CreateSplitProjectiles(stage);
                _isSplited = true;
            }
        }

        private void RotateMoveDirectionToHomingDirection(float deltaTime)
        {
            Vector2 targetDirection = _targetTransform.position - this.transform.position;
            targetDirection.Normalize();
            _movingDirection = Vector2.Lerp(_movingDirection, targetDirection, _homingPower * deltaTime).normalized;

        }

        private void MoveToCurrentPosition(float deltaTime)
        {
            Vector2 currentPosition = this.transform.position;
            Vector2 nextPosition = currentPosition + _movingDirection * deltaTime * _objectMovingSpeed;
            this.transform.position = nextPosition;
        }

        // 몸체 이미지 리소스 회전 코드
        private void RotateBodyImageToMoveDirection()
        {
            if (!_willRotateInMoveDirection)
            {
                return;
            }

            Vector2 direction = _movingDirection * _objectMovingSpeed;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _body.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        private void CreateSplitProjectiles(Stage stage)
        {
            for (int i = 0; i < _projectileAmount; i++)
            {
                Vector2 projectileDir = Quaternion.Euler(0, 0, 360f / _projectileAmount * i) * Vector2.up;
                stage.CreateProjectile(_projectilePath, _owner.Alliance, _owner, _projectileDamage, _projectileKnobackPower, this.transform.position,
                                        projectileDir, _projectileSpeed, _projectileAcceleration, _projectileRadius, _projectileAliveDistance,
                                        hitChances: 1, splitCount: 0, isRemovableBySpinBladeObject: false, hitSoundPrefabPath: string.Empty);
            }
        }

        public override void PuttingBackToPool()
        {
            if(_body != null)
            {
                ResourcePool.Instance.PutBackInstance(_bodyPath, _body);
                _bodyPath = null;
                _body = null;
            }
            base.PuttingBackToPool();
        }
    }
}