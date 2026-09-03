using Shared.DataTables;
using UnityEngine;
using Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.MeleeAIs;
using Z.GameClients.Stages.CombatSystems;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.SummonAIs
{
    public class SummonMeleeIdleAIStrategy : MonsterAIStrategyBase
    {
        Character _target;
        private float _findTargetCheckAt;
        private readonly static float _findTargetCheckDuration = 1.0f;



        public override void Begin(Stage stage, Monster owner)
        {
            owner.StopMovement();
            _findTargetCheckAt = 0.0f;
            
        }

        public override MonsterAIStrategyBase Update(Stage stage, Monster owner)
        {
            if(owner.Action.IsBeingSummoned)
            {
                return null;
            }

            float now = Time.time;
            if (_findTargetCheckAt < now)
            {
                _findTargetCheckAt = now + _findTargetCheckDuration;
                Character enemy = stage.FindClosestCharacter(owner.Alliance.ToEnemyAlliance(), owner.Pos, limitDistance: GameConstants.AI_TARGET_SEARCH_MAX_DISTANCE,
                    condition: character => !character.Action.IsDead);
                _target = enemy;
            }

            if (IsEnemyInSearchDistance(owner, _target))
            {
                return new MeleeCombatAIStrategy(_target);
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
            if (null == attacker)
            {
                return null;
            }

            return new MeleeCombatAIStrategy(attacker);
        }
    }

}
