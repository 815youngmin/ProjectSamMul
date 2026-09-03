using System.Collections.Generic;
using SamMul.GameClients.Stages.Characters.Actions.Boss.Alibaba;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.BossAIStrategies.Arabian
{
    public class HarunAlRashidIdleAIStrategy : BossIdleAIStrategy
    {
        protected override BossCombatAIStrategy CombatAIStrategy => new HarunAlRashidCombatAIStrategy();
        protected override float WaitingTime => 2.0f;
    }

    public class HarunAlRashidCombatAIStrategy : BossCombatAIStrategy
    {
        private static readonly List<Action> ACTIONS = new List<Action>()
        {
            Monster.Do<HarunAlRashidBoomerangAction>,
            Monster.Do<HarunAlRashidSummonAction>
        };

        protected override BossIdleAIStrategy IdleAIStategy => new HarunAlRashidIdleAIStrategy();
        protected override List<Action> Actions => ACTIONS;
        protected override float ActionCoolTime => 2f;
        protected override MovementType MovementType => MovementType.MoveToTarget;
    }
}

