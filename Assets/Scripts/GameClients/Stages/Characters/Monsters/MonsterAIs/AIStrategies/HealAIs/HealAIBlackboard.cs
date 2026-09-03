
namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.HealAIs
{
    public class HealAIBlackboard : MonsterAIBlackboardBase
    {
        public readonly float HealRange;            //힐 범위
        public readonly float HealPercent;          //힐 퍼센트 (타겟 몬스터 최대 체력 기준)
        public readonly float HealPeriod;           //힐 간격(쿨타임)
        public readonly float MaxProximityDistance;    //접근하는 최대 거리
        public readonly float MaxEscapeDistance;       //멀어지는 최대 거리
        public readonly float MoveTime;            //이동 시간
        public readonly float StopTime;            //멈춰있는 시간

        public HealAIBlackboard(float healRange, float healPercent, float healPeriod, float maxProximityDistance, float maxEscapeDistance, float moveTime, float stopTime)
        {
            HealRange = healRange;
            HealPercent = healPercent;
            HealPeriod = healPeriod;
            MaxProximityDistance = maxProximityDistance;
            MaxEscapeDistance = maxEscapeDistance;
            MoveTime = moveTime;
            StopTime = stopTime;
        }
    }
}
