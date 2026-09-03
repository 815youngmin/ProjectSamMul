using System.Collections.Generic;
using Z.GameClients.Stages.Characters.Actions.Boss.Alibaba;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.BossAIStrategies.Arabian
{
    public class SchaibarIdleAIStrategy : BossIdleAIStrategy
    {
        protected override BossCombatAIStrategy CombatAIStrategy => new SchaibarCombatAIStrategy();
        protected override float WaitingTime => 2.0f;
    }

    public class SchaibarCombatAIStrategy : BossCombatAIStrategy
    {
        private static readonly List<Action> ACTIONS = new List<Action>()
        {
            Monster.Do<SchaibarDashHomingObjectAction>,
            Monster.Do<SchaibarMultiAreaEffectAction>,
            Monster.Do<SchaibarReflectionObjectAction>,
            Monster.Do<SchaibarSummonAction>
        };

        protected override BossIdleAIStrategy IdleAIStategy => new SchaibarIdleAIStrategy();
        protected override List<Action> Actions => ACTIONS;
        protected override float ActionCoolTime => 2f;
        protected override MovementType MovementType => MovementType.MoveToTarget;
    }
}

