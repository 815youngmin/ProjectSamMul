using System.Collections.Generic;
using SamMul.GameClients.Stages.Characters.Actions.Boss.Western;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.BossAIStrategies.Western
{
    public class WesternBombMasterIdleAIStrategy : BossIdleAIStrategy
    {
        protected override BossCombatAIStrategy CombatAIStrategy => new WesternBombMasterCombatAIStrategy();
        protected override float WaitingTime => 2.0f;
    }

    public class WesternBombMasterCombatAIStrategy : BossCombatAIStrategy
    {
        private static readonly List<Action> ACTIONS = new List<Action>()
        {
            Monster.Do<BombMasterDashAction>,
            Monster.Do<BombMasterBombThrowAction>
        };

        protected override BossIdleAIStrategy IdleAIStategy => new WesternBombMasterIdleAIStrategy();
        protected override List<Action> Actions => ACTIONS;
        protected override float ActionCoolTime => 2f;
        protected override MovementType MovementType => MovementType.MoveToTarget;
    }
}

