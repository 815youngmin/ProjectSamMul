using DG.Tweening;
using Z.Animations.Placeholder;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class KennyoSpinBeads : AreaEffectObjectBase
    {
        public override bool IsAlive => !_isHitEnd || _hitEndAt + _disappearTime > Time.time;

        private Character _owner;
        private float _damage;
        private Vector2 _startPosition;

        private GameObject _body;

        private Vector2 _direction;

        private float _currentRadius;
        private float _currentAngle;

        private float _radiusUpSpeed;
        private float _angleUpSpeed;

        private float _radiusUpAcceleration;
        private float _angleUpAcceleration;

        private float _maxRadius;
        private float _attackRadius;

        private float _hittedCharacterClearPeriod = 0.5f;
        private float _hittedCharacterClearAt;

        private HashSet<Character> _hittedCharacters;

        private bool _isHitEnd;
        private float _hitEndAt;
        private float _disappearTime = 0.3f;

        private Sequence _endAlphaSequence;
        private SkeletonAnimation _skeletonAnimation;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.KennyoSpinBeads);
            _body = ResourcePool.Instance.InstantiateFromResource("Stages/AreaEffects/SpinBeads.prefab");
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;
            _body.transform.localScale = Vector2.one;

            _hittedCharacters = new HashSet<Character>();

            _skeletonAnimation = _body.GetComponent<SkeletonAnimation>();

            _endAlphaSequence = DOTween.Sequence();
            _endAlphaSequence.Append(DOTween.To(() => _skeletonAnimation.skeleton.A, x => _skeletonAnimation.skeleton.A = x, 0f, _disappearTime));
            _endAlphaSequence.SetAutoKill(false);
            _endAlphaSequence.SetRecyclable(true);
            _endAlphaSequence.Pause();
        }

        public void Initialize(
            Character owner,
            float damage,
            Vector2 startPosition,
            float radiusUpSpeed,
            float angleUpSpeed,
            float radiusUpAccelration,
            float angleUpAccelration,
            float maxRadius,
            float attackRadius,
            Vector2 direction
            )
        {
            base.InitializeAreaObject(owner.Alliance);
            _owner = owner;

            _damage = damage;
            _startPosition = startPosition;
            this.transform.position = _startPosition;

            _radiusUpSpeed = radiusUpSpeed;
            _angleUpSpeed = angleUpSpeed;

            _currentAngle = 0;
            _currentRadius = 0;

            _radiusUpAcceleration = radiusUpAccelration;
            _angleUpAcceleration = angleUpAccelration;

            _maxRadius = maxRadius;
            _direction = direction;
            _isHitEnd = false;
            _hitEndAt = 0;

            _body.transform.localScale = Vector2.one * (attackRadius / 2.0f);
            _attackRadius = attackRadius;
            _endAlphaSequence.Pause();

        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            if (_isHitEnd)
            {
                return;
            }

            _currentAngle += _angleUpSpeed * deltaTime;
            _currentRadius += _radiusUpSpeed * deltaTime;

            _angleUpSpeed += _angleUpAcceleration * deltaTime;
            _radiusUpSpeed += _radiusUpAcceleration * deltaTime;

            if (_currentRadius < _maxRadius)
            {
                Vector2 anglePos = Quaternion.Euler(0, 0, _currentAngle) * _direction * _currentRadius;
                this.transform.position = _startPosition + anglePos;
            }
            else
            {
                _isHitEnd = true;
                _hitEndAt = Time.time + _disappearTime;
                _endAlphaSequence.Restart();
            }

            if (_hittedCharacterClearAt < Time.time)
            {
                _hittedCharacters.Clear();
                _hittedCharacterClearAt = Time.time + _hittedCharacterClearPeriod;
            }

            CircularTargetArea area = new CircularTargetArea(this.transform.position, _attackRadius);
            CombatSystem.HitOnTargetArea(
                stage, area, _owner, _damage, CombatSystem.KnockBackType.Pivot,
                _owner.Pos, 0f, _hittedCharacters, _hittedCharacters
                , null);

        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _hittedCharacters.Clear();
            _endAlphaSequence.Pause();

            _skeletonAnimation.skeleton.A = 1f;

        }

    }

}
