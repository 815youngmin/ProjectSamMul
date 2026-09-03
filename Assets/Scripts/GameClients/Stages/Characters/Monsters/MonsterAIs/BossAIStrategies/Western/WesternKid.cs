using System.Collections.Generic;
using Z.GameClients.Stages.Characters.Actions.Boss.Western;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.BossAIStrategies.Western
{
    public class WesternKidIdleAIStrategy : BossIdleAIStrategy
    {
        protected override BossCombatAIStrategy CombatAIStrategy => new WesternKidCombatAIStrategy();
        protected override float WaitingTime => 2.0f;
    }

    public class WesternKidCombatAIStrategy : BossCombatAIStrategy
    {
        private static readonly List<Action> ACTIONS = new List<Action>()
        {
           Monster.Do<KidEliteSummonAction>,
           Monster.Do<KidOutSideDashReflectionObjetAction>,
           Monster.Do<KidSplitReflectionObjectAction>,
           Monster.Do<KidConcentratedFireAction>
        };

        protected override BossIdleAIStrategy IdleAIStategy => new WesternKidIdleAIStrategy();
        protected override List<Action> Actions => ACTIONS;
        protected override float ActionCoolTime => 2f;
        protected override MovementType MovementType => MovementType.MoveToTarget;
    }
}

