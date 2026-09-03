using System.Collections.Generic;
using Z.GameClients.Stages.Characters.Actions.Boss.Steampunk;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.BossAIStrategies.Steampunk
{
    public class Steampunk_AlchemyGuildLeaderIdleAIStrategy : BossIdleAIStrategy
    {
        protected override BossCombatAIStrategy CombatAIStrategy => new Steampunk_AlchemyGuildLeaderCombatAIStrategy();
        protected override float WaitingTime => 2.0f;
    }

    public class Steampunk_AlchemyGuildLeaderCombatAIStrategy : BossCombatAIStrategy
    {
        private static readonly List<Action> ACTIONS = new List<Action>()
        {
            Monster.Do<AlchemyGuildLeaderDashAreaEffectAction>,
            Monster.Do<AlchemyGuildLeaderSectorPoisonPotionFireAction>,
            Monster.Do<AlchemyGuildLeaderSummonAction>
        };

        protected override BossIdleAIStrategy IdleAIStategy => new Steampunk_AlchemyGuildLeaderIdleAIStrategy();
        protected override List<Action> Actions => ACTIONS;
        protected override float ActionCoolTime => 2f;
        protected override MovementType MovementType => MovementType.MoveToTarget;
    }
}

