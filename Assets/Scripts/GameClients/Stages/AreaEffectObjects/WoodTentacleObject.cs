using Z.Animations.Placeholder;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class WoodTentacleObject : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + _lifeTime;

        private Character _owner;
        private float _damage;

        private float _createdAt;
        private float _lifeTime;
        private float _hitAt;
        private float _delay;
        private float _indicatorDuration;

        private float _indicatorAt;


        private GameObject _body;
        private SkeletonAnimation _bodySkeletonData;
        private HashSet<Character> _hittedCharacters;
        private float _attackRadius;

        private static readonly string BodyPath = "Stages/AreaEffects/Public_tentacle.prefab";
        private static readonly string AttackAnimationName = "tentacle";

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.WoodTentacleObject);
            _body = ResourcePool.Instance.InstantiateFromResource(BodyPath);
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;
            _body.transform.localScale = Vector2.one;

            _bodySkeletonData = _body.GetComponent<SkeletonAnimation>();
            _hittedCharacters = new HashSet<Character>();
        }

        public void Initialize(
            Character owner,
            Vector2 position,
            float radius,
            float delay,
            float indicatorDuration,
            float damage)
        {
            base.InitializeAreaObject(owner.Alliance);
            float now = Time.time;

            _owner = owner;
            _lifeTime = _bodySkeletonData.Skeleton.Data.FindAnimation(AttackAnimationName).Duration + indicatorDuration + _delay;
            _damage = damage;
            _attackRadius = radius;
            _delay = delay;
            _indicatorDuration = indicatorDuration;

            _body.transform.localScale = Vector3.one / 1.5f * _attackRadius;
            _bodySkeletonData.AnimationState.SetEmptyAnimation(0, 0);
            _bodySkeletonData.AnimationState.AddAnimation(0, AttackAnimationName, false, indicatorDuration + delay);

            _createdAt = now;
            _indicatorAt = _createdAt + _delay;
            _hitAt = _indicatorAt + indicatorDuration;
            this.transform.position = position;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;

            if(_indicatorAt <= now)
            {
                stage.CreateBlinkCircularAttackRangeIndicator(this.transform.position, _attackRadius, _indicatorDuration);
                _indicatorAt = float.MaxValue;
            }

            if (_hitAt <= now)
            {
                CircularTargetArea targetArea = new CircularTargetArea(this.transform.position, _attackRadius);
                CombatSystem.HitOnTargetArea(stage, targetArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, Vector2.zero, 0f, _hittedCharacters, _hittedCharacters, null);
            }
        }

        public override void PuttingBackToPool()
        {
            _hittedCharacters.Clear();
            _bodySkeletonData.AnimationState.SetEmptyAnimation(0, 0);

            base.PuttingBackToPool();
        }

    }

}
