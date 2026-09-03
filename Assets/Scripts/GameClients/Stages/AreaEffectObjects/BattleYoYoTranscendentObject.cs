using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.Characters.PCs;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class BattleYoYoTranscendentObject : AreaEffectObjectBase
    {
        public override bool IsAlive => !_isHitEnd || _hitEndAt + _disappearTime > Time.time;

        private PlayerCharacter _owner;
        private float _damage;

        private GameObject _body;

        private Vector2 _direction;

        private float _currentRadius;
        private float _currentAngle;

        private float _radiusUpSpeed;
        private float _angleUpSpeed;

        private float _radiusUpAcceleration;
        private float _angleUpAcceleration;

        private float _knockBackPower;

        private float _maxRadius;
        private float _attackRadius;

        private float _hittedCharacterClearPeriod = 8.5f;
        private float _hittedCharacterClearAt;

        private HashSet<Character> _hittedCharacters;

        private bool _isHitEnd;
        private float _hitEndAt;
        private float _disappearTime = 0.3f;

        private Sequence _endAlphaSequence;
        private SpriteRenderer _spriteRenderer;
        private TrailRenderer _trailRenderer;

        private Color _saveSpriteRendererColor;
        private Color _savetrailRendererStartColor;
        private Color _savetrailRendererEndColor;

        private string _soundPrefabPath;

        public void AllocateSharedResources(AreaEffectType areaEffectType)
        {
            Debug.Assert(areaEffectType == AreaEffectType.BattleYoYoTranscendent_Default || areaEffectType == AreaEffectType.BattleYoYoTranscendent_EggKim);
            base.AllocateSharedResourcesForBase(areaEffectType);

            var bodyPrefabPath = areaEffectType switch
            {
                AreaEffectType.BattleYoYoTranscendent_Default => "Stages/AreaEffects/yoyo_skill_S.prefab",
                AreaEffectType.BattleYoYoTranscendent_EggKim => "Stages/AreaEffects/Yoyo_EggKim_Transcendent.prefab",
                _ => throw new NotSupportedException($"AreaEffectType {areaEffectType}이 배틀 요요 초월이 아닙니다."),
            };
            _body = ResourcePool.Instance.InstantiateFromResource(bodyPrefabPath);
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;
            _body.transform.localScale = Vector2.one;

            _hittedCharacters = new HashSet<Character>();

            _spriteRenderer = _body.GetComponentInChildren<SpriteRenderer>();
            _trailRenderer = _body.GetComponentInChildren<TrailRenderer>();

            //알파값이 정해진다음 값을 저장한다.
            _saveSpriteRendererColor = _spriteRenderer.color;
            _savetrailRendererStartColor = _trailRenderer.startColor;
            _savetrailRendererEndColor = _trailRenderer.endColor;

            _endAlphaSequence = DOTween.Sequence();
            _endAlphaSequence.Append(_spriteRenderer.DOFade(0f, _disappearTime));
            _endAlphaSequence.Append(DOTween.To(() => _trailRenderer.startColor, x => _trailRenderer.startColor = x, new Color(_trailRenderer.startColor.r, _trailRenderer.startColor.g, _trailRenderer.startColor.b, 0f), _disappearTime));
            _endAlphaSequence.Append(DOTween.To(() => _trailRenderer.endColor, x => _trailRenderer.endColor = x, new Color(_trailRenderer.endColor.r, _trailRenderer.endColor.g, _trailRenderer.endColor.b, 0f), _disappearTime));
            _endAlphaSequence.SetAutoKill(false);
            _endAlphaSequence.SetRecyclable(true);
            _endAlphaSequence.Pause();
        }

        public void Initialize(
            PlayerCharacter owner,
            float damage,
            Vector2 startPosition,
            float radiusUpSpeed,
            float angleUpSpeed,
            float radiusUpAccelration,
            float angleUpAccelration,
            float maxRadius,
            float attackRadius,
            float knockBackPower,
            Vector2 direction,
            string soundPrefabPath)
        {
            base.InitializeAreaObject(owner.Alliance);
            _owner = owner;

            _damage = damage;
            this.transform.position = startPosition;

            _radiusUpSpeed = radiusUpSpeed;
            _angleUpSpeed = angleUpSpeed;

            _currentAngle = 0;
            _currentRadius = 0;

            _radiusUpAcceleration = radiusUpAccelration;
            _angleUpAcceleration = angleUpAccelration;

            _maxRadius = maxRadius;
            _knockBackPower = knockBackPower;
            _direction = direction;
            _isHitEnd = false;
            _hitEndAt = 0;

            _body.transform.localScale = Vector2.one * (attackRadius / 0.8f);  //반지름 0.8f가 기본 사이즈 그보다 크면 이미지 크기를 키워준다
            _attackRadius = attackRadius;
            _endAlphaSequence.Pause();

            _soundPrefabPath = soundPrefabPath;
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
                this.transform.position = _owner.Pos + anglePos;
            }
            else if (_currentRadius >= _maxRadius * 2)
            {
                _isHitEnd = true;
                _hitEndAt = Time.time;
            }
            else
            {
                Vector2 anglePos = Quaternion.Euler(0, 0, _currentAngle) * _direction * (_maxRadius + _maxRadius - _currentRadius);
                this.transform.position = _owner.Pos + anglePos;
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
                _owner.Pos, _knockBackPower, 
                hittedCharacterCollector: _hittedCharacters,
                exceptedCharacters: _hittedCharacters
                , _soundPrefabPath);

        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _hittedCharacters.Clear();
            _endAlphaSequence.Pause();

            _spriteRenderer.color = _saveSpriteRendererColor;
            _trailRenderer.startColor = _savetrailRendererStartColor;
            _trailRenderer.endColor = _savetrailRendererEndColor;
        }

    }

}
