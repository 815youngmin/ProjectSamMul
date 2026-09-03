using Shared.DataTables;
using UnityEngine;
using Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.HealAIs;
using Z.GameClients.Stages.CombatSystems;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.RangeAIs
{
    internal class EliteReflectionRangeIdleAIStrategy : MonsterAIStrategyBase
    { 
        Character _target;
        private float _findTargetCheckAt;
        private readonly static float _findTargetCheckDuration = 1.0f;
        private EliteReflectionRangeAttackAIBlackboard Blackboard => (EliteReflectionRangeAttackAIBlackboard)base._blackboard;

        public EliteReflectionRangeIdleAIStrategy(EliteReflectionRangeAttackAIBlackboard blackboard) : base(blackboard) { }

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
                    condition: character => !character.Action.IsDead && !character.IsImmuneToHit);
                _target = enemy;
            }

            if (IsEnemyInSearchDistance(owner, _target))
            {
                return new EliteReflectionRangeCombatAIStrategy(_target, Blackboard);
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
            return new EliteReflectionRangeCombatAIStrategy(attacker, Blackboard);
        }
    }
}
