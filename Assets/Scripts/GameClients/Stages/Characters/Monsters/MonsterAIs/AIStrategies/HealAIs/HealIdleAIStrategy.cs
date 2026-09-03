using Shared.DataTables;
using UnityEngine;
using Z.GameClients.Stages.CombatSystems;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.HealAIs
{
    public class HealIdleAIStrategy : MonsterAIStrategyBase
    {
        private HealAIBlackboard Blackboard => (HealAIBlackboard)base._blackboard;

        private float _findTargetAt;

        private readonly static float TARGET_CHECK_PERIOD = 1.0f;

        public HealIdleAIStrategy(HealAIBlackboard blackboard) : base(blackboard) 
        { 
        }

        public override void Begin(Stage stage, Monster owner)
        {
            owner.StopMovement();
            _findTargetAt = 0.0f;
        }

        public override void End(Stage stage, Monster owner)
        {
        }

        public override MonsterAIStrategyBase OnHitted(Stage stage, Monster owner, Character attacker)
        {
            return null;
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
                    owner.Alliance.ToEnemyAlliance(),
                    owner.Pos,
                    limitDistance: GameConstants.AI_TARGET_SEARCH_MAX_DISTANCE,
                    condition: character => !character.Action.IsDead);

                if (IsEnemyInSearchDistance(owner, target))
                {
                    return new HealCombatAIStrategy(target, Blackboard);
                }
            }

            return null;
        }
    }

}
