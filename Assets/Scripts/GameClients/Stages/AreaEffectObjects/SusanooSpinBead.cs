using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class SusanooSpinBead : AreaEffectObjectBase
    {
        public override bool IsAlive => !_isHitEnd;

        private Character _owner;
        private float _damage;

        private GameObject _body;

        private Vector2 _direction;
        private Vector2 _center;

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
        private float _fadeInDuration;
        private float _waitDuration;
        private float _fadeOutDuration;

        private float _spinAt;

        private Sequence _fadeInSequence;
        private Sequence _fadeOutSequence;


        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.SusanooSpinBead);
            _body = ResourcePool.Instance.InstantiateFromResource("Stages/Projectiles/Susanoo_w.prefab");
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;
            _body.transform.localScale = Vector2.one;
        }

        public void Initialize(
            Character owner,
            float fadeInDuration,       //등장시 페이드인 연출 시간
            float waitDuration,         //등장후 대기시간
            float fadeOutDuration,      //퇴장시 페이드아웃 연출 시간
            float damage,
            Vector2 center,
            float startAngle,
            float startRadius,
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
            _center = center;

            _radiusUpSpeed = radiusUpSpeed;
            _angleUpSpeed = angleUpSpeed;

            _currentAngle = startAngle;
            _currentRadius = startRadius;

            _radiusUpAcceleration = radiusUpAccelration;
            _angleUpAcceleration = angleUpAccelration;

            _maxRadius = maxRadius;
            _direction = direction;
            _isHitEnd = false;

            _body.transform.localScale = Vector2.one * (attackRadius / 0.615f);
            _attackRadius = attackRadius;

            _fadeInDuration = fadeInDuration;
            _fadeOutDuration = fadeOutDuration;
            _waitDuration = waitDuration;

            _spinAt = Time.time + _waitDuration + _fadeInDuration;

            _hittedCharacters = new HashSet<Character>();

            _fadeInSequence = DOTween.Sequence(this);
            _fadeInSequence.Append(_body.GetComponent<SpriteRenderer>().DOFade(1f, _fadeInDuration).From(0f));
            _fadeInSequence.Pause();

            _fadeOutSequence = DOTween.Sequence(this);
            _fadeOutSequence.Append(_body.GetComponent<SpriteRenderer>().DOFade(0f, _fadeOutDuration).From(1f));
            _fadeOutSequence.OnComplete(() =>
            {
                _isHitEnd = true;
            });
            _fadeOutSequence.Pause();

            _fadeInSequence.Restart();

            Vector2 anglePos = Quaternion.Euler(0, 0, _currentAngle) * _direction * _currentRadius;
            this.transform.position = _center + anglePos;
        }


        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;

            if (_isHitEnd)
            {
                return;
            }

            if(now < _spinAt)
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
                this.transform.position = _center+ anglePos;
            }
            else
            {
                if (!_fadeOutSequence.IsPlaying())
                {
                    _fadeOutSequence.Restart();
                }
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

            _fadeInSequence.Kill();
            _fadeOutSequence.Kill();
        }

    }

}
