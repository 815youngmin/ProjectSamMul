using System.Collections.Generic;
using Z.GameClients.Stages.Characters.Actions.Boss.Western;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.BossAIStrategies.Western
{
    public class WesternMiningKingIdleAIStrategy : BossIdleAIStrategy
    {
        protected override BossCombatAIStrategy CombatAIStrategy => new WesternMiningKingCombatAIStrategy();
        protected override float WaitingTime => 2.0f;
    }

    public class WesternMiningKingCombatAIStrategy : BossCombatAIStrategy
    {
        private static readonly List<Action> ACTIONS = new List<Action>()
        {
            Monster.Do<MiningKingSummonAction>,
            Monster.Do<MiningKingMultiDashAction>,
            Monster.Do<MiningKingMultiReflectionObjectAction>,
            Monster.Do<MiningKingHomingObjectAction>
        };

        protected override BossIdleAIStrategy IdleAIStategy => new WesternMiningKingIdleAIStrategy();
        protected override List<Action> Actions => ACTIONS;
        protected override float ActionCoolTime => 2f;
        protected override MovementType MovementType => MovementType.MoveToTarget;
    }
}

