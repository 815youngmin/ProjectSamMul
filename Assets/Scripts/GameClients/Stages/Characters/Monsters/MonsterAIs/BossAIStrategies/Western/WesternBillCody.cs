using System.Collections.Generic;
using SamMul.GameClients.Stages.Characters.Actions.Boss.Western;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.BossAIStrategies.Western
{
    public class WesternBillCodyIdleAIStrategy : BossIdleAIStrategy
    {
        protected override BossCombatAIStrategy CombatAIStrategy => new WesternBillCodyCombatAIStrategy();
        protected override float WaitingTime => 2.0f;
    }

    public class WesternBillCodyCombatAIStrategy : BossCombatAIStrategy
    {
        private static readonly List<Action> ACTIONS = new List<Action>()
        {
            Monster.Do<BillCodyDashProjectileAction>,
            Monster.Do<BillCodySectorMultipleProjectileAction>,
            Monster.Do<BillCodySectorRandomProjectileAction>
        };

        protected override BossIdleAIStrategy IdleAIStategy => new WesternBillCodyIdleAIStrategy();
        protected override List<Action> Actions => ACTIONS;
        protected override float ActionCoolTime => 2f;
        protected override MovementType MovementType => MovementType.MoveToTarget;
    }
}

