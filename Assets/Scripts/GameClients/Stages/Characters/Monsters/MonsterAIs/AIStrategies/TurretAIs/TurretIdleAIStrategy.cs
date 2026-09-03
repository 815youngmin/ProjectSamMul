using Shared.DataTables;
using UnityEngine;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies
{
    public class TurretIdleAIStrategy : MonsterAIStrategyBase
    {
        private float _findTargetAt;

        private readonly static float TARGET_CHECK_PERIOD = 1.0f;

        public override void Begin(Stage stage, Monster owner)
        {
            owner.StopMovement();
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
                    return new TurretCombatAIStrategy(target);
                }
            }

            return null;
        }

        public override void End(Stage stage, Monster owner)
        {

        }

        public override MonsterAIStrategyBase OnHitted(Stage stage, Monster owner, Character attacker)
        {
            // NOTE: burn 데미지로 들어오는 경우 null이 들어옵니다.
            if (null == attacker || owner.Action.IsBeingSummoned)
            {
                return null;
            }

            // 전투 전략으로 전환 
            return new TurretCombatAIStrategy(attacker);
        }
    }
}
