using Shared.StaticDatas;
using UnityEngine;

namespace Z.GameClients.Stages.StageEvents
{
    public class SandAreaEffectSpawnStageEvent : StageEventBase
    {
        //총 스폰량
        protected int _totalAmount;
     
        // 이 스테이지 이벤트가 스폰해야할 남은 수
        protected int _remainingSpawnAmountForThisEvent;

        //매 틱당 스폰에 사용되는 값
        //정수 값 만큼 소환에 사용되며 나머지는 버리지 않는다.
        private float _leftoverSpawnAmountFromPreviousTick;
        private float _amountBySpawnPeriod;

        public SandAreaEffectSpawnStageEvent(StageEventStaticData stageEventStaticData) : base(stageEventStaticData)
        {

        }
        public override void Begin(Stage stage)
        {
            base.Begin(stage);

            _totalAmount = _stageEventStaticData.Amount;
            _remainingSpawnAmountForThisEvent = _totalAmount;

            _amountBySpawnPeriod = (float)_totalAmount / StageEventTickMaxNumber;

        }
        public override void Update(Stage stage, int stageEventTickNumber)
        {
            if (StageEventTickMaxNumber < stageEventTickNumber)
            {
                return;
            }

            _leftoverSpawnAmountFromPreviousTick += _amountBySpawnPeriod;
            int currentTickSpawnAmount = (int)_leftoverSpawnAmountFromPreviousTick;
            _leftoverSpawnAmountFromPreviousTick -= (float)currentTickSpawnAmount;
            this.SpawnSandAreaEffect(stage, currentTickSpawnAmount);
        }

        public override void End(Stage stage)
        {
            this.SpawnSandAreaEffect(stage, _remainingSpawnAmountForThisEvent);
        }

        private void SpawnSandAreaEffect(Stage stage, int amount)
        {
            if (stage.PC == null)
            {
                Debug.LogError("PC가 존재하지 않습니다. 로직이 잘못되었습니다. 확인이 필요합니다.");
                return;
            }

            float randomRadius = 15f;
            float objectRadius = 5f;
            float attackDuration = 3f;

            for (int i = 0; i < amount; i++)
            {
                if (_remainingSpawnAmountForThisEvent <= 0)
                {
                    return;
                }
                _remainingSpawnAmountForThisEvent--;

                stage.CreateStageSandAreaEffectObject(
                    CombatSystems.AllianceType.Monsters, 
                    indicatorDuration: 2f, 
                    stage.PC.Pos + Random.insideUnitCircle * randomRadius,
                    objectRadius,
                    attackDuration
                    );
            }
        }


    }
}
