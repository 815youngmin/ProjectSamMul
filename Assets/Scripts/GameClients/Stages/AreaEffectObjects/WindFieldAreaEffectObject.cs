using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.Characters.PCs;
using Z.GameClients.Stages.Characters.StatusEffects;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class WindFieldAreaEffectObject : AreaEffectObjectBase
    {
        private const string BODY_PATH = "Stages/AreaEffects/IronFists/WindField.prefab";
        private const string EFFECT_PATH = "Stages/AreaEffects/IronFists/WindFieldEffect.prefab";

        public override bool IsAlive => _isAlive;

        private SpriteRenderer _bodySpriteRenderer;
        private SpriteRenderer _effectSpriteRenderer;

        private IronFistAreaEffectObject _ironFist;
        private PlayerCharacter _owner;
        private Vector2 _createdPosition;
        private Vector2 _movingDirection;
        private float _movingSpeed;
        private float _height;
        private float _damagePerTick;
        private float _tickPeriod;
        private float _lifetime;
        private bool _removePoisons;

        private float _angle;
        private float _attackAt;
        private float _ironFistCreatedAt;
        private float _ironFistExpiredAt;
        private bool _isAlive;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.WindField);

            var bodyTransform = ResourcePool.Instance.InstantiateFromResource(BODY_PATH).transform;
            bodyTransform.SetParent(transform);
            bodyTransform.localPosition = Vector3.zero;
            bodyTransform.localScale = new Vector3(0.25f, 0.35f, 1.0f);
            _bodySpriteRenderer = bodyTransform.GetComponent<SpriteRenderer>();

            var effectTransform = ResourcePool.Instance.InstantiateFromResource(EFFECT_PATH).transform;
            effectTransform.localPosition = Vector3.zero;
            effectTransform.localScale = new Vector3(0.4f, 0.4f, 1.0f);
            _effectSpriteRenderer = effectTransform.GetComponent<SpriteRenderer>();

        }

        public void Initialize(
            IronFistAreaEffectObject ironFist,
            PlayerCharacter owner,
            Vector2 position,
            Vector2 movingDirection,
            float movingSpeed,
            float height,
            float damagePerTick,
            float tickPeriod,
            float lifetime,
            bool removePoisons)
        {
            base.InitializeAreaObject(owner.Alliance);

            float now = Time.time;

            _ironFist = ironFist;
            _owner = owner;
            _createdPosition = position;
            _movingDirection = movingDirection;
            _movingSpeed = movingSpeed;
            _height = height;
            _damagePerTick = damagePerTick;
            _tickPeriod = tickPeriod;
            _lifetime = lifetime;
            _removePoisons = removePoisons;

            _angle = Vector2.SignedAngle(Vector2.right, _movingDirection);
            var rotation = Quaternion.Euler(0.0f, 0.0f, _angle);

            transform.SetPositionAndRotation(_createdPosition, rotation);
            transform.localScale = new Vector3(0.0f, _height, 1.0f);

            _effectSpriteRenderer.gameObject.SetActive(true);
            _effectSpriteRenderer.transform.SetPositionAndRotation(_createdPosition, rotation);
            _effectSpriteRenderer.transform.localScale = new Vector3(0.4f * _height, 0.4f * _height, 1.0f);

            _attackAt = now + _tickPeriod;
            _ironFistCreatedAt = now;
            _ironFistExpiredAt = float.MaxValue;
            _isAlive = true;
        }


        private HashSet<Character> v_hittedCharacters = new HashSet<Character>();
        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;

            if (_ironFistExpiredAt == float.MaxValue && !_ironFist.IsAlive)
            {
                _ironFistExpiredAt = now;
            }

            float startAt = Mathf.Max(now - _lifetime, _ironFistCreatedAt) - _ironFistCreatedAt;
            float endAt = Mathf.Min(now, _ironFistExpiredAt) - _ironFistCreatedAt;
            float width = (endAt - startAt) * _movingSpeed;
            transform.localScale = new Vector3(width, _height, 1.0f);
            _effectSpriteRenderer.size = new Vector2(width / (0.4f * _height), 5.0f);

            Vector2 startPosition = _createdPosition + startAt * _movingSpeed * _movingDirection;
            Vector2 endPosition = _createdPosition + endAt * _movingSpeed * _movingDirection;
            Vector2 position = 0.5f * (startPosition + endPosition);
            transform.position = position;
            _effectSpriteRenderer.transform.position = position;

            _isAlive = startAt <= endAt;

            if (now < _attackAt)
            {
                return;
            }

            v_hittedCharacters.Clear();
            var targetArea = new SquareTargetArea(transform.position, transform.localScale, _angle);
            CombatSystem.HitOnTargetArea(stage, targetArea, _owner, _damagePerTick, knockBackType: CombatSystem.KnockBackType.Pivot, Vector2.zero, 0.0f, v_hittedCharacters, null, null);
            foreach(var target in v_hittedCharacters)
            {
                if (target.IsBoss)
                {
                    continue;
                }

                float moveSpeedChangeRatio = target.IsElite ? 0.66f : 0.33f;
                target.StatusEffects.AddOrUpdateStatusEffect(stage, target, StatusEffectType.SlowMove, "WindFieldAreaEffectSlow", duration: 1.3f, now, moveSpeedChangeRatio);
            }

            if (_removePoisons)
            {
                stage.ForAllAliveAreaEffects((areaEffect) =>
                {
                    if (areaEffect.AreaEffectObjectType != AreaEffectType.PoisonousArea)
                    {
                        return true;
                    }

                    var poisonousAreaEffect = areaEffect as PoisonousAreaEffectObject;
                    if (poisonousAreaEffect == null)
                    {
                        return true;
                    }

                    if (!targetArea.Contains(poisonousAreaEffect.Center, poisonousAreaEffect.Radius))
                    {
                        return true;
                    }

                    poisonousAreaEffect.TryDisappear();
                    return true;
                });
            }
            _attackAt += _tickPeriod;
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _effectSpriteRenderer.gameObject.SetActive(false);
        }
    }
}
