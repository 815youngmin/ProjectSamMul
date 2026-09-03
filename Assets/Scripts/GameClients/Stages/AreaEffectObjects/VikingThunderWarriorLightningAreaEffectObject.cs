using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class VikingThunderWarriorLightningAreaEffectObject : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + _lifeTime;

        private Stage _stage;
        private Character _owner;
        private float _damage;

        private float _createdAt;
        private float _lifeTime;

        private GameObject _body;
        private readonly string _prefabPath = "Stages/AreaEffects/ZeusLightning/Zeus_Lightning.prefab";
        private SpriteAnimationHandler _animationHandler;

        private float _attackRadius;
        private float _indicatorTime;

        private float _animationPlayAt;

        private static readonly string ProjectileBodyPath = "Stages/Projectiles/LightningRadius0_4.prefab";
        private static readonly float ProjectileRadius = 0.4f;
        private int   _projectileAmount;
        private float _projectileSpeed; 
        private float _projectileAcceleration;
        private float _projectileAliveDistance;
        private float _projectileKnobackPower;


        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.VikingThunderWarriorLightning);
            _body = ResourcePool.Instance.InstantiateFromResource(_prefabPath);
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
            float indicatorTime,
            int   projectileAmount,
            float projectileSpeed,
            float projectileAcceleration,
            float projectileAliveDistance, 
            float projectileKnobackPower
            )
        {
            base.InitializeAreaObject(owner.Alliance);

            _stage = stage;
            _owner = owner;
            this.transform.position = attackPosition;
            _attackRadius = attackRadius;
            _damage = damage;
            _indicatorTime = indicatorTime;

            _projectileAmount = projectileAmount;
            _projectileSpeed = projectileSpeed;
            _projectileAcceleration = projectileAcceleration;
            _projectileAliveDistance = projectileAliveDistance;
            _projectileKnobackPower = projectileKnobackPower;

            float now = Time.time;
            _createdAt = now;
            _lifeTime = _animationHandler.AnimationDuration + indicatorTime;

            _body.gameObject.SetActive(false);
            _animationPlayAt = now + _indicatorTime;
            stage.AreaIndicators.CreateBlinkCircularAttackRangeIndicator(this.transform.position, _attackRadius, indicatorTime);
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;
            if (now > _animationPlayAt)
            {
                _body.gameObject.SetActive(true);
                _animationHandler.InitializeAndPlay(Hit);
                _animationPlayAt = float.MaxValue;
            }
        }

        public override void PuttingBackToPool()
        {
            _body.gameObject.SetActive(false);
            base.PuttingBackToPool();
        }

        private void Hit()
        {
            CircularTargetArea targetArea = new CircularTargetArea(this.transform.position, _attackRadius);
            CombatSystem.HitOnTargetArea(
                _stage, targetArea, _owner, _damage,
                CombatSystem.KnockBackType.Pivot, Vector2.zero, 0f,
                null, null, hitSoundPrefabPath: string.Empty);

            for(int i = 0; i < _projectileAmount; i++)
            {
                _stage.CreateProjectile(
                   ProjectileBodyPath,
                   _owner.Alliance,
                   _owner,
                   _damage,
                   _projectileKnobackPower,
                   this.transform.position,
                   Quaternion.Euler(0, 0, 360f / _projectileAmount * i) * Vector2.up,
                   _projectileSpeed,
                   _projectileAcceleration,
                   ProjectileRadius,
                   _projectileAliveDistance,
                   hitChances: 1,
                   splitCount: 0,
                   isRemovableBySpinBladeObject: false,
                   hitSoundPrefabPath: string.Empty
                   );
            }

        }
    }
}