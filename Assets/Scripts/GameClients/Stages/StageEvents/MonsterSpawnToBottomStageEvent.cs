using Shared.GameDataTypes;
using Shared.StaticDatas;
using System;
using UnityEngine;

namespace SamMul.GameClients.Stages.StageEvents
{
    //플레이어 기준 위쪽에 몬스터를 한번에 소환해주는 이벤트
    public class MonsterSpawnToBottomStageEvent : MonsterSpawnStageEventBase
    {
        private static readonly float MaxSapwnWidth = 20f;

        public MonsterSpawnToBottomStageEvent(StageEventStaticData stageEventStaticData) : base(stageEventStaticData)
        {

        }

        public override void Begin(Stage stage)
        {
            base.Begin(stage);
            this.SpawnMonsters(stage, _totalAmount);
        }

        public override void End(Stage stage)
        {
            base.End(stage);
        }

        public override void Update(Stage stage, int stageEventTickNumber)
        {
            base.Update(stage, stageEventTickNumber);
        }

        protected override void SpawnMonsters(Stage stage, int amount)
        {
            var (nearRadius, maxRadius) = MonsterSpawnTools.CalculateSpawnRadius(this.TargetCameraOrthographicSize, stage.StaticData.StageFormType);
            var spawnBoundary = MonsterSpawnTools.CalculateSpawnBoundary(stage, maxRadius, maxRadius);
            var monsterColliderRadius = StaticDataRepository.Instance.Monsters.Get(_spawnMonsterType).ColliderRadius;
            var spawnPoint = stage.StaticData.SpawnLogicType switch
            {
                SpawnLogicType.FromEveryBoundary => new Vector2(spawnBoundary.center.x, spawnBoundary.center.y - spawnBoundary.height * 0.4f),
                SpawnLogicType.FromThreeQuadrant => new Vector2(spawnBoundary.center.x, spawnBoundary.center.y - spawnBoundary.height * 0.4f),
                SpawnLogicType.FromVerticalBoundary => new UnityEngine.Vector2(0, spawnBoundary.center.y - spawnBoundary.height * 0.4f),
                _ => throw new NotImplementedException($"{stage.StaticData.SpawnLogicType}")
            };
            var spawnWidth = spawnBoundary.width < MaxSapwnWidth ? spawnBoundary.width : MaxSapwnWidth;

            //한줄에 소환 가능한 최대 몬스터 개수
            int maxWithSapwnAmount = (int)(spawnWidth / (monsterColliderRadius * 2f));

            //최대치로 소환해야되는 줄 개수
            int maxSpawnCount = amount / maxWithSapwnAmount;

            //그외 나머지값
            int remainSpawnCount = amount % maxWithSapwnAmount;


            //최대치로 소환해야되는 줄 먼저 배치해준다.
            for(int yCount = 0; yCount < maxSpawnCount; yCount++)
            {
                for (int i = -(maxWithSapwnAmount - 1); i <= maxWithSapwnAmount - 1; i += 2)
                {
                    float offsetX = 0.5f * i * spawnWidth / maxWithSapwnAmount;
                    float offsetY = monsterColliderRadius * 2f * yCount;
                    this.SpawnMonster(stage, spawnPoint + new Vector2(offsetX, -offsetY));
                }
            }

            //나머지 값들을 배치해준다.
            for (int i = -(remainSpawnCount - 1); i <= remainSpawnCount - 1; i += 2)
            {
                float offsetX = 0.5f * i * spawnWidth / amount;
                float offsetY = monsterColliderRadius * 2f * maxSpawnCount;
                this.SpawnMonster(stage, spawnPoint + new Vector2(offsetX, -offsetY));
            }
        }
    }
}
