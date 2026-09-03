using System.Collections.Generic;
using Z.GameClients.Stages.Characters.Actions.Boss.Alibaba;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.BossAIStrategies.Arabian
{
    public class WitchQueenIdleAIStrategy : BossIdleAIStrategy
    {
        protected override BossCombatAIStrategy CombatAIStrategy => new WitchQueenCombatAIStrategy();
        protected override float WaitingTime => 2.0f;
    }

    public class WitchQueenCombatAIStrategy : BossCombatAIStrategy
    {
        private static readonly List<Action> ACTIONS = new List<Action>()
        {
            Monster.Do<WitchQueenCircleSplitObjectAction>,
            Monster.Do<WitchQueenCrossSplitObjectAction>,
            Monster.Do<WitchQueenSectorSplitObjectAction>
        };

        protected override BossIdleAIStrategy IdleAIStategy => new WitchQueenIdleAIStrategy();
        protected override List<Action> Actions => ACTIONS;
        protected override float ActionCoolTime => 2f;
        protected override MovementType MovementType => MovementType.MoveToTarget;
    }
}

