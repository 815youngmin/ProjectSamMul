using UnityEngine;
using Z.GameClients.Stages.Characters.Monsters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class IceThreeLeapsBugReflectionAreaEffect : AreaEffectObjectBase
    {
        private static readonly float ObjectRadius = 0.5f;
        private static readonly float KnockBackPower = 0f;
        private static readonly float LifeTime = 5;

        private static readonly float AreaEffectRadius = 2.5f;
        private static readonly string PrefabPath = "Stages/AreaEffects/PoisonEffects/PoisonBall.prefab";

        public override bool IsAlive => _isAlive;
        private bool _isAlive;

        private Monster _owner;
        private Vector2 _movingDirection;
        private float _damage;
        private float _createdAt;
        private float _areaEffectDamage;
        private Rect _moveRect;

        private float _movingSpeed;
        private float _areaEffectLifeTime;


        private GameObject _body;
        private Collider2D[] _overlappedColliders;
        private RaycastHit2D[] _raycastHits;


        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.IceThreeLeapsBugReflectionObject);

            _body = ResourcePool.Instance.InstantiateFromResource(PrefabPath);
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;
            _body.transform.localScale = Vector2.one;

            _overlappedColliders = new Collider2D[16];
            _raycastHits = new RaycastHit2D[16];
        }

        public void Initialize(
            Monster owner,
            Vector2 startPosition,
            Vector2 movingDirection,
            float damage,
            float moveSpeed,
            float areaEffectLifeTime,
            float areaEffectDamage,
            Rect moveRect
            )
        {
            base.InitializeAreaObject(owner.Alliance);
            _owner = owner;
            _movingDirection = movingDirection.normalized;
            _moveRect = moveRect;
            _damage = damage;
            _areaEffectDamage = areaEffectDamage;

            _movingSpeed = moveSpeed;
            _areaEffectLifeTime = areaEffectLifeTime;

            _createdAt = Time.time;
            _isAlive = true;
            this.transform.position = startPosition;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            CombatSystem.HandleAreaEffectCollisionWithCharacter(
                areaEffectObject: this,
                position: transform.position,
                radius: ObjectRadius,
                direction: _movingDirection,
                speed: _movingSpeed,
                deltaTime: deltaTime,
                overlappedColliders: _overlappedColliders,
                raycastHits: _raycastHits,
                onHitCharacter: (character) =>
                {
                    character.Hitted(stage, _owner, _damage, _movingDirection, transform.position, hitSoundPrefabPath: string.Empty);
                    stage.CreateIceThreeLeapsBugPoisonAreaEffect(_owner, this.transform.position, _areaEffectLifeTime, 0f, _areaEffectDamage, AreaEffectRadius);
                    _isAlive = false;
                });

            if (!IsAlive)
            {
                return;
            }

            this.MoveToCurrentPosition(deltaTime);
            this.ReflectionToRect();
            this.RotateBodyImageToMoveDirection();

            if (_createdAt + LifeTime <= Time.time)
            {
                stage.CreateIceThreeLeapsBugPoisonAreaEffect(_owner, this.transform.position, _areaEffectLifeTime, 0f, _areaEffectDamage, AreaEffectRadius);
                _isAlive = false;
                return;
            }
        }

        private void MoveToCurrentPosition(float deltaTime)
        {
            Vector2 currentPosition = this.transform.position;
            Vector2 nextPosition = currentPosition + _movingDirection * _movingSpeed * deltaTime;
            this.transform.position = nextPosition;
        }

        private void ReflectionToRect()
        {

            Vector2 movePosition = this.transform.position;
            if (movePosition.x < _moveRect.xMin)
            {
                movePosition.x = _moveRect.xMin;
                _movingDirection = Vector2.Reflect(_movingDirection, Vector2.right);

            }
            else if (movePosition.x > _moveRect.xMax)
            {
                movePosition.x = _moveRect.xMax;
                _movingDirection = Vector2.Reflect(_movingDirection, Vector2.left);

            }
            else if (movePosition.y > _moveRect.yMax)
            {
                movePosition.y = _moveRect.yMax;
                _movingDirection = Vector2.Reflect(_movingDirection, Vector2.down);
            }
            else if (movePosition.y < _moveRect.yMin)
            {
                movePosition.y = _moveRect.yMin;
                _movingDirection = Vector2.Reflect(_movingDirection, Vector2.up);

            }

            _movingDirection.Normalize();
            this.transform.position = movePosition;
        }

        private void RotateBodyImageToMoveDirection()
        {
            Vector2 direction = _movingDirection * _movingSpeed;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _body.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
        }

    }

}
