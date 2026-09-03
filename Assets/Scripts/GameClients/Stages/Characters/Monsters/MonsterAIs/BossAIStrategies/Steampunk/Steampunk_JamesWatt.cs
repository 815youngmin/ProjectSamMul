using System.Collections.Generic;
using SamMul.GameClients.Stages.Characters.Actions.Boss.Steampunk;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.BossAIStrategies.Steampunk
{
    public class Steampunk_JamesWattIdleAIStrategy : BossIdleAIStrategy
    {
        protected override BossCombatAIStrategy CombatAIStrategy => new Steampunk_JamesWattCombatAIStrategy();
        protected override float WaitingTime => 2.0f;
    }

    public class Steampunk_JamesWattCombatAIStrategy : BossCombatAIStrategy
    {
        private static readonly List<Action> ACTIONS = new List<Action>()
        {
            Monster.Do<JamesWattMultiDashAction>,
            Monster.Do<JamesWattMultipleAreaEffectAction>,
            Monster.Do<JamesWattSectorReflectionObject>
        };

        protected override BossIdleAIStrategy IdleAIStategy => new Steampunk_JamesWattIdleAIStrategy();
        protected override List<Action> Actions => ACTIONS;
        protected override float ActionCoolTime => 2f;
        protected override MovementType MovementType => MovementType.MoveToTarget;
    }
}

