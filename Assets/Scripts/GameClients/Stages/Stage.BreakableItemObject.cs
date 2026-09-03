using Shared.GameDataTypes;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using Z.GameClients.Stages.ItemObjects;
using Z.UnityHelpers;
using Random = UnityEngine.Random;

namespace Z.GameClients.Stages
{
    public partial class Stage
    {
        private static readonly IReadOnlyDictionary<DropItemType, int> s_ItemBoxDropItemCandidates = new Dictionary<DropItemType, int>() 
        {
            // 오차 발생을 피하게 위해 Int로 처리. 1퍼센트 :100, 100퍼센트 : 10000, 0.01퍼센트 : 1
            { DropItemType.ExpMagnet, 3000 },
            { DropItemType.Gold, 5000 },
            { DropItemType.HpResorative, 1000},
            { DropItemType.Bomb, 1000}
        };

        /// <summary>
        /// 스테이지의 활동영역 내로 제한한다. 
        /// WalkableArea를 넘어서면 그 영역의 가장자리의 위치를 리턴해준다.
        /// </summary>
        public Vector2 ConfineToWalkableArea(Vector2 position)
        {
            var walkableArea = this.StaticData.GetWalkableArea();

            return ClampPositionToRect(position, walkableArea);
        }

        /// <param name="modifyingRatio">
        /// WalkableArea에서 이 비율만큼 곱한 영역 내로 제한한다.
        /// </param>
        public Vector2 ConfineToWalkableArea(Vector2 position, float modifyingRatio)
        {
            var walkableArea = this.StaticData.GetWalkableArea();
            walkableArea.max *= modifyingRatio;
            walkableArea.min *= modifyingRatio;

            return ClampPositionToRect(position, walkableArea);
        }

        private static Vector2 ClampPositionToRect(Vector2 position, Rect rect)
        {
            float x = Mathf.Clamp(position.x, rect.xMin, rect.xMax);
            float y = Mathf.Clamp(position.y, rect.yMin, rect.yMax);
            return new Vector2(x, y);
        }

        // 아이템 박스 오브젝트를 생성한다.
        public ItemBoxObject CreateItemBoxObject(Vector2 spawnPosition)
        {
            // 0스테이지에서는 구조물 때문에 아이템이 스폰되는 위치를 80%로 제한한다. 
            // 40스테이지도 동일한 맵을 사용하기에 동일한 값을 사용한다.
            if(this.StaticData.StageNumber == 0 ||
               this.StaticData.StageNumber == 40)
            {
                spawnPosition = ConfineToWalkableArea(spawnPosition, 0.8f);
            }
            else
            {
                spawnPosition = ConfineToWalkableArea(spawnPosition);
            }

            var itemBoxObject = _itemObjectPool.TakeOneFromPool<ItemBoxObject>(DropItemType.ItemBox);
            itemBoxObject.InitializeItemBoxObject(s_ItemBoxDropItemCandidates, spawnPosition);

            _breakableItemObjects.Add(itemBoxObject);

            itemBoxObject.gameObject.SetActive(true);

            return itemBoxObject;
        }

        private FenceObject CreateFenceObject(Vector2 spawnPosition)
        {
            var fenceObject = _itemObjectPool.TakeOneFromPool<FenceObject>(DropItemType.Fence);
            fenceObject.InitializeFenceObject(spawnPosition, this);

            _breakableItemObjects.Add(fenceObject);

            fenceObject.gameObject.SetActive(true);

            return fenceObject;
        }

        public void RemoveBreakableItemObject(BreakableItemObject itemObject)
        {
            if (!_breakableItemObjects.Remove(itemObject))
            {
                // 대부분의 경우 이미 제거되어 있다.
                // 이대로 ObjectPool로 되돌려주면 된다.
            }

            _itemObjectPool.PutBack(itemObject);
        }

        private void UpdateBreakableItemObjects(float now, float delta)
        {
#if USE_SCOPED_PROFILER
            using (new ScopedProfiler("Stage.UpdateBreakableItemObjects"))
#endif
            {
                foreach (var item in _breakableItemObjects)
                {
                    item.UpdateLogic(this, now);
                }
            }
        }

        // 마지막으로 아이템박스 스폰한 시각
        private float _itemBoxSpawnedAt = 0f;
        private void CheckAndSpawnItemBoxObject(float now, float deltaTime)
        {
#if USE_SCOPED_PROFILER
            using (new ScopedProfiler("Stage.CheckAndSpawnItemBoxObject"))
#endif
            {
                if (PC == null)
                {
                    return;
                }

                if ((_itemBoxSpawnedAt + this.StaticData.ItemBoxSpawnPeriodSec) > now)
                {
                    return;
                }


                float random = Random.Range(0, 1f);
                if ((1f + AdditionalItemDropPercent) < random)
                {
                    //아이템 드랍 확률 계산 후 드랍 실패시 스폰 시간만 업데이트 하고 리턴 시킨다.
                    _itemBoxSpawnedAt = now;
                    return;
                }

                _itemBoxSpawnedAt = now;

                // 카메라 사이즈로부터 1~3M만큼 사이로 더 넓은 범위에서 드롭
                float cameraSize = GameClient.CameraController.OrthographicSize;
                float distanceFromPlayer = Random.Range(cameraSize + 1f, cameraSize + 3f);
                var direction = Random.insideUnitCircle.normalized;

                this.CreateItemBoxObject(PC.Pos + direction * distanceFromPlayer);
            }
        }

        public BreakableItemObject FindClosestBreakableItemObjectExceptFence(Vector2 position)
        {
            BreakableItemObject closestTarget = null;
            float closestDistance = float.MaxValue;

            int count = _breakableItemObjects.Count;
            for (int i = 0; i < count; i++)
            {
                var item = _breakableItemObjects[i];
                if (item.DropItemType == DropItemType.Fence)
                {
                    continue;
                }

                float distance = Vector2.SqrMagnitude((Vector2)item.transform.position - position);
                if (closestDistance > distance)
                {
                    closestDistance = distance;
                    closestTarget = item;
                }
            }

            return closestTarget;
        }

        //사이즈에 맞춰 사각형으로 된 울타리오브젝트들을 생성한다. 
        //울타리오브젝트 외에 추가로 보이지않는 두꺼운 벽을 울타리 밖에 생성해 플레이어, 몬스터가 이동하지 못하도록 한다.
        public void CreateFenceObjectSquareAreaBorder(Vector2 center, Vector2 size)
        {
            _fenceRect = new Rect(new Vector2(center.x - size.x * 0.5f, center.y - size.y * 0.5f), size);
            float offset = BreakableItemObject.ITEM_COLLIDER_RADIUS + 0.3f;
            float widthHalf = size.x * 0.5f;
            float hightHalf = size.y * 0.5f;
            int xNumber = (int)(size.x / offset) + 1;
            int yNumber = (int)(size.y / offset) + 1;

            Vector2 pos = new Vector2(center.x - widthHalf, center.y - hightHalf);

            GameObject[] outerWalls = new GameObject[4];
            
            for (int i = 0; i < outerWalls.Length; i++)
            {
                outerWalls[i] = new GameObject("OuterCollider");
                var collider = outerWalls[i].AddComponent<BoxCollider2D>();
                collider.gameObject.layer = LayerMask.NameToLayer("LowWall");
                collider.gameObject.transform.position = center;
                _fenceOuterWallColliders.Add(collider);
            }

            _fenceOuterWallColliders[0].GetComponent<BoxCollider2D>().size = new Vector2(size.x,100f);
            _fenceOuterWallColliders[1].GetComponent<BoxCollider2D>().size = new Vector2(size.x,100f);
            _fenceOuterWallColliders[2].GetComponent<BoxCollider2D>().size = new Vector2(100f, size.y);
            _fenceOuterWallColliders[3].GetComponent<BoxCollider2D>().size = new Vector2(100f, size.y);

            _fenceOuterWallColliders[0].GetComponent<BoxCollider2D>().offset = new Vector2(-size.x,0f);   //왼쪽벽
            _fenceOuterWallColliders[1].GetComponent<BoxCollider2D>().offset = new Vector2(size.x, 0f);   //오른쪽벽
            _fenceOuterWallColliders[2].GetComponent<BoxCollider2D>().offset = new Vector2(0f, -size.y);  //위벽
            _fenceOuterWallColliders[3].GetComponent<BoxCollider2D>().offset = new Vector2(0f, size.y);   //아래벽

            pos.x -= offset;
            for (int x = 0; x < xNumber; ++x)
            {
                pos.x += offset;
                FenceObject obj = this.CreateFenceObject(pos);
                _fenceObjects.Add(obj);
            }
            
            for (int y = 1; y < yNumber; ++y)
            {
                pos.y += offset;
                FenceObject obj = this.CreateFenceObject(pos);
                _fenceObjects.Add(obj);
            }

            for (int x = 1; x < xNumber; ++x)
            {
                pos.x -= offset;
                FenceObject obj = this.CreateFenceObject(pos);
                _fenceObjects.Add(obj);
            }

            for (int y = 2; y < yNumber; ++y)
            {
                pos.y -= offset;
                FenceObject obj = this.CreateFenceObject(pos);
                _fenceObjects.Add(obj);
            }
        }

        public void CreateFenceObjectCircularAreaBorder(Vector2 center, Vector2 size)
        {
            _fenceRect = new Rect(new Vector2(center.x - size.x * 0.5f, center.y - size.y * 0.5f), size);
            float fenceRadius = BreakableItemObject.ITEM_COLLIDER_RADIUS + 0.3f;

            float xRadius = size.x * 0.5f;
            float yRadius= size.y * 0.5f;

            float perimeter = Mathf.PI * (3 * (xRadius + yRadius) - Mathf.Sqrt((3 * xRadius + yRadius) * (xRadius + 3 * yRadius)));
            int numObjects = Mathf.CeilToInt(perimeter / fenceRadius);

            for (int i = 0; i < numObjects; i++)
            {
                float t = (float)i / numObjects * 2 * Mathf.PI;
                float x = center.x + xRadius * Mathf.Cos(t);
                float y = center.y + yRadius * Mathf.Sin(t);

                FenceObject obj = this.CreateFenceObject(new Vector2(x, y));
                _fenceObjects.Add(obj);
            }

            //원형형태의 울타리는 외부 보이지않는 벽을 울타리 생성후 EdgeCollider2D를 통해 구현해준다.
            GameObject fenceCollider = new GameObject("OuterCollider");
            var collider = fenceCollider.AddComponent<EdgeCollider2D>();
            collider.gameObject.layer = LayerMask.NameToLayer("LowWall");
            collider.gameObject.transform.localScale = Vector3.one;
            collider.transform.localPosition = Vector3.zero;
            collider.edgeRadius = 10.0f;
            List<Vector2> fenceColliderPoints = new List<Vector2>();
            for (int i = 0; i < _fenceObjects.Count; i++)
            {
                Vector2 fenceObjPos = _fenceObjects[i].transform.position;
                Vector2 dir = fenceObjPos - center;
                dir.Normalize();
                fenceColliderPoints.Add(fenceObjPos + dir * collider.edgeRadius);
            }
            collider.SetPoints(fenceColliderPoints);

            _fenceOuterWallColliders.Add(collider);
        }

        /// <summary>
        /// CreateFenceObjectSquareAreaBoarder로 생성된 Fence만 제거합니다.
        /// </summary>
        public void RemoveAllFence()
        {
            _fenceRect = null;

            foreach (FenceObject taget in _fenceObjects)
            {
                RemoveBreakableItemObject(taget);
            }

            //울타리에 사용된 보이지 않는벽 제거.
            //추후 문제되면 풀링 할 수 있도록 작업 진행한다.
            foreach (var target in _fenceOuterWallColliders)
            {
                GameObject.Destroy(target.gameObject);
            }

            _fenceObjects.Clear();
            _fenceOuterWallColliders.Clear();
        }

        /// <summary>
        /// 현재 필드의 모든 울타리를 순회하며 <paramref name="doAction"/>을 실행한다.
        /// 모든 울타리를 순회하지만, 어떤 순서로 처리할지 여부는 보장되지 않는다.
        /// </summary>
        /// <remarks>울타리 오브젝트 컨테이너를 순회하기 때문에, doAction내부에서 울타리를 추가하거나 삭제해서는 안 된다.</remarks>
        /// <param name="doAction">이 false를 리턴하면 이터레이션을 종료한다. true를 리턴하면 남은 순회를 이어서 지속한다.</param>
        public void ForAllFenceObjects(Func<FenceObject, bool> doAction)
        {
            foreach(var fenceObject in _fenceObjects)
            {
                if (!doAction(fenceObject))
                {
                    return;
                }
            }
        }
    }
}
