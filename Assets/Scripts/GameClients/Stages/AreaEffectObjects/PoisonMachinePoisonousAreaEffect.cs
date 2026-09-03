using DG.Tweening;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class PoisonMachinePoisonousAreaEffect : AreaEffectObjectBase
    {
        private const string PROJECTILE_BODY_PREFAB_PATH = "Stages/AreaEffects/PoisonEffects/PoisonBall.prefab";

        public override bool IsAlive => _isAlive;

        private GameObject _body;
        private Monster _owner;
        private float _lifetime;
        private float _tickPeriod;
        private float _damagePerTick;
        private float _radius;
        private bool _isAlive;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.PoisonMachinePoisonousAreaEffect);
            _body = ResourcePool.Instance.InstantiateFromResource(PROJECTILE_BODY_PREFAB_PATH);
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;
            _body.transform.localScale = Vector2.one;
        }

        public void Initialize(
            Stage stage,
            Monster owner,
            Vector2 startPosition,
            Vector2 endPosition,
            float movingTime,
            float lifetime,
            float tickPeriod,
            float damagePerTick,
            float radius)
        {
            base.InitializeAreaObject(owner.Alliance);
            this.transform.position = startPosition;

            _owner = owner;
            _lifetime = lifetime;
            _tickPeriod = tickPeriod;
            _damagePerTick = damagePerTick;
            _radius = radius;
            _isAlive = true;

            DOTween.Sequence()
                .Append(this.transform.DOMove(endPosition, movingTime).From(startPosition).SetEase(Ease.Linear))
                .OnComplete(() =>
                {
                    stage.CreatePoisonousAreaEffect(
                        _owner,
                        this.transform.position,
                        _lifetime,
                        _tickPeriod,
                        _damagePerTick,
                        _radius);
                    _isAlive = false;
                });
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {

        }
    }
}
