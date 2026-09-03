using System.Collections.Generic;
using Z.GameClients.Stages.Characters.Actions.Boss.Western;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.BossAIStrategies.Western
{
    public class WesternBombMasterHardIdleAIStrategy : BossIdleAIStrategy
    {
        protected override BossCombatAIStrategy CombatAIStrategy => new WesternBombMasterHardCombatAIStrategy();
        protected override float WaitingTime => 2.0f;
    }

    public class WesternBombMasterHardCombatAIStrategy : BossCombatAIStrategy
    {
        //BombMasterHard는 기존 패턴을 동일하게 사용하고 추가 패턴인
        //BombMasterHardBombCircleThrowAction 만 추가한다.
        private static readonly List<Action> ACTIONS = new List<Action>()
        {
            Monster.Do<BombMasterDashAction>,
            Monster.Do<BombMasterBombThrowAction>,
            Monster.Do<BombMasterHardBombCircleThrowAction>
        };

        protected override BossIdleAIStrategy IdleAIStategy => new WesternBombMasterHardIdleAIStrategy();
        protected override List<Action> Actions => ACTIONS;
        protected override float ActionCoolTime => 2f;
        protected override MovementType MovementType => MovementType.MoveToTarget;
    }
}

