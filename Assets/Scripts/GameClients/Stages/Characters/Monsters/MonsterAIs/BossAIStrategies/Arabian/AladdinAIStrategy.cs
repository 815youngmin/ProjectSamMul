using System.Collections.Generic;
using Z.GameClients.Stages.Characters.Actions.Boss.Alibaba;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.BossAIStrategies.Arabian
{
    public class AladdinIdleAIStrategy : BossIdleAIStrategy
    {
        protected override BossCombatAIStrategy CombatAIStrategy => new AladdinCombatAIStrategy();
        protected override float WaitingTime => 2.0f;
    }

    public class AladdinCombatAIStrategy : BossCombatAIStrategy
    {
        private static readonly List<Action> ACTIONS = new List<Action>()
        {
            Monster.Do<AladdinDashAction>,
            Monster.Do<AladdinFireAreaEffectAction>,
            Monster.Do<AladdinMultiSandAreaEffectAction>,
            Monster.Do<AladdinSummonAction>,
        };

        protected override BossIdleAIStrategy IdleAIStategy => new AladdinIdleAIStrategy();
        protected override List<Action> Actions => ACTIONS;
        protected override float ActionCoolTime => 2f;
        protected override MovementType MovementType => MovementType.MoveToTarget;
    }
}

