using System.Collections.Generic;
using Z.GameClients.Stages.Characters.Actions.Boss.Western;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.BossAIStrategies.Western
{
    public class WesternHeadMinerHardIdleAIStrategy : BossIdleAIStrategy
    {
        protected override BossCombatAIStrategy CombatAIStrategy => new WesternHeadMinerHardCombatAIStrategy();
        protected override float WaitingTime => 2.0f;
    }

    public class WesternHeadMinerHardCombatAIStrategy : BossCombatAIStrategy
    {
        //패턴1은 일반AI 버전과 동일
        //패턴2는 신규 추가 패턴
        //패턴3는 가중치 값때문에 따로 작업
        private static readonly List<Action> ACTIONS = new List<Action>()
        {
            Monster.Do<HeadMinerReflectionObjectAction>,
            Monster.Do<HeadMinerHardSectorSplitObjectAction>,
            Monster.Do<HeadMinerHardSummonAction>
        };

        protected override BossIdleAIStrategy IdleAIStategy => new WesternHeadMinerHardIdleAIStrategy();
        protected override List<Action> Actions => ACTIONS;
        protected override float ActionCoolTime => 1.5f;
        protected override MovementType MovementType => MovementType.MoveToTarget;
    }
}

