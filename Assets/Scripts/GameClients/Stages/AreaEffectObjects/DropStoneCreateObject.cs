using Shared.DataTables;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.Characters.Monsters;
using Z.GameClients.Stages.CombatSystems;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class DropStoneCreateObject : AreaEffectObjectBase
    {
        public override bool IsAlive => _owner != null && !_owner.Action.IsDead;

        private Monster _owner;
        private float _damage;
        private float _indicatorDuration;
        private float _createInterval;
        private float _dropStoneAt;
        private float _attackRadius;
        private static readonly float DropTime = 1f;


        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.DropStoneCreateObject);
        }

        public void Initialize(
            Monster owner,
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

            _dropStoneAt = now + _createInterval;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;
            if (_dropStoneAt <= now)
            {
                var target = stage.FindClosestCharacter(
                allianceType: _owner.Alliance.ToEnemyAlliance(),
                position: _owner.Pos,
                limitDistance: GameConstants.AI_TARGET_SEARCH_MAX_DISTANCE,
                condition: character => !character.Action.IsDead && !character.IsImmuneToHit);
                if (target != null)
                {
                    stage.CreateFallingRockAreaEffect(_owner, target.Pos, _indicatorDuration, DropTime, _damage, _attackRadius);
                }
                _dropStoneAt = now + _createInterval;
            }
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
        } 
    }
}
