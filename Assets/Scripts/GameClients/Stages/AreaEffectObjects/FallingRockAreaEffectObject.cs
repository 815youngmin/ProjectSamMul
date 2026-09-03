using DG.Tweening;
using UnityEngine;
using Z.GameClients.Stages.Characters.Monsters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class FallingRockAreaEffectObject : SmartAreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _arrivedTimeAt + _animationHandler.AnimationDuration;
        private Monster _owner;
        private float _droptime;     
        private float _radius;
        private float _damage;
        private float _arrivedTimeAt;
        private SpriteRenderer _spriteRenderer;
        private SpriteAnimationHandler _animationHandler;
        private Sequence _dropSequence;
        private Vector3 _offset;
        private bool _isHit;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForSmartBase(AreaEffectType.FallingRock);
            _offset = new Vector3(-0.67f, 2.1f);

            var stoneObject =  ResourcePool.Instance.InstantiateFromResource("Stages/AreaEffects/FallingRock/Stone.prefab");
            _spriteRenderer = stoneObject.GetComponent<SpriteRenderer>();
            _spriteRenderer.gameObject.SetActive(false);
            stoneObject.transform.SetParent(transform);
            _spriteRenderer.transform.localPosition = _offset;

            _animationHandler = ResourcePool.Instance.InstantiateFromResource<SpriteAnimationHandler>("Stages/AreaEffects/FallingRock/StoneDrop.prefab");
            _animationHandler.gameObject.SetActive(false);
            _animationHandler.transform.SetParent(this.transform);
            _animationHandler.transform.localPosition = _offset;
            _animationHandler.InitializeOnly();
        }

        //인디케이터 있는버전
        public void Initialize(Stage stage, Monster owner, Vector2 position, float indicatorTime, float dropTime, float damage, float radius)
        {
            base.InitializeAreaObject(owner.Alliance);
            float now = Time.time;
            _owner = owner;

            this.transform.position = position;
            _arrivedTimeAt = dropTime + now + indicatorTime;
            _droptime = dropTime;
            _radius = radius;
            _damage = damage;

            _spriteRenderer.gameObject.SetActive(true);
            _spriteRenderer.transform.localScale = Vector3.one / 3f * radius;
            _animationHandler.gameObject.SetActive(false);
            _animationHandler.transform.localScale = Vector3.one / 3f * radius;
            _spriteRenderer.gameObject.transform.localPosition = new Vector3(0, 30f, 0) + _offset;
            this.AllocateDropSequenceAnimation(indicatorTime);
            _dropSequence.Restart();
            _isHit = false;

            stage.AreaIndicators.CreateBlinkCircularAttackRangeIndicator(position, radius, indicatorTime + dropTime);
        }


        //인디케이터 없는버전 (구 버전)
        public void Initialize(Monster owner, Vector2 position, float dropTime, float damage, float radius)
        {
            base.InitializeAreaObject(owner.Alliance);
            float now = Time.time;
            _owner = owner;

            this.transform.position = position;
            _arrivedTimeAt = dropTime + now ;
            _droptime = dropTime;
            _radius = radius;
            _damage = damage;

            _spriteRenderer.gameObject.SetActive(true);
            _spriteRenderer.transform.localScale = Vector3.one / 3f * radius;
            _animationHandler.gameObject.SetActive(false);
            _animationHandler.transform.localScale = Vector3.one / 3f * radius;
            _spriteRenderer.gameObject.transform.localPosition = new Vector3(0, 30f, 0) + _offset;
            this.AllocateDropSequenceAnimation(0f);
            _dropSequence.Restart();
            _isHit = false;
        }

        private void AllocateDropSequenceAnimation(float indicatorDuration)
        {
            var sequence = DOTween.Sequence();
            sequence.Append(_spriteRenderer.DOFade(0.0f, 0.0f));
            sequence.AppendInterval(indicatorDuration);
            sequence.Append(_spriteRenderer.DOFade(1.0f, _droptime));
            sequence.Join(_spriteRenderer.gameObject.transform.DOLocalMove(Vector3.zero + _offset, _droptime).SetEase(Ease.InQuart));
            sequence.SetRecyclable(true);
            sequence.SetAutoKill(false);
            sequence.Pause();

            _dropSequence = sequence;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            base.UpdateLogic(stage, deltaTime);

            float now = Time.time;

            if (_arrivedTimeAt <= now && !_isHit)
            {
                _animationHandler.gameObject.SetActive(true);
                _animationHandler.Play();
                _spriteRenderer.gameObject.SetActive(false);

                _isHit = true;
                CircularTargetArea circular = new CircularTargetArea(this.transform.position, _radius);
                CombatSystem.HitOnTargetArea(stage, circular, _owner, _damage, CombatSystem.KnockBackType.Pivot, this.transform.position, 0.0f, null, null, string.Empty);
            }
        }


        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();

            _animationHandler.gameObject.SetActive(false);
            _spriteRenderer.gameObject.SetActive(true);
        }

    }
}
