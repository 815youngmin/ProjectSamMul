using System.Collections.Generic;
using SamMul.GameClients.Stages.Characters.Actions.Boss.Steampunk;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.BossAIStrategies.Steampunk
{
    public class Steampunk_ThomasEdisonIdleAIStrategy : BossIdleAIStrategy
    {
        protected override BossCombatAIStrategy CombatAIStrategy => new Steampunk_ThomasEdisonCombatAIStrategy();
        protected override float WaitingTime => 2.0f;
    }

    public class Steampunk_ThomasEdisonCombatAIStrategy : BossCombatAIStrategy
    {
        private static readonly List<Action> ACTIONS = new List<Action>()
        {
            Monster.Do<ThomasEdisonDashAction>,
            Monster.Do<ThomasEdisonRandomLightningAction>,
            Monster.Do<ThomasEdisonCircleAreaEffectAction>,
            Monster.Do<ThomasEdisonSummonAction>
        };

        protected override BossIdleAIStrategy IdleAIStategy => new Steampunk_ThomasEdisonIdleAIStrategy();
        protected override List<Action> Actions => ACTIONS;
        protected override float ActionCoolTime => 2f;
        protected override MovementType MovementType => MovementType.MoveToTarget;
    }
}

