using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.Characters.Monsters;
using Z.ResourcePools;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class GenieSandHomingObject : AreaEffectObjectBase
    {
        private const string BODY_PREFAB_PATH = "Stages/Projectiles/Stone_Radius2.prefab";
        private const float RotateSpeed = 180f;

        public override bool IsAlive => _isAlive;

        private GameObject _body;
        private Collider2D[] _overlappedColliders;
        private RaycastHit2D[] _raycastHits;

        private Monster _owner;
        private Character _target;
        private float _moveSpeed;
        private float _homingPowerRate;
        private float _lifetime;
        private float _homingObjectDamage;
        private float _homingObjectRadius;
        private float _areaEffectCreationPeriod;
        private float _areaEffectLifetime;
        private float _areaEffectTickPeriod;
        private float _areaEffectDamagePerTick;
        private float _areaEffectRadius;
        private float _lastAreaEffectRadius;

        private float _createdAt;
        private float _createAreaEffectAt;
        private bool _isAlive;
        private Vector2 _movingDirection;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.GenieSandHomingObject);
            _body = ResourcePool.Instance.InstantiateFromResource(BODY_PREFAB_PATH);
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;
            _body.transform.localScale = Vector2.one * 2f;

            _overlappedColliders = new Collider2D[16];
            _raycastHits = new RaycastHit2D[16];
        }

        public void Initialize(
            Monster owner,
            Character target,
            Vector2 startPosition,
            float moveSpeed,
            float lifetime,
            float homingPower,
            float homingObjectDamage,
            float homingObjectRadius,
            float areaEffectCreationPeriod,
            float areaEffectLifetime,
            float areaEffectTickPeriod,
            float areaEffectDamagePerTick,
            float areaEffectRadius,
            float lastAreaEffectRadius)
        {
            base.InitializeAreaObject(owner.Alliance);
            this.transform.position = startPosition;

            _owner = owner;
            _target = target;
            _moveSpeed = moveSpeed;
            _lifetime = lifetime;
            _homingPowerRate = homingPower;
            _homingObjectDamage = homingObjectDamage;
            _homingObjectRadius = homingObjectRadius;
            _areaEffectCreationPeriod = areaEffectCreationPeriod;
            _areaEffectLifetime = areaEffectLifetime;
            _areaEffectTickPeriod = areaEffectTickPeriod;
            _areaEffectDamagePerTick = areaEffectDamagePerTick;
            _areaEffectRadius = areaEffectRadius;
            _lastAreaEffectRadius = lastAreaEffectRadius;

            _createdAt = Time.time;
            _createAreaEffectAt = _createdAt + _areaEffectCreationPeriod;
            _isAlive = true;
            _movingDirection = Vector2.zero;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;

            if (_createdAt + _lifetime < now && _isAlive)
            {
                this.CreateSandAreaEffect(stage, _lastAreaEffectRadius);
                _isAlive = false;
                return;
            }

            if (_createAreaEffectAt < now && _isAlive)
            {
                this.CreateSandAreaEffect(stage, _areaEffectRadius);
                _createAreaEffectAt += _areaEffectCreationPeriod;
            }

            this.RotateMoveDirectionToHomingDirection(deltaTime);
            this.RotateBodyImage(deltaTime);
            this.MoveToCurrentPosition(deltaTime);
            this.HandleCharacterCollisions(stage, deltaTime);
        }

        private void RotateBodyImage(float deltaTime)
        {
            _body.transform.Rotate(0, 0, RotateSpeed * deltaTime);
        }

        private void RotateMoveDirectionToHomingDirection(float deltaTime)
        {
            Vector2 targetDirection = _target.transform.position - this.transform.position;
            targetDirection.Normalize();
            _movingDirection = Vector2.Lerp(_movingDirection, targetDirection, _homingPowerRate * deltaTime).normalized;

        }        
        // 현재 포지션 적용 (이동) 코드
        private void MoveToCurrentPosition(float deltaTime)
        {
            Vector2 currentPosition = this.transform.position;
            Vector2 nextPosition = currentPosition + _movingDirection * deltaTime * _moveSpeed;
            this.transform.position = nextPosition;
        }

        private void CreateSandAreaEffect(Stage stage, float radius)
        {
            stage.CreateSandAreaEffectObject(
                _owner, 
                delay: 0f,
                indicatorDuration: 0f,
                _areaEffectDamagePerTick, 
                _areaEffectTickPeriod, 
                _areaEffectLifetime,
                radius, 
                this.transform.position);
        }

        private void HandleCharacterCollisions(Stage stage, float deltaTime)
        {
            if (!IsAlive)
            {
                return;
            }

            int overlappedColliderscount = Physics2D.OverlapCircleNonAlloc(transform.position, _homingObjectRadius, _overlappedColliders);
            for (int i = 0; i < overlappedColliderscount; ++i)
            {
                var character = _overlappedColliders[i].GetComponentInParent<Character>();
                if (character == null)
                {
                    continue;
                }

                if (character.Alliance == _owner.Alliance)
                {
                    continue;
                }

                character.Hitted(stage, _owner, _homingObjectDamage, _movingDirection, transform.position, string.Empty);
                this.CreateSandAreaEffect(stage, 2.0f * _areaEffectRadius);
                _isAlive = false;
                return;
            }

            var projectileLayer = LayerMask.NameToLayer("Projectile");
            int collidibleLayers = Physics2D.GetLayerCollisionMask(projectileLayer);
            float moveDistance = deltaTime * _moveSpeed;

            int raycastHitsCount = Physics2D.RaycastNonAlloc(transform.position, _movingDirection, _raycastHits, moveDistance, collidibleLayers);
            for (int i = 0; i < raycastHitsCount; ++i)
            {
                var collider = _raycastHits[i].collider;
                if (collider.isTrigger)
                {
                    continue;
                }

                var character = collider.GetComponentInParent<Character>();
                if (character == null)
                {
                    continue;
                }

                if (character.Alliance == _owner.Alliance)
                {
                    continue;
                }

                character.Hitted(stage, _owner, _homingObjectDamage, _movingDirection, transform.position, string.Empty);
                this.CreateSandAreaEffect(stage, 2.0f * _areaEffectRadius);
                _isAlive = false;
                return;
            }
        }

    }
}
