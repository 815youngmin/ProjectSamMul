using System.Collections;
using System.Collections.Generic;
using UnityEngine; 
using SamMul.GameClients.Stages.Characters.Actions.Boss.Demo;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.BossAIStrategies.Demo
{
    public class DemoFirstBossIdleAIStrategy : BossIdleAIStrategy
    {
        protected override BossCombatAIStrategy CombatAIStrategy => new DemoFirstBossCombatAIStrategy();
        protected override float WaitingTime => 2.0f;
    }

    public class DemoFirstBossCombatAIStrategy : BossCombatAIStrategy
    {
        private static readonly List<Action> ACTIONS = new List<Action>()
        {
             Monster.DoSpriteMonster<DemoFirstBossDashAction>,
             Monster.DoSpriteMonster<DemoFirstBossSectorProjectileAction>,
        };

        protected override BossIdleAIStrategy IdleAIStategy => new DemoFirstBossIdleAIStrategy();
        protected override List<Action> Actions => ACTIONS;
        protected override float ActionCoolTime => 2f;
        protected override MovementType MovementType => MovementType.MoveToTarget;
    }
}