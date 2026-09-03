using System.Collections.Generic;
using SamMul.GameClients.Stages.Characters.Actions.Boss.Steampunk;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.BossAIStrategies.Steampunk
{
    public class Steampunk_WatchManIdleAIStrategy : BossIdleAIStrategy
    {
        protected override BossCombatAIStrategy CombatAIStrategy => new Steampunk_WatchManCombatAIStrategy();
        protected override float WaitingTime => 2.0f;
    }

    public class Steampunk_WatchManCombatAIStrategy : BossCombatAIStrategy
    {
        private static readonly List<Action> ACTIONS = new List<Action>()
        {
            Monster.Do<WatchManDashReflectionObjectAction>,
            Monster.Do<WatchManSectorProjectileAction>,
            Monster.Do<WatchManSummonAction>
        };

        protected override BossIdleAIStrategy IdleAIStategy => new Steampunk_WatchManIdleAIStrategy();
        protected override List<Action> Actions => ACTIONS;
        protected override float ActionCoolTime => 2f;
        protected override MovementType MovementType => MovementType.MoveToTarget;
    }
}

