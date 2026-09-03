using System.Collections.Generic;
using SamMul.GameClients.Stages.Characters.Actions.Boss.Western;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.BossAIStrategies.Western
{
    public class WesternWyattEarpIdleAIStrategy : BossIdleAIStrategy
    {
        protected override BossCombatAIStrategy CombatAIStrategy => new WesternWyattEarpCombatAIStrategy();
        protected override float WaitingTime => 2.0f;
    }

    public class WesternWyattEarpCombatAIStrategy : BossCombatAIStrategy
    {
        private static readonly List<Action> ACTIONS = new List<Action>()
        {
            Monster.Do<WyattEarpDashReflectionObjectAction>,
            Monster.Do<WyattEarpHomingObjectAction>,
            Monster.Do<WyattEarpCircleSplitObjectAction>,
        };

        protected override BossIdleAIStrategy IdleAIStategy => new WesternWyattEarpIdleAIStrategy();
        protected override List<Action> Actions => ACTIONS;
        protected override float ActionCoolTime => 1.7f;
        protected override MovementType MovementType => MovementType.MoveToTarget;
    }
}

