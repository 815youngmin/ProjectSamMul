using System.Collections.Generic;
using SamMul.GameClients.Stages.Characters.Actions.Boss.Alibaba;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.BossAIStrategies.Arabian
{
    public class PeribanouIdleAIStrategy : BossIdleAIStrategy
    {
        protected override BossCombatAIStrategy CombatAIStrategy => new PeribanouCombatAIStrategy();
        protected override float WaitingTime => 2.0f;
    }

    public class PeribanouCombatAIStrategy : BossCombatAIStrategy
    {
        private static readonly List<Action> ACTIONS = new List<Action>()
        {
            Monster.Do<PeribanouAreaEffectAction>,
            Monster.Do<PeribanouHomingObjectAction>,
            Monster.Do<PeribanouSectorSplitObjectAction>
        };

        protected override BossIdleAIStrategy IdleAIStategy => new PeribanouIdleAIStrategy();
        protected override List<Action> Actions => ACTIONS;
        protected override float ActionCoolTime => 2f;
        protected override MovementType MovementType => MovementType.MoveToTarget;
    }
}

