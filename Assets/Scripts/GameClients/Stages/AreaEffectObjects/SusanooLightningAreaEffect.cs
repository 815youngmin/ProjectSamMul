using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class SusanooLightningAreaEffect : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + _lifeTime;

        private Stage _stage;
        private Character _owner;
        private float _lightningdamage;

        private float _createdAt;
        private float _lifeTime;

        private GameObject _body;
        private readonly static string LightningPath = "Stages/AreaEffects/ZeusLightning/Zeus_Lightning.prefab";
        private SpriteAnimationHandler _animationHandler;


        private float _lightningRadius;
        private float _indicatorTime;

        private float _animationPlayAt;
        private float _indicatorAt;

        private Vector2 _projectileDirection;
        private float _projectileSpeed;
        private float _projectileRadius;
        private float _projectileAliveDistance;
        private float _projectileDamage;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.SusanooLightning);
            _body = ResourcePool.Instance.InstantiateFromResource(LightningPath);
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;
            _body.transform.localScale = Vector2.one;
            _animationHandler = _body.GetComponent<SpriteAnimationHandler>();
            _animationHandler.InitializeOnly();
        }

        public void Initialize(
            Stage stage,
            Character owner,
            float waitTime,         //대기 시간
            float indicatorTime,    //대기 시간 후 인디케이터 시간
            Vector2 lightningPosition,
            float lightningRadius,
            float lightningdamage,
            Vector2 projectileDirection,
            float projectileSpeed,
            float projectileRadius,
            float projectileAliveDistance,
            float projectileDamage
            )
        {
            base.InitializeAreaObject(owner.Alliance);
            _stage = stage;
            _owner = owner;
            this.transform.position = lightningPosition;
            _lightningRadius = lightningRadius;
            _lightningdamage = lightningdamage;
            _indicatorTime = indicatorTime;

            _projectileDirection = projectileDirection;
            _projectileSpeed = projectileSpeed;
            _projectileRadius = projectileRadius;
            _projectileAliveDistance = projectileAliveDistance;
            _projectileDamage = projectileDamage;

            float now = Time.time;
            _createdAt = now;
            _lifeTime = _animationHandler.AnimationDuration + indicatorTime + waitTime;

            _body.gameObject.SetActive(false);
            _body.transform.localScale = Vector3.one * (_lightningRadius / 1.5f);

            _animationPlayAt = now + waitTime + _indicatorTime;
            _indicatorAt = now + waitTime;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;

            if(now >= _indicatorAt)
            {
                stage.AreaIndicators.CreateBlinkCircularAttackRangeIndicator(this.transform.position, _lightningRadius, _indicatorTime);
                _indicatorAt = float.MaxValue;
            }

            if (now >= _animationPlayAt)
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
            CircularTargetArea targetArea = new CircularTargetArea(this.transform.position, _lightningRadius);
            CombatSystem.HitOnTargetArea(
                _stage, targetArea, _owner, _lightningdamage,
                CombatSystem.KnockBackType.Pivot, Vector2.zero, 0f, null, null,
                hitSoundPrefabPath: string.Empty);

            _stage.CreateProjectile(
                    "Stages/Projectiles/LightningBall2Radius1_0.prefab",
                    _owner.Alliance,
                    _owner,
                    _projectileDamage,
                    knockBackPower: 0f,
                    this.transform.position,
                    _projectileDirection,
                    _projectileSpeed,
                    acceleration: 0f,
                    _projectileRadius,
                    _projectileAliveDistance,
                    hitChances: 999,
                    splitCount: 0,
                    hitSoundPrefabPath: null
                    );

        }
    }

}
