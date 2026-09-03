using System.Collections.Generic;
using SamMul.GameClients.Stages.Characters.Actions.Boss.Steampunk;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.BossAIStrategies.Steampunk
{
    public class Steampunk_AlchemyGuildLeaderHardIdleAIStrategy : BossIdleAIStrategy
    {
        protected override BossCombatAIStrategy CombatAIStrategy => new Steampunk_AlchemyGuildLeaderHardCombatAIStrategy();
        protected override float WaitingTime => 2.0f;
    }

    public class Steampunk_AlchemyGuildLeaderHardCombatAIStrategy : BossCombatAIStrategy
    {
        private static readonly List<Action> ACTIONS = new List<Action>()
        {
            Monster.Do<AlchemyGuildLeaderDashAreaEffectAction>,
            Monster.Do<AlchemyGuildLeaderSectorPoisonPotionFireAction>,
            Monster.Do<AlchemyGuildLeaderSummonAction>,
            Monster.Do<AlchemyGuildLeaderSectorProjectileAction>,
        };

        protected override BossIdleAIStrategy IdleAIStategy => new Steampunk_AlchemyGuildLeaderHardIdleAIStrategy();
        protected override List<Action> Actions => ACTIONS;
        protected override float ActionCoolTime => 1.5f;
        protected override MovementType MovementType => MovementType.MoveToTarget;
    }
}

