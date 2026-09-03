using System.Collections.Generic;
using Z.GameClients.Stages.Characters.Actions.Boss.Steampunk;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.BossAIStrategies.Steampunk
{
    public class Steampunk_NicolasFlamelHardIdleAIStrategy : BossIdleAIStrategy
    {
        protected override BossCombatAIStrategy CombatAIStrategy => new Steampunk_NicolasFlamelHardCombatAIStrategy();
        protected override float WaitingTime => 2.0f;
    }

    public class Steampunk_NicolasFlamelHardCombatAIStrategy : BossCombatAIStrategy
    {
        private static readonly List<Action> ACTIONS = new List<Action>()
        {
            Monster.Do<NicolasFlamelDashSectorProjectileAction>,
            Monster.Do<NicolasFlamelSectorReflectionObjectAction>,
            Monster.Do<NicolasFlamelPoisonBallAction>,
            Monster.Do<NicolasFlamelSpinAreaEffectAction>
        };

        protected override BossIdleAIStrategy IdleAIStategy => new Steampunk_NicolasFlamelHardIdleAIStrategy();
        protected override List<Action> Actions => ACTIONS;
        protected override float ActionCoolTime => 1.5f;
        protected override MovementType MovementType => MovementType.MoveToTarget;
    }
}

