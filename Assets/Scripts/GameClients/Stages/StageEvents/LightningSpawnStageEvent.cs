using DG.Tweening;
using Shared.StaticDatas;
using UnityEngine;

namespace Z.GameClients.Stages.StageEvents
{
    public class LightningSpawnStageEvent : StageEventBase
    {
        //총 스폰량
        protected int _totalAmount;
     
        // 이 스테이지 이벤트가 스폰해야할 남은 수
        protected int _remainingSpawnAmountForThisEvent;

        //매 틱당 스폰에 사용되는 값
        //정수 값 만큼 소환에 사용되며 나머지는 버리지 않는다.
        private float _leftoverSpawnCountFromPreviousTick;
        private float _amountBySpawnPeriod;

        public LightningSpawnStageEvent(StageEventStaticData stageEventStaticData) : base(stageEventStaticData)
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

            _leftoverSpawnCountFromPreviousTick += _amountBySpawnPeriod;
            int currentTickSpawnCount = (int)_leftoverSpawnCountFromPreviousTick;
            _leftoverSpawnCountFromPreviousTick -= (float)currentTickSpawnCount;
            this.SpawnLightning(stage, currentTickSpawnCount);
        }

        public override void End(Stage stage)
        {
            this.SpawnLightning(stage, _remainingSpawnAmountForThisEvent);
        }

        private void SpawnLightning(Stage stage, int count)
        {
            if (stage.PC == null)
            {
                Debug.LogError("PC가 존재하지 않습니다. 로직이 잘못되었습니다. 확인이 필요합니다.");
                return;
            }

            float attackDamage = _stageEventStaticData.Param1;
            float attackRadius = 1.5f;
            float stunDuration = 1.0f;
            float indicatorDuration = 2.0f;
            int spawnAmount = 6;
            float spawnRadius = 4f;

            for (int i = 0; i < count; i++)
            {
                if (_remainingSpawnAmountForThisEvent <= 0)
                {
                    return;
                }
                _remainingSpawnAmountForThisEvent--;

                for(int amount =  0; amount < spawnAmount; amount++)
                {
                    DOVirtual.DelayedCall(amount * 0.1f, () =>
                    {
                        Vector2 attackPos = stage.PC.Pos + Random.insideUnitCircle * spawnRadius;
                        stage.CreateStageLightningAreaEffectObject(
                            CombatSystems.AllianceType.Monsters,
                            indicatorDuration,
                            attackPos,
                            attackRadius,
                            attackDamage,
                            stunDuration
                            );
                    });
                }
            }
        }


    }
}
