using Shared.DataTables;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class IntiTentacleCreateObject : AreaEffectObjectBase
    {
        public override bool IsAlive => _owner != null && !_owner.Action.IsDead;

        private Character _owner;
        private float _damage;
        private float _indicatorDuration;
        private float _createInterval;
        private float _intiTentacleCreateAt;
        private float _attackRadius;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.IntiTentacleCreateObject);
        }

        public void Initialize(
            Character owner,
            float damage,
            float attackRadius,
            float indicatorDuration,
            float createInterval)
        {
            base.InitializeAreaObject(owner.Alliance);
            float now = Time.time;
            _owner = owner;
            _damage = damage;
            _attackRadius = attackRadius;
            _indicatorDuration = indicatorDuration;
            _createInterval = createInterval;

            _intiTentacleCreateAt = now + _createInterval;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;
            if(_intiTentacleCreateAt <= now)
            {
                var target = stage.FindClosestCharacter(
                    allianceType: _owner.Alliance.ToEnemyAlliance(),
                    position: _owner.Pos,
                    limitDistance: GameConstants.AI_TARGET_SEARCH_MAX_DISTANCE,
                    condition: character => !character.Action.IsDead && !character.IsImmuneToHit);

                if( target != null )
                {
                    stage.CreateIntiTentacleObject(_owner, target.Pos, _attackRadius, _indicatorDuration, _damage);
                }
                _intiTentacleCreateAt = now + _createInterval;
            }
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
        }

    }

}
