using DG.Tweening;
using SamMul.Animations.Placeholder;
using System;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class SpineBodyAreaEffectObject : SmartAreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + _lifeTime + _indicatorDuration + _delay;
        
        private HashSet<Character> _hittedCharacters;

        private Character _owner;
        private float _delay;
        private float _indicatorDuration;
        private float _objectRadius;
        private float _damage;
        private float _lifeTime;
        private float _createdAt;
        private float _canHitAt;
        private float _tickDamagePeriod = 0.25f;
        private float _tickDamageClearNextAt;

        private Sequence _fadeInSequence;
        private Sequence _fadeOutSequence;
        private readonly float _fadeOutSequenceDuration = 0.5f;

        private GameObject _body;
        private SkeletonAnimation _skeletonAnimation;

        private bool _isFadeOutSequencePlayed;
        private bool _isCanHit;

        private float _indicatorAt;

        public void AllocateSharedResources(AreaEffectType areaEffectType)
        {
            base.AllocateSharedResourcesForSmartBase(areaEffectType);

            switch (areaEffectType)
            {
                case AreaEffectType.RaAreaEffectObject:
                case AreaEffectType.HenriIISandAreaEffect:
                    _body = ResourcePool.Instance.InstantiateFromResource("Stages/AreaEffects/BossRa_field.prefab");
                    break;
                case AreaEffectType.FreyaMagicCircle:
                    _body = ResourcePool.Instance.InstantiateFromResource("Stages/Characters/SpineSkeletons/Viking/Viking_BossFreya/FreyaMagicCircle.prefab");
                    break;
                default:
                    throw new NotImplementedException($"{areaEffectType} 구현 안됨. 구현해주세요.");
            }

            _skeletonAnimation = _body.GetComponentInChildren<SkeletonAnimation>();
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector3.zero;
            _body.transform.localScale = Vector3.one;

            _body.gameObject.SetActive(false);
            _hittedCharacters = new HashSet<Character>();

            this.AllocateSequenceAnimation();
        }

        private void AllocateSequenceAnimation()
        {
            _fadeInSequence = DOTween.Sequence();
            _fadeInSequence.Append(DOTween.To(() => 0f, x => _skeletonAnimation.skeleton.A = x, 1f, 0.5f));
            _fadeInSequence.SetRecyclable(true);
            _fadeInSequence.SetAutoKill(false);
            _fadeInSequence.Pause();

            _fadeOutSequence = DOTween.Sequence();
            _fadeOutSequence.Append(DOTween.To(() => 1f, x => _skeletonAnimation.skeleton.A = x, 0f, 0.5f));
            _fadeOutSequence.SetRecyclable(true);
            _fadeOutSequence.SetAutoKill(false);
            _fadeOutSequence.Pause();
        }

        public void Initialize(
            Character owner,
            float delay,
            float indicatorDuration,
            float damage,
            float attackPeriod,
            float lifeTime,
            float objectRadius,
            Vector2 position
            )
        {
            base.InitializeAreaObject(owner.Alliance);

            float now = Time.time;

            _createdAt = now;
            _owner = owner;
            _delay = delay;
            _indicatorDuration = indicatorDuration;
            _damage = damage;
            _objectRadius = objectRadius;
            _tickDamagePeriod = attackPeriod;

            _tickDamageClearNextAt = now;

            this.transform.position = position;
            _isFadeOutSequencePlayed = false;

            _body.gameObject.SetActive(false);
            this.InitializeBodyScale(AreaEffectObjectType);
            _body.GetComponent<MeshRenderer>().sortingOrder = (int)(this.transform.position.y * -100.0f);
            _isCanHit = false;

            _lifeTime = lifeTime;
            _canHitAt = now +  _delay + indicatorDuration;
            _indicatorAt = now + _delay;
        }

        //타입별 리소스 사이즈가 달라 스케일 조절은 여기서 처리한다.
        private void InitializeBodyScale(AreaEffectType areaEffectType)
        {
            switch (areaEffectType)
            {
                case AreaEffectType.RaAreaEffectObject:
                case AreaEffectType.HenriIISandAreaEffect:
                    _body.transform.localScale = Vector3.one * _objectRadius / 4f;
                    break; 
                case AreaEffectType.FreyaMagicCircle:
                    _body.transform.localScale = Vector3.one * _objectRadius / 2.5f;
                    break;
                default:
                    _body.transform.localScale = Vector3.one * _objectRadius;
                    break;
            }
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _owner = null;
            _hittedCharacters.Clear();

            _fadeInSequence.Pause();
            _fadeOutSequence.Pause();
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            base.UpdateLogic(stage, deltaTime);
            float now = Time.time;

            if (now > _indicatorAt)
            {
                stage.CreateBlinkCircularAttackRangeIndicator(this.transform.position, _objectRadius, _indicatorDuration);
                _indicatorAt = float.MaxValue;
            }

            if (_canHitAt <= now && !_isCanHit)
            {
                _body.gameObject.SetActive(true);
                _fadeInSequence.Restart();
                _isCanHit = true;
            }

            this.UpdateAttacked(stage, now);
            if (_createdAt + _delay +  _lifeTime + _indicatorDuration - _fadeOutSequenceDuration < now && !_isFadeOutSequencePlayed)
            {
                _fadeOutSequence.Restart();
                _isFadeOutSequencePlayed = true;
            }
        }

        private void UpdateAttacked(Stage stage, float now)
        {
            if(!_isCanHit)
            {
                return;
            }

            if (_tickDamageClearNextAt <= now)
            {
                _hittedCharacters.Clear();
                _tickDamageClearNextAt = now + _tickDamagePeriod;
            }
 
            CircularTargetArea area = new CircularTargetArea(this.transform.position, _objectRadius);
            CombatSystem.HitOnTargetArea(stage, area, _owner, _damage, CombatSystem.KnockBackType.Pivot, Vector2.zero, 0f, _hittedCharacters, _hittedCharacters, string.Empty);
        }
    }

}
