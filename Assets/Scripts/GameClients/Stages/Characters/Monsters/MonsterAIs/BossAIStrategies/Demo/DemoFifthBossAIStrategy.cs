using System.Collections.Generic;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.BossAIStrategies.Demo
{
    public class DemoFifthBossIdleAIStrategy : BossIdleAIStrategy
    {
        protected override BossCombatAIStrategy CombatAIStrategy => new DemoFifthBossCombatAIStrategy();
        protected override float WaitingTime => 2.0f;
    }

    public class DemoFifthBossCombatAIStrategy : BossCombatAIStrategy
    {
        // 액션을 만들면 여기에 Monster.DoSpriteMonster<액션>을 추가한다. 비어 있는 동안은 타겟을 따라 이동만 한다.
        private static readonly List<Action> ACTIONS = new List<Action>()
        {
        };

        protected override BossIdleAIStrategy IdleAIStategy => new DemoFifthBossIdleAIStrategy();
        protected override List<Action> Actions => ACTIONS;
        protected override float ActionCoolTime => 2f;
        protected override MovementType MovementType => MovementType.MoveToTarget;
    }
}
