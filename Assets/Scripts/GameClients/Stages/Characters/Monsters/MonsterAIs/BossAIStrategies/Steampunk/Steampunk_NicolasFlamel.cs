using System.Collections.Generic;
using Z.GameClients.Stages.Characters.Actions.Boss.Steampunk;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.BossAIStrategies.Steampunk
{
    public class Steampunk_NicolasFlamelIdleAIStrategy : BossIdleAIStrategy
    {
        protected override BossCombatAIStrategy CombatAIStrategy => new Steampunk_NicolasFlamelCombatAIStrategy();
        protected override float WaitingTime => 2.0f;
    }

    public class Steampunk_NicolasFlamelCombatAIStrategy : BossCombatAIStrategy
    {
        private static readonly List<Action> ACTIONS = new List<Action>()
        {
            Monster.Do<NicolasFlamelDashSectorProjectileAction>,
            Monster.Do<NicolasFlamelLightningAction>,
            Monster.Do<NicolasFlamelSectorReflectionObjectAction>,
            Monster.Do<NicolasFlamelSummonAction>
        };

        protected override BossIdleAIStrategy IdleAIStategy => new Steampunk_NicolasFlamelIdleAIStrategy();
        protected override List<Action> Actions => ACTIONS;
        protected override float ActionCoolTime => 2f;
        protected override MovementType MovementType => MovementType.MoveToTarget;
    }
}

