using System.Collections.Generic;
using SamMul.GameClients.Stages.Characters.Actions.Boss.Western;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.BossAIStrategies.Western
{
    public class WesternJesseJamesIdleAIStrategy : BossIdleAIStrategy
    {
        protected override BossCombatAIStrategy CombatAIStrategy => new WesternJesseJamesCombatAIStrategy();
        protected override float WaitingTime => 2.0f;
    }

    public class WesternJesseJamesCombatAIStrategy : BossCombatAIStrategy
    {
        private static readonly List<Action> ACTIONS = new List<Action>()
        {
            Monster.Do<JesseJamesReflectionHomingBoomerangAction>,
            Monster.Do<JesseJamesDashAction>,
            Monster.Do<JesseJamesMultipleBoomerangAction>
        };

        protected override BossIdleAIStrategy IdleAIStategy => new WesternJesseJamesIdleAIStrategy();
        protected override List<Action> Actions => ACTIONS;
        protected override float ActionCoolTime => 4f;
        protected override MovementType MovementType => MovementType.MoveToTarget;
    }
}

