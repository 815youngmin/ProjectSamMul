using Shared.DataTables;
using UnityEngine;
using Z.GameClients.Stages.CombatSystems;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.InvisibleAIs
{
    public class InvisibleDashIdleAIStrategy : MonsterAIStrategyBase
    {
        private static readonly float TARGET_CHECK_PERIOD = 1.0f;

        private float _findTargetAt;

        public InvisibleDashIdleAIStrategy() : base()
        {

        }

        public override void Begin(Stage stage, Monster owner)
        {
            owner.StopMovement();
            _findTargetAt = 0.0f;
        }

        public override MonsterAIStrategyBase Update(Stage stage, Monster owner)
        {
            if (owner.Action.IsBeingSummoned)
            {
                return null;
            }

            float now = Time.time;
            if (_findTargetAt < now)
            {
                _findTargetAt = now + TARGET_CHECK_PERIOD;
                var target = stage.FindClosestCharacter(
                    allianceType: owner.Alliance.ToEnemyAlliance(),
                    position: owner.Pos,
                    limitDistance: GameConstants.AI_TARGET_SEARCH_MAX_DISTANCE,
                    condition: character => !character.Action.IsDead && !character.IsImmuneToHit);

                if (target != null)
                {
                    return new InvisibleDashCombatAIStrategy(target);
                }
            }

            return null;
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
