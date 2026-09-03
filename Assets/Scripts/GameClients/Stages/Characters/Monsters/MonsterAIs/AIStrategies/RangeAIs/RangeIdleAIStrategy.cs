using Shared.DataTables;
using UnityEngine;
using Z.GameClients.Stages.CombatSystems;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.RangeAIs
{
    // 주변에 적을 탐색하고
    // 사정거리 내로 적이 들어오면 타겟으로 삼고
    // 타겟이 설정되면, 전투 전략으로 전이 
    public class RangeIdleAIStrategy : MonsterAIStrategyBase
    {
        Character _target;
        private float _findTargetCheckAt;
        private readonly static float _findTargetCheckDuration = 1.0f;

        public override void Begin(Stage stage, Monster owner)
        {
            _findTargetCheckAt = 0.0f;
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
                return new RangeCombatAIStrategy(_target);
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
            return new RangeCombatAIStrategy(attacker);
        }

    }

}