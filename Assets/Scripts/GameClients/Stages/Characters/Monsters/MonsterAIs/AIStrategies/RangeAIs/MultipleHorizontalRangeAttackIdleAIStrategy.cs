using Shared.DataTables;
using UnityEngine;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies
{
    public class MultipleHorizontalRangeAttackIdleAIStrategy : MonsterAIStrategyBase
    {
        Character _target;
        private float _findTargetCheckAt;
        private readonly static float _findTargetCheckDuration = 1.0f;
        public override void Begin(Stage stage, Monster owner)
        {
            _findTargetCheckAt = 0.0f;
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
            return new MultipleHorizontalRangeAttackCombatAIStrategy(attacker);
        }

        public override MonsterAIStrategyBase Update(Stage stage, Monster owner)
        {
            if (owner.Action.IsBeingSummoned)
            {
                return null;
            }

            float now = Time.time;
            if (_findTargetCheckAt < now)
            {
                _findTargetCheckAt = now + _findTargetCheckDuration;
                Character enemy = stage.FindClosestCharacter(
                    owner.Alliance.ToEnemyAlliance(),
                    owner.Pos,
                    limitDistance: GameConstants.AI_TARGET_SEARCH_MAX_DISTANCE,
                    condition: character => !character.Action.IsDead);
                _target = enemy;
            }

            if (IsEnemyInSearchDistance(owner, _target))
            {
                return new MultipleHorizontalRangeAttackCombatAIStrategy(_target);
            }

            return null;
        }
    }

}
