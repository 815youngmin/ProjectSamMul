using Shared.DataTables;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.CombatSystems;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class WoodTentacleCreateObject : AreaEffectObjectBase
    {
        public override bool IsAlive => _owner != null && !_owner.Action.IsDead;

        private Character _owner;
        private float _damage;
        private float _indicatorDuration;
        private float _createInterval;
        private float _tentacleCreateAt;
        private float _attackRadius;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.WoodTentacleCreateObject);
        }

        public void Initialize(
            Character owner,
            float damage,
            float attackRadius,
            float indicatorDuration,
            float createInterval
            )
        {
            base.InitializeAreaObject(owner.Alliance);
            float now = Time.time;
            _owner = owner;
            _damage = damage;
            _attackRadius = attackRadius;
            _indicatorDuration = indicatorDuration;
            _createInterval = createInterval;

            _tentacleCreateAt = now + _createInterval;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;
            if (_tentacleCreateAt <= now)
            {
                var target = stage.FindClosestCharacter(
                allianceType: _owner.Alliance.ToEnemyAlliance(),
                position: _owner.Pos,
                limitDistance: GameConstants.AI_TARGET_SEARCH_MAX_DISTANCE,
                condition: character => !character.Action.IsDead && !character.IsImmuneToHit);
                if (target != null)
                {
                    stage.CreateWoodTentacleObject(_owner, target.Pos, _attackRadius, 0f, _indicatorDuration, _damage);
                }
                _tentacleCreateAt = now + _createInterval;
            }
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
        }

    }

}