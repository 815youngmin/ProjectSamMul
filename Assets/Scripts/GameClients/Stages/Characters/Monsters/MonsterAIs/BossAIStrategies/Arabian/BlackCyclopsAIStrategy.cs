using System.Collections.Generic;
using Z.GameClients.Stages.Characters.Actions.Boss.Alibaba;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.BossAIStrategies.Arabian
{
    public class BlackCyclopsIdleAIStrategy : BossIdleAIStrategy
    {
        protected override BossCombatAIStrategy CombatAIStrategy => new BlackCyclopsCombatAIStrategy();
        protected override float WaitingTime => 2.0f;
    }

    public class BlackCyclopsCombatAIStrategy : BossCombatAIStrategy
    {
        private static readonly List<Action> ACTIONS = new List<Action>()
        {
            Monster.Do<BlackCyclopsCircleProjectileAction>,
            Monster.Do<BlackCyclopsDashAction>,
            Monster.Do<BlackCyclopsSplitObjectAction>
        };

        protected override BossIdleAIStrategy IdleAIStategy => new BlackCyclopsIdleAIStrategy();
        protected override List<Action> Actions => ACTIONS;
        protected override float ActionCoolTime => 2f;
        protected override MovementType MovementType => MovementType.MoveToTarget;
    }
}

