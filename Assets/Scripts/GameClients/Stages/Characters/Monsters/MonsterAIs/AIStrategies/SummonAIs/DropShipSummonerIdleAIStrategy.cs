using Shared.DataTables;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs
{
    //LifeTime 시간만큼만 생존하고 사라진다.
    public class DropShipSummonerIdleAIStrategy : MonsterAIStrategyBase
    {
        Character _target;
        private float _findTargetCheckAt;
        private readonly static float FindTargetCheckDuration = 1.0f;
        private readonly static float LifeTime = 60f;
        private float _createAt;

        public DropShipSummonerIdleAIStrategy(float createAt)
        {
            _createAt = createAt;
        }

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
            return new DropShipSummonerCombatAIStrategy(_target, _createAt);
        }

        public override MonsterAIStrategyBase Update(Stage stage, Monster owner)
        {
            float now = Time.time;
            if (owner.Action.IsBeingSummoned)
            {
                return null;
            }

            if (_createAt + LifeTime <= now)
            {
                owner.DisappearFromStage(stage, withDeadEffect: false);
                return null;
            }

            if (_findTargetCheckAt < now)
            {
                _findTargetCheckAt = now + FindTargetCheckDuration;
                Character enemy = stage.FindClosestCharacter(
                    owner.Alliance.ToEnemyAlliance(),
                    owner.Pos,
                    limitDistance: GameConstants.AI_TARGET_SEARCH_MAX_DISTANCE,
                    condition: character => !character.Action.IsDead && !character.IsImmuneToHit);
                _target = enemy;
            }

            if (IsEnemyInSearchDistance(owner, _target))
            {
                return new DropShipSummonerCombatAIStrategy(_target, _createAt);
            }

            return null;
        }
    }

}
