using Shared.DataTables;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.BossAIStrategies
{
    public abstract class BossIdleAIStrategy : MonsterAIStrategyBase
    {
        protected abstract BossCombatAIStrategy CombatAIStrategy { get; }
        protected abstract float WaitingTime { get; }

        private float _activeAt;

        public override void Begin(Stage stage, Monster owner)
        {
            owner.StopMovement();
            _activeAt = Time.time + WaitingTime;
        }

        public override MonsterAIStrategyBase Update(Stage stage, Monster owner)
        {
            if (Time.time < _activeAt)
            {
                return null;
            }

            var target = stage.FindClosestCharacter(
                allianceType: owner.Alliance.ToEnemyAlliance(),
                position: owner.Pos,
                limitDistance: GameConstants.AI_TARGET_SEARCH_MAX_DISTANCE,
                condition: character => !character.Action.IsDead && !character.IsImmuneToHit);

            return target != null ? CombatAIStrategy : null;
        }

        public override void End(Stage stage, Monster owner)
        {

        }

        public override MonsterAIStrategyBase OnHitted(Stage stage, Monster owner, Character attacker)
        {
            return null;
        }
    }
}
