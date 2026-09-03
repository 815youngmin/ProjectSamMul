using Shared.GameDataTypes;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Z.GameClients.Stages.StageEvents
{
    public static class MonsterSpawnTools
    {   
        public static readonly float SPAWN_RADIUS_RATE = 1.072f;
        public static readonly float SPAWN_OUTLINE_RATE = 0.185f;

        public static readonly float MAX_SPAWN_RADIUS_INNER = 17f;
        public static readonly float MAX_SPAWN_RADIUS_OUTER = 22.5f;
        
        public static (float Begin, float End) CalculateSpawnRadius(float targetCameraOrthographicSize, StageFormType stageFormType)
        {
            float startRadius = targetCameraOrthographicSize * SPAWN_RADIUS_RATE;
            float spawnDistance = targetCameraOrthographicSize * SPAWN_OUTLINE_RATE;
            float endRadius = startRadius + spawnDistance;

            // 세로맵과 닫힌맵의 경우, 너무 좁은 공간에 많은 몬스터가 스폰되어 렉이 유발된다.
            // 세로맵과 닫힌맵에는 outerRadius에 좀 더 버퍼를 두어 몬스터 스폰에 여유공간을 조금더 둔다.
            float endRadiusBuffer = (stageFormType == StageFormType.Infinite) ?
                0f : 3f;

            return (Mathf.Min(startRadius, MAX_SPAWN_RADIUS_INNER), Mathf.Min(endRadius, MAX_SPAWN_RADIUS_OUTER) + endRadiusBuffer);
        }

        public static Rect CalculateSpawnBoundary(Stage stage, float halfWidth, float halfHeight)
        {
            var walkableArea = stage.StaticData.GetWalkableArea();

            var pc = stage.PC;
            if (pc == null)
            {
                return Rect.MinMaxRect(
                    Mathf.Max(walkableArea.xMin, -halfWidth),
                    Mathf.Max(walkableArea.yMin, -halfHeight),
                    Mathf.Min(walkableArea.xMax, halfWidth),
                    Mathf.Min(walkableArea.yMax, halfHeight));
            }

            return Rect.MinMaxRect(
                Mathf.Max(walkableArea.xMin, pc.Pos.x - halfWidth),
                Mathf.Max(walkableArea.yMin, pc.Pos.y - halfHeight),
                Mathf.Min(walkableArea.xMax, pc.Pos.x + halfWidth),
                Mathf.Min(walkableArea.yMax, pc.Pos.y + halfHeight)
            );
        }
       
        public static Vector2 PickRandomPointFromVertical(Rect boundary, float beginRadius, float endRadius)
        {
            bool isTop = Random.Range(0, 2) == 0;
            Vector2 spawnPos = boundary.center;

            spawnPos += new Vector2(0, isTop ? 1 : -1) * Random.Range(beginRadius, endRadius);
            spawnPos += new Vector2(Random.Range(boundary.min.x, boundary.max.x), 0f);

            if (!boundary.Contains(spawnPos))
            {
                if (spawnPos.y < boundary.min.y)
                {
                    spawnPos.y = boundary.min.y;
                }

                if (boundary.max.y < spawnPos.y)
                {
                    spawnPos.y = boundary.max.y;
                }

                if (spawnPos.x < boundary.min.x)
                {
                    spawnPos.x = boundary.min.x;
                }

                if (boundary.max.x < spawnPos.x)
                {
                    spawnPos.x = boundary.max.x;
                }
            }
            return spawnPos;
        }

        /// <summary>
        /// 1) 도넛 내에 위치하면서
        /// 2) Boundary 내부에 위치하면서
        /// 3) PC로부터 먼 방향의
        /// 
        /// 랜덤한 위치를 반환한다.
        /// </summary>
        public static Vector2 PickRandomPointFromAnnulusWithinBoundary(Rect boundary, float innerRadius, float outerRadius, Vector2 pcPosition)
        {
            var spawnPos = PickRandomPointFromAnnulus(pcPosition, innerRadius, outerRadius);
            
            // 여기서 플레이어 위치보다 멀리의 방향으로 찍을 방법이 필요함. 
            // 반원을 저 방향으로 
            // 내부적으로 각도를 만들고 있기 때문에, 충분히 가능함. 
            // center - player 로 벡터를 만들고,
            // 해당 백터 중심으로 90도 
            if (!boundary.Contains(spawnPos))
            {
                // Boundary와 도넛이 충돌하고 있는 경우임.
                // 플레이어로부터 Boundary.Center로 향하는 방향의 반원으로 스폰해서 Boundary내로 줄여넣는다.
                var playerDirection = (boundary.center - pcPosition).normalized;
                if (playerDirection == Vector2.zero)
                {
                    playerDirection = Vector2.left;
                }

                spawnPos = PickRandomPointFromHalfAnnulus(pcPosition, innerRadius, outerRadius, playerDirection);

                if (spawnPos.y < boundary.min.y)
                {
                    spawnPos.y = boundary.min.y;
                }

                if (boundary.max.y < spawnPos.y)
                {
                    spawnPos.y = boundary.max.y;
                }

                if (spawnPos.x < boundary.min.x)
                {
                    spawnPos.x = boundary.min.x;
                }

                if (boundary.max.x < spawnPos.x)
                {
                    spawnPos.x = boundary.max.x;
                }
            }
            return spawnPos;
        }

        public static Vector2 PickRandomPointFromAnnulus(Vector2 center, float innerRadius, float outerRadius)
        {
            // 유니티 Random.Range의 경우 float 일 경우 두번째 매개인자의 값까지 **포함** 한 값이 나오기 때문에 0 == 360로 확률이 2배가 되기때문에
            float randomAngle = Random.Range(0.0f, 359.9999999f);
            Vector2 spawnDirection = Quaternion.AngleAxis(randomAngle, Vector3.back) * Vector2.up;

            var distance = Random.Range(innerRadius, outerRadius);
            var point = center + (spawnDirection * distance);

            return point;
        }

        public static Vector2 PickRandomPointFromHalfAnnulus(Vector2 center, float innerRadius, float outerRadius, Vector2 halfDirection)
        {
            float randomAngle = Random.Range(-90f, 90f);
            Vector2 spawnDirection = Quaternion.AngleAxis(randomAngle, Vector3.back) * halfDirection;

            var distance = Random.Range(innerRadius, outerRadius);
            var point = center + (spawnDirection * distance);
            
            return point;
        }

    }
}