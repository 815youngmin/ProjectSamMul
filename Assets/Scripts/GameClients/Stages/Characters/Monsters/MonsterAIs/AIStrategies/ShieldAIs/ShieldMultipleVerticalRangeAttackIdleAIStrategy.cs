using Shared.DataTables;
using UnityEngine;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.ShieldAIs
{
    public class ShieldMultipleVerticalRangeAttackIdleAIStrategy : MonsterAIStrategyBase
    {
        private static readonly float TARGET_CHECK_PERIOD = 1.0f;

        private float _findTargetAt;

        public override void Begin(Stage stage, Monster owner)
        {
            owner.StopMovement();
            if(owner.Shield == null)
            {
                owner.CreateShield();
            }
            owner.Shield.IncreaseShieldAmount((int)owner.StaticData.Param3);
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
                    return new MultipleVerticalRangeAttackCombatAIStrategy(target);
                }
            }

            return null;
        }

        public override void End(Stage stage, Monster owner)
        {

        }

        public override MonsterAIStrategyBase OnHitted(Stage stage, Monster owner, Character attacker)
        {
            // 전투 전략으로 전환 
            // NOTE: burn 데미지로 들어오는 경우 null이 들어옵니다.
            if (null == attacker || owner.Action.IsBeingSummoned)
            {
                return null;
            }

            return new MultipleVerticalRangeAttackCombatAIStrategy(attacker);
        }
    }
}