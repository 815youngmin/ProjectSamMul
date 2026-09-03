using Shared.GameDataTypes;
using Shared.StaticDatas;
using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SamMul.GameClients.Stages.StageEvents
{
    public class MonsterMassSpawnStageEvent : MonsterSpawnStageEventBase
    {
        public MonsterMassSpawnStageEvent(StageEventStaticData stageEventStaticData) : base(stageEventStaticData)
        {
        }

        public override void Begin(Stage stage)
        {
            base.Begin(stage);
            this.SpawnMonsters(stage, _totalAmount);    // MassSpawn은 시작시점에 한번에 다 스폰한다.
        }

        protected override void SpawnMonsters(Stage stage, int amount)
        {
            var (nearRadius, maxRadius) = MonsterSpawnTools.CalculateSpawnRadius(this.TargetCameraOrthographicSize, stage.StaticData.StageFormType);
            var spawnBoundary = MonsterSpawnTools.CalculateSpawnBoundary(stage, maxRadius, maxRadius);
            var pcPosition = stage.PC?.Pos ?? spawnBoundary.center;
            var spawnPoint = stage.StaticData.SpawnLogicType switch
            {
                SpawnLogicType.FromEveryBoundary => MonsterSpawnTools.PickRandomPointFromAnnulusWithinBoundary(spawnBoundary, nearRadius, maxRadius, pcPosition),
                SpawnLogicType.FromThreeQuadrant => MonsterSpawnTools.PickRandomPointFromAnnulusWithinBoundary(spawnBoundary, nearRadius, maxRadius, pcPosition),
                SpawnLogicType.FromVerticalBoundary => MonsterSpawnTools.PickRandomPointFromVertical(spawnBoundary, nearRadius, maxRadius),
                _ => throw new NotImplementedException($"{stage.StaticData.SpawnLogicType}")
            };

            float offsetRadius;
            if (amount > 50)
            {
                offsetRadius = 3.0f;
            }
            else if (amount > 20)
            {
                offsetRadius = 2.0f;
            }
            else
            {
                offsetRadius = 1.0f;
            }

            for (int i = 0; i < amount; ++i)
            {
                var offset = offsetRadius * Random.insideUnitCircle;
                this.SpawnMonster(stage, spawnPoint + offset);
            }
        }

        public override void End(Stage stage)
        {
            base.End(stage);
            Debug.Assert(_remainingSpawnAmountForThisEvent <= 0);
        }

        public override void Update(Stage stage, int stageEventTickNumber)
        {
            base.Update(stage, stageEventTickNumber);
        }

    }
}
