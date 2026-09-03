using System.Collections.Generic;
using Z.GameClients.Stages.Characters.Actions.Boss.Alibaba;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.BossAIStrategies.Arabian
{
    public class SinbadIdleAIStrategy : BossIdleAIStrategy
    {
        protected override BossCombatAIStrategy CombatAIStrategy => new SinbadCombatAIStrategy();
        protected override float WaitingTime => 2.0f;
    }

    public class SinbadCombatAIStrategy : BossCombatAIStrategy
    {
        private static readonly List<Action> ACTIONS = new List<Action>()
        {
            Monster.Do<SinbadHomingObjectAction>,
            Monster.Do<SinbadMultiPoisonPotionThrowAction>,
            Monster.Do<SinbadPoisonPotionThrowAction>,
            Monster.Do<SinbadSummonAction>,
        };

        protected override BossIdleAIStrategy IdleAIStategy => new SinbadIdleAIStrategy();
        protected override List<Action> Actions => ACTIONS;
        protected override float ActionCoolTime => 2f;
        protected override MovementType MovementType => MovementType.MoveToTarget;
    }
}

