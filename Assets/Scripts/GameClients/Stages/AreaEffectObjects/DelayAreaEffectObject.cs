using DG.Tweening;
using Z.Animations.Placeholder;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class DelayAreaEffectObject : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time < _dieAt;
        
        private HashSet<Character> _hittedCharacters;

        private Character _owner;
        private float _delayDuration;
        private float _attackRadius;
        private float _damage;
        private float _lifeTime;

        private float _createdAt;
        private float _fadeOutAt;
        private float _dieAt;

        private float _tickDamageNextAt;
        private string _bodyPrefabPath;

        private Sequence _fadeInSequence;
        private Sequence _fadeOutSequence;
        private static readonly float TickDamagePeriod = 0.25f;
        private static readonly float FadeOutSequenceDuration = 0.5f;
        private static readonly float FadeInSequenceDuration = 0.5f;

        private GameObject _body;
        private SkeletonAnimation _skeletonAnimation;
        private SpriteRenderer _spriteRenderer;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.DelayAreaEffectObject);
        }

        public void Initialize(
            Character owner,
            float delay,
            float damage,
            float lifeTime,
            float attackRadius,
            Vector2 position,
            string bodyPrefabPath
            )
        {
            base.InitializeAreaObject(owner.Alliance);

            float now = Time.time;

            _delayDuration = delay;
            _owner = owner;
            _damage = damage;
            _lifeTime = lifeTime;
            _attackRadius = attackRadius;

            _tickDamageNextAt = now + _delayDuration;
            _createdAt = now + _delayDuration;
            _fadeOutAt = now + _delayDuration + _lifeTime - FadeOutSequenceDuration;
            _dieAt = now + _delayDuration + _lifeTime;

            this.transform.position = position;

            _body = ResourcePool.Instance.InstantiateFromResource(bodyPrefabPath);
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector3.zero;
            _body.gameObject.SetActive(false);
            _bodyPrefabPath = bodyPrefabPath;

            _hittedCharacters = new HashSet<Character>();

            //스파인 프리팹 파일
            if (_body.GetComponentInChildren<SkeletonAnimation>() != null)
            {
                _skeletonAnimation = _body.GetComponentInChildren<SkeletonAnimation>();
                _body.GetComponent<MeshRenderer>().sortingOrder = (int)(this.transform.position.y * -100.0f);

                _fadeInSequence = DOTween.Sequence();
                _fadeInSequence.Append(DOTween.To(() => 0f, x => _skeletonAnimation.skeleton.A = x, 1f, FadeInSequenceDuration));
                _fadeInSequence.SetRecyclable(true);
                _fadeInSequence.SetAutoKill(false);
                _fadeInSequence.Pause();

                _fadeOutSequence = DOTween.Sequence();
                _fadeOutSequence.Append(DOTween.To(() => 1f, x => _skeletonAnimation.skeleton.A = x, 0f, FadeOutSequenceDuration));
                _fadeOutSequence.SetRecyclable(true);
                _fadeOutSequence.SetAutoKill(false);
                _fadeOutSequence.Pause();
            }
            //스프라이트 애니메이션 프리팹 파일
            else if (_body.GetComponentInChildren<SpriteAnimationHandler>() != null)
            {
                _spriteRenderer = _body.GetComponentInChildren<SpriteRenderer>();
                _spriteRenderer.sortingOrder = (int)(this.transform.position.y * -100.0f);

                _fadeInSequence = DOTween.Sequence();
                _fadeInSequence.Append(_spriteRenderer.DOFade(1f, FadeInSequenceDuration).From(0));
                _fadeInSequence.SetRecyclable(true);
                _fadeInSequence.SetAutoKill(false);
                _fadeInSequence.Pause();

                _fadeOutSequence = DOTween.Sequence();
                _fadeOutSequence.Append(_spriteRenderer.DOFade(0f, FadeOutSequenceDuration).From(1));
                _fadeOutSequence.SetRecyclable(true);
                _fadeOutSequence.SetAutoKill(false);
                _fadeOutSequence.Pause();

            }
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;
            if(_createdAt <= now)
            {
                _body.gameObject.SetActive(true);
                _fadeInSequence.Restart();
                _createdAt = float.MaxValue;
            }

            if (_fadeOutAt < now)
            {
                _fadeOutSequence.Restart();
                _fadeOutAt = float.MaxValue;
            }

            this.UpdateAttacked(stage, now);
        }

        private void UpdateAttacked(Stage stage, float now)
        {
            if (_tickDamageNextAt <= now)
            {
                _hittedCharacters.Clear();
                _tickDamageNextAt = now + TickDamagePeriod;
            }
 
            CircularTargetArea area = new CircularTargetArea(this.transform.position, _attackRadius);
            CombatSystem.HitOnTargetArea(stage, area, _owner, _damage, CombatSystem.KnockBackType.Pivot, Vector2.zero, 0f, _hittedCharacters, _hittedCharacters, string.Empty);
        }


        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _owner = null;
            _hittedCharacters.Clear();

            if (_fadeInSequence != null)
            {
                _fadeInSequence.Kill();
                _fadeInSequence = null;
            }

            if (_fadeOutSequence != null)
            {
                _fadeOutSequence.Kill();
                _fadeOutSequence = null;
            }

            ResourcePool.Instance.PutBackInstance(_bodyPrefabPath, _body);
            _body = null;
        }
    }

}

