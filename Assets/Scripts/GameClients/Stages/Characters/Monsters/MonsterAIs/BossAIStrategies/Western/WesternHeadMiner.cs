using System.Collections.Generic;
using SamMul.GameClients.Stages.Characters.Actions.Boss.Western;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.BossAIStrategies.Western
{
    public class WesternHeadMinerIdleAIStrategy : BossIdleAIStrategy
    {
        protected override BossCombatAIStrategy CombatAIStrategy => new WesternHeadMinerCombatAIStrategy();
        protected override float WaitingTime => 2.0f;
    }

    public class WesternHeadMinerCombatAIStrategy : BossCombatAIStrategy
    {
        private static readonly List<Action> ACTIONS = new List<Action>()
        {
            Monster.Do<HeadMinerReflectionObjectAction>,
            Monster.Do<HeadMinerSummonAction>
        };

        protected override BossIdleAIStrategy IdleAIStategy => new WesternHeadMinerIdleAIStrategy();
        protected override List<Action> Actions => ACTIONS;
        protected override float ActionCoolTime => 2f;
        protected override MovementType MovementType => MovementType.MoveToTarget;
    }
}

