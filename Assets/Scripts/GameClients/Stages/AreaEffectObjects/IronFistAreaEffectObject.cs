using DG.Tweening;
using Shared.GameDataTypes;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.Characters.PCs;
using Z.GameClients.Stages.ItemObjects;
using Z.ResourcePools;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class IronFistAreaEffectObject : AreaEffectObjectBase
    {
        private const string TRANSCENDENT_BODY_PATH = "Stages/AreaEffects/IronFists/IronFistTranscendent.prefab";
        private const string NORMAL_BODY_PATH = "Stages/AreaEffects/IronFists/IronFistNormal.prefab";

        private static readonly float TRANSCENDENT_OBJECT_HITTED_CHARACTERS_CLEAR_PERIOD = 0.75f;
        private static readonly float WIND_FIELD_DAMAGE_PER_TICK_COEFFICIENT = 0.04f;
        private static readonly float WIND_FIELD_TICK_PERIOD = 0.25f;
        private static readonly float WIND_FIELD_LIFETIME = 2.0f;

        public override bool IsAlive => Time.time < _expiresAt && _hitCount > 0;

        private Transform _bodyTransform;
        private HashSet<Character> _hittedCharacters;
        private Collider2D[] _overlappedColliders;
        private RaycastHit2D[] _raycastHits;
        private int _layserMask;

        private PlayerCharacter _owner;
        private Vector2 _movingDirection;
        private float _movingSpeed;
        private float _attackDamage;
        private float _attackRadius;
        private float _knockbackPower;
        private float _expiresAt;
        private int _hitCount;
        private bool _isTranscendent;

        private float _clearHittedCharactersAt;

        // 마지막으로 타격한 시각. 정지연출을 위해 사용
        private float _lastHitAt;
        private const float HIT_STOP_DURATION = 0.093f;

        public void AllocateSharedResources(bool isTranscendent)
        {
            base.AllocateSharedResourcesForBase(isTranscendent ? AreaEffectType.IronFistTranscendent : AreaEffectType.IronFist);

            _bodyTransform = ResourcePool.Instance.InstantiateFromResource(isTranscendent ? TRANSCENDENT_BODY_PATH : NORMAL_BODY_PATH).transform;
            _bodyTransform.SetParent(transform);
            _bodyTransform.localPosition = Vector3.zero;
            _bodyTransform.localScale = (isTranscendent ? 0.5f : 1.0f) * Vector3.one;

            _hittedCharacters = new HashSet<Character>();
            _overlappedColliders = new Collider2D[16];
            _raycastHits = new RaycastHit2D[16];
            _layserMask = Physics2D.GetLayerCollisionMask(LayerMask.NameToLayer("Projectile"));

            _lastHitAt = 0f;

        }

        public void Initialize(
            Stage stage,
            PlayerCharacter owner,
            Vector2 position,
            Vector2 movingDirection,
            float movingSpeed,
            float attackDamage,
            float attackRadius,
            float knockbackPower,
            float lifetime,
            int hitCount,
            bool isTranscendent,
            bool withWindField,
            bool removePoisons)
        {
            base.InitializeAreaObject(owner.Alliance);

            float now = Time.time;

            _owner = owner;
            _movingDirection = movingDirection;
            _movingSpeed = movingSpeed;
            _attackDamage = attackDamage;
            _attackRadius = attackRadius;
            _knockbackPower = knockbackPower;
            _expiresAt = now + lifetime;
            _hitCount = hitCount;
            _isTranscendent = isTranscendent;

            transform.position = position;
            var finalScale = _attackRadius * Vector3.one;
            transform.localScale = finalScale;
            _bodyTransform.rotation = Quaternion.Euler(0.0f, 0.0f, Vector2.SignedAngle(Vector2.right, _movingDirection));

            _hittedCharacters.Clear();
            _clearHittedCharactersAt = now + TRANSCENDENT_OBJECT_HITTED_CHARACTERS_CLEAR_PERIOD;

            this.transform.DOKill();
            this.transform.localScale = finalScale * 0.12f;
            this.transform.DOScale(finalScale, 0.33f);

            if (withWindField)
            {
                stage.CreateWindFieldAreaEffect(
                    ironFist: this,
                    owner: _owner,
                    position: transform.position,
                    movingDirection: _movingDirection,
                    movingSpeed: _movingSpeed,
                    height: 2.0f * _attackRadius,
                    damagePerTick: WIND_FIELD_DAMAGE_PER_TICK_COEFFICIENT * _attackDamage,
                    tickPeriod: WIND_FIELD_TICK_PERIOD,
                    lifetime: _owner.Stats.DurationIncreaseRate.Value * WIND_FIELD_LIFETIME,
                    removePoisons: removePoisons);
            }
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;

            // 초월 공격은 같은 캐릭터를 여러 번 공격할 수 있도록 주기적으로 공격한 캐릭터를 초기화한다.
            if (_isTranscendent && _clearHittedCharactersAt < now)
            {
                _hittedCharacters.Clear();
                _clearHittedCharactersAt += TRANSCENDENT_OBJECT_HITTED_CHARACTERS_CLEAR_PERIOD;
            }

            _ = this.HandleCharacterCollisions(stage, deltaTime);

            // 적을 타격한 뒤 일정시간동안은 움직이지 않는다. 타격 연출을 위함.
            if (now >= (_lastHitAt + HIT_STOP_DURATION))
            {
                this.Move(deltaTime);
            }
        }

        private bool HandleCharacterCollisions(Stage stage, float deltaTime)
        {
            bool hit = false;

            // 콜라이더로 충돌 처리.
            if (_hitCount <= 0)
            {
                return hit;
            }
            int overlappedCollidersCount = Physics2D.OverlapCircleNonAlloc(transform.position, _attackRadius, _overlappedColliders, _layserMask);
            for (int i = 0; i < overlappedCollidersCount && _hitCount > 0; ++i)
            {
                hit |= this.TryHit(stage, _overlappedColliders[i]);
            }

            // 레이캐스트로 충돌 처리.
            if (_hitCount <= 0)
            {
                return hit;
            }
            int raycastHitsCount = Physics2D.RaycastNonAlloc(transform.position, _movingDirection, _raycastHits, deltaTime * _movingSpeed, _layserMask);
            for (int i = 0; i < raycastHitsCount && _hitCount > 0; ++i)
            {
                hit |= this.TryHit(stage, _raycastHits[i].collider);
            }

            if (hit)
            {
                _lastHitAt = Time.time;

                DOTween.Kill(this);
                if (!_isTranscendent)
                {
                    DOTween.Sequence(this)
                        .Append(this.transform.DOScale(_attackRadius * 1.15f, 0.033f))
                        .AppendInterval(0.066f)
                        .Append(this.transform.DOScale(_attackRadius * 0.90f, 0.016f))
                        .AppendInterval(0.033f)
                        .Append(this.transform.DOScale(_attackRadius * 1.10f, 0.016f))
                        .AppendInterval(0.033f)
                        .Append(this.transform.DOScale(_attackRadius * 0.95f, 0.016f))
                        .AppendInterval(0.008f)
                        .Append(this.transform.DOScale(_attackRadius * 1.0f, 0.016f));
                }
                else
                {
                    DOTween.Sequence(this)
                        .Append(this.transform.DOScale(_attackRadius * 1.075f, 0.033f))
                        .AppendInterval(0.066f)
                        .Append(this.transform.DOScale(_attackRadius * 0.95f, 0.016f))
                        .AppendInterval(0.016f)
                        .Append(this.transform.DOScale(_attackRadius * 1.05f, 0.016f))
                        .AppendInterval(0.016f)
                        .Append(this.transform.DOScale(_attackRadius * 0.98f, 0.016f))
                        .AppendInterval(0.008f)
                        .Append(this.transform.DOScale(_attackRadius * 1.0f, 0.016f));
                }
            }

            return hit;
        }

        private bool TryHit(Stage stage, Collider2D collider)
        {
            if (_hitCount <= 0)
            {
                return false;
            }

            if (collider == null)
            {
                return false;
            }

            if (collider.gameObject.layer == LayerMask.NameToLayer("CharacterHitBox"))
            {
                var character = collider.transform.parent.GetComponent<Character>();
                if (character.Alliance == _owner.Alliance)
                {
                    return false;
                }

                if (_hittedCharacters.Contains(character))
                {
                    return false;
                }

                character.Hitted(stage, _owner, _attackDamage, _knockbackPower * _movingDirection, transform.position, null);
                _hittedCharacters.Add(character);
                --_hitCount;
                return true;
            }

            if (collider.gameObject.layer == LayerMask.NameToLayer("BreakableItem"))
            {
                var breakableItemObject = collider.GetComponent<BreakableItemObject>();
                if (breakableItemObject.DropItemType == DropItemType.Fence)
                {
                    return false;
                }

                breakableItemObject.OnBroken(_owner, _attackDamage, stage);
                --_hitCount;
                return true;
            }

            return false;
        }

        private void Move(float deltaTime)
        {
            this.transform.Translate(deltaTime * _movingSpeed * _movingDirection);
        }
    }
}