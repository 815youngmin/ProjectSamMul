using System.Collections.Generic;
using SamMul.GameClients.Stages.Characters.Actions.Boss.Demo;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.BossAIStrategies.Demo
{
    public class DemoFourthBossIdleAIStrategy : BossIdleAIStrategy
    {
        protected override BossCombatAIStrategy CombatAIStrategy => new DemoFourthBossCombatAIStrategy();
        protected override float WaitingTime => 2.0f;
    }

    public class DemoFourthBossCombatAIStrategy : BossCombatAIStrategy
    {
        // 액션을 만들면 여기에 Monster.DoSpriteMonster<액션>을 추가한다. 비어 있는 동안은 타겟을 따라 이동만 한다.
        private static readonly List<Action> ACTIONS = new List<Action>()
        {
            Monster.DoSpriteMonster<DemoFourthBossDashCircleProjectileAction>,
            Monster.DoSpriteMonster<DemoFourthBossSummonAction>,
        };

        protected override BossIdleAIStrategy IdleAIStategy => new DemoFourthBossIdleAIStrategy();
        protected override List<Action> Actions => ACTIONS;
        protected override float ActionCoolTime => 2f;
        protected override MovementType MovementType => MovementType.MoveToTarget;
    }
}
