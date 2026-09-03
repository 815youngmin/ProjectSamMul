using Shared.DataTables;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.HealAIs;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.MeleeAIs
{
    public class ShieldHealIdleAIStrategy : MonsterAIStrategyBase
    {
        private HealAIBlackboard Blackboard => (HealAIBlackboard)base._blackboard;

        private float _findTargetAt;

        private readonly static float TARGET_CHECK_PERIOD = 1.0f;

        public ShieldHealIdleAIStrategy(HealAIBlackboard blackboard) : base(blackboard)
        {
        }

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

        public override void End(Stage stage, Monster owner)
        {

        }

        public override MonsterAIStrategyBase OnHitted(Stage stage, Monster owner, Character attacker)
        {
            return null;
        }
    }
}