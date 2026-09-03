using Shared.DataTables;
using UnityEngine;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.PoisonousAreaAI
{
    public class PoisonousAreaIdleAIStrategy : PoisonousAreaBaseAIStrategy
    {
        private static readonly float TARGET_CHECK_PERIOD = 1.0f;

        private float _findTargetAt;

        public override void Begin(Stage stage, Monster owner)
        {
            owner.StopMovement();
            _findTargetAt = 0.0f;
        }

        public override MonsterAIStrategyBase Update(Stage stage, Monster owner)
        {
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
                    return new PoisonousAreaCombatAIStrategy(target);
                }
            }

            return null;
        }

        public override void End(Stage stage, Monster owner)
        {

        }

        public override MonsterAIStrategyBase OnHitted(Stage stage, Monster owner, Character attacker)
        {
            if (owner.Action.IsDead)
            {
                this.CreatePoisonousAreaEffect(stage, owner);
                return null;
            }

            if (null == attacker)
            {
                return null;
            }

            // 전투 전략으로 전환 
            return new PoisonousAreaCombatAIStrategy(attacker);
        }

        public override MonsterAIStrategyBase OnDead(Stage stage, Monster owner)
        {
            _ = base.OnDead(stage, owner);

            this.CreatePoisonousAreaEffect(stage, owner);

            return null;
        }
    }
}
