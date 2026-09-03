using DG.Tweening;
using UnityEngine;
using Z.GameClients.Stages.Characters.Monsters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class ExplosiveAreaEffectObject : AreaEffectObjectBase
    {
        private readonly string ExplosionPath = "Stages/ETCEffects/missile_boom.prefab";

        public override bool IsAlive => !_isHit && Time.time <= _createdAt + _lifeTime;

        private Stage _stage;
        private Monster _owner;
        private float _damage;
        private float _attackRadius;

        private float _createdAt;
        private float _lifeTime;
        private bool _isHit;
        private float _explosionAt;

        private GameObject _bombBody;
        private GameObject _explosionBody;

        private SpriteAnimationHandler _explosionAnimationHandler;

        public void AllocateSharedResources(AreaEffectType areaEffectType, string imagePath)
        {
            base.AllocateSharedResourcesForBase(areaEffectType);
            _bombBody = ResourcePool.Instance.InstantiateFromResource(imagePath);
            _bombBody.transform.SetParent(this.transform);
            _bombBody.transform.localPosition = Vector2.zero;
            _bombBody.transform.localScale = Vector2.one;

            _explosionBody= ResourcePool.Instance.InstantiateFromResource(ExplosionPath);
            _explosionBody.transform.SetParent(this.transform);
            _explosionBody.transform.localPosition = Vector2.zero;
            _explosionBody.transform.localScale = Vector2.one;
            _explosionAnimationHandler = _explosionBody.GetComponent<SpriteAnimationHandler>();
            _explosionAnimationHandler.InitializeOnly();
        }

        public void Initialize(
            Stage stage,
            Monster owner,
            Vector2 startPosition,
            Vector2 endPosition,
            float waitTime,
            float attackRadius,
            float damage
            )
        {
            base.InitializeAreaObject(owner.Alliance);

            float now = Time.time;
            _stage = stage;
            _owner = owner;
            _createdAt = now;
            _attackRadius = attackRadius;
            _damage = damage;

            _lifeTime = waitTime + _explosionAnimationHandler.AnimationDuration;
            _explosionAt = now + waitTime;

            stage.AreaIndicators.CreateBlinkCircularAttackRangeIndicator(endPosition, attackRadius, waitTime);

            _explosionBody.transform.localScale = Vector3.one * (attackRadius / 1.2f);
            _explosionBody.SetActive(false);

            this.transform.position = startPosition;
            this.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360f));
            _bombBody.SetActive(true);
            DOTween.Sequence(this.transform.DOJump(endPosition, jumpPower: 5f, 1, 0.3f).SetEase(Ease.Linear));
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;
            if (_explosionAt <= now)
            {
                _bombBody.SetActive(false);
                _explosionBody.SetActive(true);
                _explosionAnimationHandler.InitializeAndPlay();
                _explosionAt = float.MaxValue;
                this.Hit();
            }
        }

        private void Hit()
        {
            CircularTargetArea targetArea = new CircularTargetArea(this.transform.position, _attackRadius);
            CombatSystem.HitOnTargetArea(
                _stage, targetArea, _owner, _damage,
                CombatSystem.KnockBackType.Pivot, Vector2.zero, 0f, null, null,
                hitSoundPrefabPath: string.Empty);
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _bombBody.SetActive(false);
            _bombBody.transform.localScale = Vector3.one;

            _explosionBody.SetActive(false);
            _explosionBody.transform.localScale = Vector3.one;
        }

    }

}
