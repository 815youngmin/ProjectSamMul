using System.Collections.Generic;
using Z.GameClients.Stages.Characters.Actions.Boss.Steampunk;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.BossAIStrategies.Steampunk
{
    public class Steampunk_BattleGolemIdleAIStrategy : BossIdleAIStrategy
    {
        protected override BossCombatAIStrategy CombatAIStrategy => new Steampunk_BattleGolemCombatAIStrategy();
        protected override float WaitingTime => 2.0f;
    }

    public class Steampunk_BattleGolemCombatAIStrategy : BossCombatAIStrategy
    {
        private static readonly List<Action> ACTIONS = new List<Action>()
        {
            Monster.Do<BattleGolemBoomerangAction>,
            Monster.Do<BattleGolemMultiDashAction>
        };

        protected override BossIdleAIStrategy IdleAIStategy => new Steampunk_BattleGolemIdleAIStrategy();
        protected override List<Action> Actions => ACTIONS;
        protected override float ActionCoolTime => 1.5f;
        protected override MovementType MovementType => MovementType.MoveToTarget;
    }
}

