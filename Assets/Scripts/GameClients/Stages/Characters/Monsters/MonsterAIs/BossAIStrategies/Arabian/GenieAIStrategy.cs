using System.Collections.Generic;
using Z.GameClients.Stages.Characters.Actions.Boss.Alibaba;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.BossAIStrategies.Arabian
{
    public class GenieIdleAIStrategy : BossIdleAIStrategy
    {
        protected override BossCombatAIStrategy CombatAIStrategy => new GenieCombatAIStrategy();
        protected override float WaitingTime => 2.0f;
    }

    public class GenieCombatAIStrategy : BossCombatAIStrategy
    {
        private static readonly List<Action> ACTIONS = new List<Action>()
        {
            Monster.Do<GenieDashAction>,
            Monster.Do<GenieMultiSandAreaEffectAction>,
            Monster.Do<GenieHomingSandObjectAction>,
        };

        protected override BossIdleAIStrategy IdleAIStategy => new GenieIdleAIStrategy();
        protected override List<Action> Actions => ACTIONS;
        protected override float ActionCoolTime => 2f;
        protected override MovementType MovementType => MovementType.MoveToTarget;
    }
}

