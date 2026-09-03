using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class ZeusLightningAreaEffectObject : SmartAreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + _lifeTime;

        private Stage _stage;
        private Character _owner;
        private float _damage;

        private float _createdAt;
        private float _lifeTime;

        private GameObject _body;
        private readonly string _zeusLightningPath = "Stages/AreaEffects/ZeusLightning/Zeus_Lightning.prefab";
        private SpriteAnimationHandler _animationHandler;

        private float _attackRadius;
        private float _delay;
        private float _indicatorTime;

        private float _animationPlayAt;
        private float _indicatorAt;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForSmartBase(AreaEffectType.ZeusLightning);
            _body = ResourcePool.Instance.InstantiateFromResource(_zeusLightningPath);
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;
            _body.transform.localScale = Vector2.one;
            _animationHandler = _body.GetComponent<SpriteAnimationHandler>();
            _animationHandler.InitializeOnly();
        }

        public void Initialize(
            Stage stage,
            Character owner,
            Vector3 attackPosition,
            float attackRadius,
            float damage,
            float delay,
            float indicatorTime
            )
        {
            base.InitializeAreaObject(owner.Alliance);

            _stage = stage;
            _owner = owner;
            this.transform.position = attackPosition;
            _attackRadius = attackRadius;
            _damage = damage;
            _delay = delay;
            _indicatorTime = indicatorTime;

            float now = Time.time;
            _createdAt = now;
            _lifeTime = _delay + _animationHandler.AnimationDuration + indicatorTime;

            _body.gameObject.SetActive(false);
            _body.transform.localScale = Vector3.one * (_attackRadius / 1.5f);
            //
            _animationPlayAt = now + _delay + _indicatorTime;
            _indicatorAt = now + delay;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            base.UpdateLogic(stage, deltaTime);
            float now = Time.time;
 
            if(now > _indicatorAt)
            {
                stage.AreaIndicators.CreateBlinkCircularAttackRangeIndicator(this.transform.position, _attackRadius, _indicatorTime);
                _indicatorAt = float.MaxValue;
            }

            if (now > _animationPlayAt)
            {
                _body.gameObject.SetActive(true);
                _animationHandler.InitializeAndPlay(Hit);
                _animationPlayAt = float.MaxValue;
            }
        }

        public override void PuttingBackToPool()
        {
            _body.transform.localScale = Vector3.one;
            _body.gameObject.SetActive(false);
            base.PuttingBackToPool();
        }

        private void Hit()
        {
            CircularTargetArea targetArea = new CircularTargetArea(this.transform.position, _attackRadius);
            CombatSystem.HitOnTargetArea(
                _stage, targetArea, _owner, _damage,
                CombatSystem.KnockBackType.Pivot, Vector2.zero, 0f, null, null,
                hitSoundPrefabPath: string.Empty);
        }
    }
}
