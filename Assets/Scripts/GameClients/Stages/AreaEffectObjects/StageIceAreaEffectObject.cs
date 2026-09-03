using DG.Tweening;
using SamMul.Animations.Placeholder;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class StageIceAreaEffectObject : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + _lifeTime + _indicatorDuration;
        
        private HashSet<Character> _slowedCharacters;

        private AllianceType _allianceType;
        private float _indicatorDuration;
        private float _objectRadius;
        private float _lifeTime;
        private float _createdAt;
        private float _canSlowAt;
        private float _slowRate;
        private float _slowDuration;

        private float _tickSlowClearNextAt;
        private readonly float _tickSlowPeriod = 0.25f;

        private Sequence _fadeInSequence;
        private Sequence _fadeOutSequence;
        private readonly float _fadeOutSequenceDuration = 0.5f;

        private GameObject _body;
        private SkeletonAnimation _skeletonAnimation;

        private bool _isFadeOutSequencePlayed;
        private bool _isCanSlow;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.StageSandAreaEffectObject);

            _body = ResourcePool.Instance.InstantiateFromResource("Stages/AreaEffects/BossRa_field.prefab");
            _skeletonAnimation = _body.GetComponentInChildren<SkeletonAnimation>();
            _skeletonAnimation.Skeleton.SetColor(new Color(0.3f, 0.3f, 1f));
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector3.zero;
            _body.transform.localScale = Vector3.one;

            _body.gameObject.SetActive(false);
            _slowedCharacters = new HashSet<Character>();

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
            Stage stage,
            AllianceType allianceType,
            float indicatorDuration,
            Vector2 position,
            float objectRadius,
            float objectDuration,
            float slowRate,
            float slowDuration
            )
        {
            base.InitializeAreaObject(allianceType);

            float now = Time.time;

            _allianceType = allianceType;
            _createdAt = now;
            _indicatorDuration = indicatorDuration;
            _lifeTime = objectDuration;
            _objectRadius = objectRadius;
            _slowRate = slowRate;
            _slowDuration = slowDuration;

            _tickSlowClearNextAt = now;

            this.transform.position = position;
            _isFadeOutSequencePlayed = false;

            _body.gameObject.SetActive(false);
            _body.transform.localScale = Vector3.one * _objectRadius / 4f;

            _body.GetComponent<MeshRenderer>().sortingOrder = (int)(this.transform.position.y * -100.0f);
            _isCanSlow = false;
            _canSlowAt = now + indicatorDuration;

            stage.AreaIndicators.CreateBlinkCircularAttackRangeIndicator(position, objectRadius, indicatorDuration);
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _slowedCharacters.Clear();

            _fadeInSequence.Pause();
            _fadeOutSequence.Pause();
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;

            if (_canSlowAt <= now && !_isCanSlow)
            {
                _body.gameObject.SetActive(true);
                _fadeInSequence.Restart();
                _isCanSlow = true;
            }

            this.UpdateAttacked(stage, now);
            if (_createdAt + _lifeTime + _indicatorDuration - _fadeOutSequenceDuration < now && !_isFadeOutSequencePlayed)
            {
                _fadeOutSequence.Restart();
                _isFadeOutSequencePlayed = true;
            }
        }

        private void UpdateAttacked(Stage stage, float now)
        {
            if(!_isCanSlow)
            {
                return;
            }

            if (_tickSlowClearNextAt <= now)
            {
                _slowedCharacters.Clear();
                _tickSlowClearNextAt = now + _tickSlowPeriod;
            }

            CircularTargetArea targetArea = new CircularTargetArea(this.transform.position, _objectRadius);
            List<Character> findedCharacters = new List<Character>();
            stage.FindAliveCharactersInArea(_allianceType.ToEnemyAlliance(), targetArea, findedCharacters);

            foreach (Character character in findedCharacters)
            {
                if(!_slowedCharacters.Contains(character))
                {
                    character.StatusEffects.AddOrUpdateStatusEffect(stage, character, Characters.StatusEffects.StatusEffectType.SlowMove, "AmbientSlow", _slowDuration, now, _slowRate);
                    _slowedCharacters.Add(character);
                }
            }
        }
    }

}

