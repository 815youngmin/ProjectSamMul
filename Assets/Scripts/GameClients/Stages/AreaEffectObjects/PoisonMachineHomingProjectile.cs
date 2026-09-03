using DG.Tweening;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.Characters.Monsters;
using Z.ResourcePools;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class PoisonMachineHomingProjectile : AreaEffectObjectBase
    {
        private const string PROJECTILE_BODY_PREFAB_PATH = "Stages/AreaEffects/PoisonEffects/PoisonBall.prefab";

        public override bool IsAlive => _isAlive;

        private GameObject _body;
        private Collider2D[] _overlappedColliders;
        private RaycastHit2D[] _raycastHits;

        private Monster _owner;
        private Character _target;
        private float _projectileSpeed;
        private float _projectileLifetime;
        private float _projectileDamage;
        private float _projectileRadius;
        private float _areaEffectCreationPeriod;
        private float _areaEffectLifetime;
        private float _areaEffectTickPeriod;
        private float _areaEffectDamagePerTick;
        private float _areaEffectRadius;

        private float _createdAt;
        private float _createAreaEffectAt;
        private bool _isAlive;
        private Vector2 _movingDirection;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.PoisonMachineHomingProjectile);
            _body = ResourcePool.Instance.InstantiateFromResource(PROJECTILE_BODY_PREFAB_PATH);
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;
            _body.transform.localScale = 1.5f * Vector2.one;

            _overlappedColliders = new Collider2D[16];
            _raycastHits = new RaycastHit2D[16];
        }

        public void Initialize(
            Monster owner,
            Character target,
            Vector2 startPosition,
            float projectileSpeed,
            float projectileLifetime,
            float projectileDamage,
            float projectileRadius,
            float areaEffectCreationPeriod,
            float areaEffectLifetime,
            float areaEffectTickPeriod,
            float areaEffectDamagePerTick,
            float areaEffectRadius)
        {
            base.InitializeAreaObject(owner.Alliance);
            this.transform.position = startPosition;

            _owner = owner;
            _target = target;
            _projectileSpeed = projectileSpeed;
            _projectileLifetime = projectileLifetime;
            _projectileDamage = projectileDamage;
            _projectileRadius = projectileRadius;
            _areaEffectCreationPeriod = areaEffectCreationPeriod;
            _areaEffectLifetime = areaEffectLifetime;
            _areaEffectTickPeriod = areaEffectTickPeriod;
            _areaEffectDamagePerTick = areaEffectDamagePerTick;
            _areaEffectRadius = areaEffectRadius;

            _createdAt = Time.time;
            _createAreaEffectAt = _createdAt + _areaEffectCreationPeriod;
            _isAlive = true;
            _movingDirection = Vector2.zero;

            DOTween.Sequence(this)
                .Append(this.transform.DOScale(1.2f, 0.15f * _areaEffectCreationPeriod).From(1.0f))
                .Append(this.transform.DOScale(1.0f, 0.15f * _areaEffectCreationPeriod).From(1.2f))
                .Append(this.transform.DOScale(1.2f, 0.15f * _areaEffectCreationPeriod).From(1.0f))
                .Append(this.transform.DOScale(1.0f, 0.15f * _areaEffectCreationPeriod).From(1.2f))
                .AppendInterval(0.4f * _areaEffectCreationPeriod)
                .SetLoops(-1);
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;

            if (_createdAt + _projectileLifetime < now && _isAlive)
            {
                DOTween.Kill(this);
                this.CreatePoisonousAreaEffect(stage, 2.0f * _areaEffectRadius);
                _isAlive = false;
                return;
            }

            if (_createAreaEffectAt < now && _isAlive)
            {
                this.CreatePoisonousAreaEffect(stage, _areaEffectRadius);
                _createAreaEffectAt += _areaEffectCreationPeriod;
            }

            _movingDirection = (_target.Pos - (Vector2)this.transform.position).normalized;
            this.transform.Translate(deltaTime * _projectileSpeed * _movingDirection);
            this.HandleCharacterCollisions(stage, deltaTime);
        }

        private void CreatePoisonousAreaEffect(Stage stage, float radius)
        {
            stage.CreatePoisonousAreaEffect(
                _owner,
                this.transform.position,
                _areaEffectLifetime,
                _areaEffectTickPeriod,
                _areaEffectDamagePerTick,
                radius);
        }

        private void HandleCharacterCollisions(Stage stage, float deltaTime)
        {
            if (!IsAlive)
            {
                return;
            }

            int overlappedColliderscount = Physics2D.OverlapCircleNonAlloc(transform.position, _projectileRadius, _overlappedColliders);
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

                character.Hitted(stage, _owner, _projectileDamage, _movingDirection, transform.position, string.Empty);
                this.CreatePoisonousAreaEffect(stage, 2.0f * _areaEffectRadius);
                _isAlive = false;
                return;
            }

            var projectileLayer = LayerMask.NameToLayer("Projectile");
            int collidibleLayers = Physics2D.GetLayerCollisionMask(projectileLayer);
            float moveDistance = deltaTime * _projectileSpeed;

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

                character.Hitted(stage, _owner, _projectileDamage, _movingDirection, transform.position, string.Empty);
                this.CreatePoisonousAreaEffect(stage, 2.0f * _areaEffectRadius);
                _isAlive = false;
                return;
            }
        }
    }
}
