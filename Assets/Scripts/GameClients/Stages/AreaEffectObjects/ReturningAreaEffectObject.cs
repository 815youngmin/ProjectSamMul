using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.Characters.Monsters;
using Z.ResourcePools;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class ReturningAreaEffectObject : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _endAt && !_isHitted;

        private GameObject _bodyImage;

        private Monster _owner;
        private float _radius;
        private Vector2 _startPosition;
        private Vector2 _movingDirection;
        private float _movingSpeed;
        private float _rotatingSpeed;
        private float _damage;
        private Rect _rect;

        private Collider2D[] _overlappedColliders;
        private RaycastHit2D[] _raycastHits;
        private float _endAt;
        private bool _isHitted;

        public void AllocateSharedResources(AreaEffectType areaEffectType, string imagePath)
        {
            base.AllocateSharedResourcesForBase(areaEffectType);
            _bodyImage = ResourcePool.Instance.InstantiateFromResource(imagePath);
            _bodyImage.transform.SetParent(gameObject.transform);
            _bodyImage.transform.localPosition = Vector3.zero;
        }

        public void Initialize(
            Monster owner,
            float radius,
            Vector2 startPosition,
            Vector2 movingDirection,
            float movingSpeed,
            float rotatingSpeed,
            float damage,
            Rect rect)
        {
            base.InitializeAreaObject(owner.Alliance);

            _owner = owner;
            _radius = radius;
            _startPosition = startPosition;
            _movingDirection = movingDirection;
            _movingSpeed = movingSpeed;
            _rotatingSpeed = rotatingSpeed;
            _damage = damage;
            _rect = rect;

            _overlappedColliders = new Collider2D[16];
            _raycastHits = new RaycastHit2D[16];
            _endAt = Time.time + 10.0f;
            _isHitted = false;

            transform.position = _startPosition;
            _bodyImage.transform.rotation = Quaternion.AngleAxis(Mathf.Atan2(_movingDirection.y, _movingDirection.x) * Mathf.Rad2Deg, Vector3.forward);
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            this.Move(deltaTime);
            this.CheckAndReturn();
            this.Rotate(deltaTime);
            this.HandleCharacterCollisions(stage, deltaTime);
        }

        private void Move(float deltaTime)
        {
            this.transform.Translate(deltaTime * _movingSpeed * _movingDirection);
        }

        private void CheckAndReturn()
        {
            Vector2 position = transform.position;
            bool isReturning = false;

            if (position.x < _rect.xMin)
            {
                position.x = _rect.xMin;
                isReturning = true;
            }
            else if (position.x > _rect.xMax)
            {
                position.x = _rect.xMax;
                isReturning = true;
            }
            else if (position.y > _rect.yMax)
            {
                position.y = _rect.yMax;
                isReturning = true;
            }
            else if (position.y < _rect.yMin)
            {
                position.y = _rect.yMin;
                isReturning = true;
            }

            if (isReturning)
            {
                transform.position = position;
                _movingDirection *= -1;
                _endAt = Time.time + Vector2.Distance(_startPosition, position) / _movingSpeed;
                if (_rotatingSpeed == 0.0f)
                {
                    _bodyImage.transform.Rotate(0.0f, 0.0f, 180.0f);
                }
            }
        }

        private void Rotate(float deltaTime)
        {
            if (_rotatingSpeed == 0.0f)
            {
                return;
            }

            _bodyImage.transform.Rotate(0.0f, 0.0f, deltaTime * _rotatingSpeed);
        }

        private void HandleCharacterCollisions(Stage stage, float deltaTime)
        {
            if (!IsAlive)
            {
                return;
            }

            int overlappedColliderscount = Physics2D.OverlapCircleNonAlloc(transform.position, _radius, _overlappedColliders);
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

                character.Hitted(stage, _owner, _damage, _movingDirection, transform.position, string.Empty);
                _isHitted = true;
                return;
            }

            var projectileLayer = LayerMask.NameToLayer("Projectile");
            int collidibleLayers = Physics2D.GetLayerCollisionMask(projectileLayer);
            float moveDistance = deltaTime * _movingSpeed;

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

                character.Hitted(stage, _owner, _damage, _movingDirection, transform.position, string.Empty);
                _isHitted = true;
                return;
            }
        }
    }
}
