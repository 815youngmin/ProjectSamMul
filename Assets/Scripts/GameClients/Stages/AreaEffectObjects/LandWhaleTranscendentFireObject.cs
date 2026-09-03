using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class LandWhaleTranscendentFireObject : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + _lifeTime;

        private HashSet<Character> _hittedCharacters;

        private Character _owner;
        private float _objectRadius;
        private float _damage;
        private float _lifeTime;
        private float _createdAt;
        private readonly float _tickDamagePeriod = 0.25f;
        private float _tickDamageClearNextAt;

        private Sequence _fadeInSequence;
        private Sequence _fadeOutSequence;
        private readonly float _fadeOutSequenceDuration = 0.5f;

        private GameObject _body;
        private SpriteAnimationHandler _spriteAnimationHandler;

        private bool _isFadeOutSequencePlayed;



        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.LandWhaleTranscendentFireObject);

            _body = ResourcePool.Instance.InstantiateFromResource("Stages/SkillEffects/LandWhale/Blue_Fire.prefab");
            _spriteAnimationHandler = _body.GetComponentInChildren<SpriteAnimationHandler>();
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector3.zero;
            _body.transform.localScale = Vector3.one;

            _body.gameObject.SetActive(false);
            _hittedCharacters = new HashSet<Character>();
            _spriteAnimationHandler.InitializeOnly();

            _fadeInSequence = DOTween.Sequence();
            _fadeInSequence.Append(_spriteAnimationHandler.SpriteRenderer.DOFade(1f, 0.3f).From(0f));
            _fadeInSequence.SetRecyclable(true);
            _fadeInSequence.SetAutoKill(false);
            _fadeInSequence.Pause();

            _fadeOutSequence = DOTween.Sequence();
            _fadeOutSequence.Append(_spriteAnimationHandler.SpriteRenderer.DOFade(0f, 0.3f).From(1f));
            _fadeOutSequence.SetRecyclable(true);
            _fadeOutSequence.SetAutoKill(false);
            _fadeOutSequence.Pause();
            


        }


        public void Initialize(
            Character owner,
            float damage,
            float lifeTime,
            float objectRadius,
            Vector2 position
            )
        {
            base.InitializeAreaObject(owner.Alliance);

            float now = Time.time;

            _createdAt = now;
            _owner = owner;
            _damage = damage;
            _objectRadius = objectRadius;

            _tickDamageClearNextAt = now;
            _lifeTime = lifeTime;
            this.transform.position = position;
            _isFadeOutSequencePlayed = false;

            _body.gameObject.SetActive(true);
            _spriteAnimationHandler.SpriteRenderer.sortingOrder = (int)(this.transform.position.y * -100.0f);
            _body.transform.localScale = Vector3.one * objectRadius / 1.6f;

            _fadeInSequence.Restart();
        }


        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;
            this.UpdateAttacked(stage, now);
            if (_createdAt + _lifeTime + - _fadeOutSequenceDuration < now && !_isFadeOutSequencePlayed)
            {
                _fadeOutSequence.Restart();
                _isFadeOutSequencePlayed = true;
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

        private void UpdateAttacked(Stage stage, float now)
        {

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
