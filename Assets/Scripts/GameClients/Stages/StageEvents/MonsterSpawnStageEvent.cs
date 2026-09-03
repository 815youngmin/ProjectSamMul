using Shared.GameDataTypes;
using Shared.StaticDatas;
using System;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.PCs;

namespace SamMul.GameClients.Stages.StageEvents
{
    public class MonsterSpawnStageEvent : MonsterSpawnStageEventBase
    {
        //매 틱당 소환에 사용되는 값
        //정수 값 만큼 소환에 사용되며 나머지는 버리지 않는다.
        private float _leftoverSpawnAmountFromPreviousTick;
        private float _amountBySpawnPeriod;

        public MonsterSpawnStageEvent(StageEventStaticData stageEventStaticData) : base(stageEventStaticData)
        {

        }

        public override void Begin(Stage stage)
        {
            base.Begin(stage);
            _amountBySpawnPeriod = (float)_totalAmount / StageEventTickMaxNumber;
        }

        public override void End(Stage stage)
        {
            base.End(stage);
            this.SpawnMonsters(stage, _remainingSpawnAmountForThisEvent);
        }

        public override void Update(Stage stage, int stageEventTickNumber)
        {
            if (StageEventTickMaxNumber < stageEventTickNumber)
            {
                return;
            }

            base.Update(stage, stageEventTickNumber);
            _leftoverSpawnAmountFromPreviousTick += _amountBySpawnPeriod;
            int currentTickSpawnAmount = (int)_leftoverSpawnAmountFromPreviousTick;
            _leftoverSpawnAmountFromPreviousTick -= (float)currentTickSpawnAmount;
            this.SpawnMonsters(stage, currentTickSpawnAmount);
        }

        protected override void SpawnMonsters(Stage stage, int amount)
        {
            var (nearRadius, maxRadius) = MonsterSpawnTools.CalculateSpawnRadius(this.TargetCameraOrthographicSize, stage.StaticData.StageFormType);
            var spawnBoundary = MonsterSpawnTools.CalculateSpawnBoundary(stage, maxRadius, maxRadius);
            var pcPosition = stage.PC?.Pos ?? spawnBoundary.center;

            for (int i = 0; i < amount; ++i)
            {
                var spawnPoint = stage.StaticData.SpawnLogicType switch
                {
                    SpawnLogicType.FromEveryBoundary => MonsterSpawnTools.PickRandomPointFromAnnulusWithinBoundary(spawnBoundary, nearRadius, maxRadius, pcPosition),
                    SpawnLogicType.FromThreeQuadrant => MonsterSpawnTools.PickRandomPointFromAnnulusWithinBoundary(spawnBoundary, nearRadius, maxRadius, pcPosition),
                    SpawnLogicType.FromVerticalBoundary => MonsterSpawnTools.PickRandomPointFromVertical(spawnBoundary, nearRadius, maxRadius),
                    _ => throw new NotImplementedException($"{stage.StaticData.SpawnLogicType}")
                };
                this.SpawnMonster(stage, spawnPoint);
            }
        }
    }
}
