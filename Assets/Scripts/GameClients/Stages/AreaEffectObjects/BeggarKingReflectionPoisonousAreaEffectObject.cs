using DG.Tweening;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class BeggarKingReflectionPoisonousAreaEffectObject : AreaEffectObjectBase
    {

        private static readonly float AreaEffectAttackPeriod = 0.5f;

        public override bool IsAlive => _isAlive;
        private bool _isAlive;

        private Monster _owner;
        private float _objectRadius;
        private Vector2 _movingDirection;
        private float _movingSpeed;
        private float _rotatingSpeed;
        private float _damage;
        private float _knockBackPower;
        private float _lifeTime;
        private float _createdAt;
        private Rect _moveRect;
        private float _poisonousAreaEffectDuration;
        private float _poisonousAreaEffectRadius;
        private float _poisonousAreaEffectDamage;

        private GameObject _bodyImage;
        private Collider2D[] _overlappedColliders;
        private RaycastHit2D[] _raycastHits;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.BeggarKingReflectionPoisonousAreaEffectObject);

            _bodyImage = ResourcePool.Instance.InstantiateFromResource("Stages/Projectiles/BottleRadius0_5.prefab");
            _bodyImage.transform.SetParent(this.transform);
            _bodyImage.transform.localPosition = Vector2.zero;
            _bodyImage.transform.localScale = Vector2.one;

            _overlappedColliders = new Collider2D[16];
            _raycastHits = new RaycastHit2D[16];
        }

        public void Initialize(
            Monster owner,
            float objectRadius,
            Vector2 startPosition,
            Vector2 movingDirection,
            float movingSpeed,
            float rotatingSpeed,
            float damage,
            float knockBackPower,
            float lifeTime,
            Rect moveRect,
            float poisonousAreaEffectDuration,
            float poisonousAreaEffectRadius,
            float poisonousAreaEffectDamage)
        {
            base.InitializeAreaObject(owner.Alliance);
            _owner = owner;
            _objectRadius = objectRadius;
            _movingDirection = movingDirection.normalized;
            _movingSpeed = movingSpeed;
            _rotatingSpeed = rotatingSpeed;
            _damage = damage;
            _knockBackPower = knockBackPower;
            _lifeTime = lifeTime;
            _moveRect = moveRect;

            _poisonousAreaEffectDuration = poisonousAreaEffectDuration;
            _poisonousAreaEffectRadius = poisonousAreaEffectRadius;
            _poisonousAreaEffectDamage = poisonousAreaEffectDamage;

            _createdAt = Time.time;
            this.transform.position = startPosition;

            float angle = Mathf.Rad2Deg * Mathf.Atan2(_movingDirection.y, _movingDirection.x);
            _bodyImage.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            _isAlive = true;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            CombatSystem.HandleAreaEffectCollisionWithCharacter(
                areaEffectObject: this,
                position: transform.position,
                radius: _objectRadius,
                direction: _movingDirection,
                speed: _movingSpeed,
                deltaTime: deltaTime,
                overlappedColliders: _overlappedColliders,
                raycastHits: _raycastHits,
                onHitCharacter: (character) =>
                {
                    character.Hitted(stage, _owner, _damage, _movingDirection, transform.position, hitSoundPrefabPath: string.Empty);
                    stage.CreatePoisonousAreaEffect(_owner, this.transform.position, _poisonousAreaEffectDuration, AreaEffectAttackPeriod, _poisonousAreaEffectDamage, _poisonousAreaEffectRadius);
                    _isAlive = false;
                });

            if (!IsAlive)
            {
                return;
            }

            this.Move(deltaTime);
            this.CheckAndReflect();
            this.Rotate(deltaTime);

            if (_createdAt + _lifeTime < Time.time)
            {
                stage.CreatePoisonousAreaEffect(_owner, this.transform.position, _poisonousAreaEffectDuration, AreaEffectAttackPeriod, _poisonousAreaEffectDamage, _poisonousAreaEffectRadius);
                _isAlive = false;
            }
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            //외부에서transform 사이즈 연출을 Dotween으로 처리하고있어 해당 코드를 추가했다.
            DOTween.Kill(this.transform);
        }

        private void Move(float deltaTime)
        {
            this.transform.Translate(deltaTime * _movingSpeed * _movingDirection);
        }

        private void CheckAndReflect()
        {
            Vector2 position = this.transform.position;
            bool isReflecting = false;

            if (position.x < _moveRect.xMin)
            {
                position.x = _moveRect.xMin;
                _movingDirection = Vector2.Reflect(_movingDirection, Vector2.right).normalized;
                isReflecting = true;
            }
            else if (position.x > _moveRect.xMax)
            {
                position.x = _moveRect.xMax;
                _movingDirection = Vector2.Reflect(_movingDirection, Vector2.left).normalized;
                isReflecting = true;
            }
            else if (position.y > _moveRect.yMax)
            {
                position.y = _moveRect.yMax;
                _movingDirection = Vector2.Reflect(_movingDirection, Vector2.down).normalized;
                isReflecting = true;
            }
            else if (position.y < _moveRect.yMin)
            {
                position.y = _moveRect.yMin;
                _movingDirection = Vector2.Reflect(_movingDirection, Vector2.up).normalized;
                isReflecting = true;
            }

            if (isReflecting)
            {
                this.transform.position = position;
                if (_rotatingSpeed == 0.0f)
                {
                    float angle = Mathf.Rad2Deg * Mathf.Atan2(_movingDirection.y, _movingDirection.x);
                    _bodyImage.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
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
    }
}
