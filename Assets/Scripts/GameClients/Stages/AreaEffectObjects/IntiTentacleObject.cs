using Z.Animations.Placeholder;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class IntiTentacleObject : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + _lifeTime;

        private Character _owner;
        private float _damage;

        private float _createdAt;
        private float _lifeTime;
        private float _hitAt;

        private GameObject _body;
        private SkeletonAnimation _bodySkeletonData;
        private HashSet<Character> _hittedCharacters;

        private static readonly string BodyPath = "Stages/Characters/SpineSkeletons/Inca/Inca_BossInti/IntiTentacle.prefab";
        private static readonly string AttackAnimationName = "tentacle";
        private float _attackRadius;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.IntiTentacleObject);
            _body = ResourcePool.Instance.InstantiateFromResource(BodyPath);
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;
            _body.transform.localScale = Vector2.one;

            _bodySkeletonData = _body.GetComponent<SkeletonAnimation>();
            _hittedCharacters = new HashSet<Character>();
        }

        public void Initialize(
            Stage stage,
            Character owner,
            Vector2 position,
            float attackRadius,
            float indicatorDuration,
            float damage)
        {
            base.InitializeAreaObject(owner.Alliance);
            float now = Time.time;

            _owner = owner;
            _lifeTime = _bodySkeletonData.Skeleton.Data.FindAnimation(AttackAnimationName).Duration + indicatorDuration;
            _damage = damage;
            _attackRadius = attackRadius;

            _bodySkeletonData.AnimationState.SetEmptyAnimation(0, 0);
            _bodySkeletonData.AnimationState.AddAnimation(0, AttackAnimationName, false, indicatorDuration);

            _body.transform.localScale = Vector3.one / 1.5f * attackRadius;
            stage.AreaIndicators.CreateBlinkCircularAttackRangeIndicator(position, attackRadius, indicatorDuration);

            _createdAt = now;
            _hitAt = _createdAt + indicatorDuration;
            this.transform.position = position;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;
            if(_hitAt <= now)
            {
                CircularTargetArea targetArea = new CircularTargetArea(this.transform.position, _attackRadius);
                CombatSystem.HitOnTargetArea(stage, targetArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, Vector2.zero, 0f, _hittedCharacters, _hittedCharacters, null);
            }
        }

        public override void PuttingBackToPool()
        {
            _hittedCharacters.Clear();
            _bodySkeletonData.AnimationState.SetEmptyAnimation(0, 0);
            _body.transform.localScale = Vector3.one;

            base.PuttingBackToPool();
        }

    }

}
