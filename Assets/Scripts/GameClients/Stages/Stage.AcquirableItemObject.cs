using DG.Tweening;
using Shared.GameDataTypes;
using SamMul.Animations.Placeholder;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.ItemObjects;
using SamMul.ResourcePools;
using SamMul.UnityHelpers;

namespace SamMul.GameClients.Stages
{

    public partial class Stage
    {
        private void UpdateAcquirableItemObjects(float now, float delta)
        {

        }

        private List<ExpObject> v_removedExpsForMerge = new List<ExpObject>();
        public ExpObject CreateExpObject(long expAmount, Vector2 spawnPosition)
        {
            const float sqrMergeDistance = 0.1f;

            long mergeExpAmount = 0;

            foreach (var item in _acquirableItemObjects)
            {
                if (item is not ExpObject)
                {
                    continue;
                }

                if (((Vector2)item.transform.position - spawnPosition).sqrMagnitude <= sqrMergeDistance)
                {
                    ExpObject otherExpObject = (ExpObject)item;
                    mergeExpAmount += otherExpObject.ExpAmount;
                    v_removedExpsForMerge.Add(otherExpObject);
                }
            }

            foreach (var item in v_removedExpsForMerge)
            {
                this.RemoveAcquirableItemObject(item);
            }
            v_removedExpsForMerge.Clear();

            long resultExpAmount = expAmount + mergeExpAmount + (long)PC.Stats.AdditionalExpRate.Value;

            var itemType = ExpObject.GetItemTypeByAmount(resultExpAmount);
            var expObject = _itemObjectPool.TakeOneFromPool<ExpObject>(itemType);
            expObject.gameObject.transform.SetParent(_expObjectsRoot.gameObject.transform);
            expObject.InitializeExpObject(resultExpAmount, spawnPosition);

            _acquirableItemObjects.Add(expObject);

            expObject.gameObject.SetActive(true);
            return expObject;
        }

        public HpResorativeObject CreateHpResorativeObject(Vector2 spawnPosition)
        {
            spawnPosition = ConfineToWalkableArea(spawnPosition);

            var hpObject = _itemObjectPool.TakeOneFromPool<HpResorativeObject>(DropItemType.HpResorative);
            hpObject.InitializeHpResorativeObject(spawnPosition);

            _acquirableItemObjects.Add(hpObject);

            hpObject.gameObject.SetActive(true);
            return hpObject;
        }

        public GoldObject CreateGoldObject(long amount, Vector2 spawnPosition)
        {
            spawnPosition = ConfineToWalkableArea(spawnPosition);

            Debug.Assert(amount > 0, "드롭할 골드의 양은 0보다 커야 한다.");
            var goldObject = _itemObjectPool.TakeOneFromPool<GoldObject>(DropItemType.Gold);
            goldObject.InitializeGoldObject(amount, spawnPosition);

            _acquirableItemObjects.Add(goldObject);

            goldObject.gameObject.SetActive(true);
            return goldObject;
        }

        public GemObject CreateGemObject(long amount, Vector2 spawnPosition)
        {
            spawnPosition = ConfineToWalkableArea(spawnPosition);

            Debug.Assert(amount > 0, "드롭할 보석의 양은 0보다 커야 한다.");
            var gemObject = _itemObjectPool.TakeOneFromPool<GemObject>(DropItemType.Gem);
            gemObject.InitializeGemObject(amount, spawnPosition);

            _acquirableItemObjects.Add(gemObject);

            gemObject.gameObject.SetActive(true);
            return gemObject;
        }

        public ExpMagnetObject CreateExpMagnetObject(Vector2 spawnPosition)
        {
            spawnPosition = ConfineToWalkableArea(spawnPosition);

            var expMagnetObject = _itemObjectPool.TakeOneFromPool<ExpMagnetObject>(DropItemType.ExpMagnet);
            expMagnetObject.InitializeExpMagnetObject(spawnPosition);

            _acquirableItemObjects.Add(expMagnetObject);

            expMagnetObject.gameObject.SetActive(true);
            return expMagnetObject;
        }

        public RandomEquipmentElementObject CreateRandomEquipmentElementObject(Vector2 spawnPosition)
        {
            spawnPosition = ConfineToWalkableArea(spawnPosition);

            var randomEquipmentElementObject = _itemObjectPool.TakeOneFromPool<RandomEquipmentElementObject>(DropItemType.RandomEquipmentElement);
            randomEquipmentElementObject.InitializeRandomEquipmentElementObject(spawnPosition);

            _acquirableItemObjects.Add(randomEquipmentElementObject);

            randomEquipmentElementObject.gameObject.SetActive(true);
            return randomEquipmentElementObject;
        }

        public BombObject CreateBombObject(Vector2 spawnPosition)
        {
            spawnPosition = ConfineToWalkableArea(spawnPosition);

            var bombObject = _itemObjectPool.TakeOneFromPool<BombObject>(DropItemType.Bomb);
            bombObject.InitializeBombObject(spawnPosition);

            _acquirableItemObjects.Add(bombObject);

            bombObject.gameObject.SetActive(true);
            return bombObject;
        }

        public SkillBoxObject CreateSkillBoxObject(Vector2 spawnPosition)
        {
            spawnPosition = ConfineToWalkableArea(spawnPosition);

            var skillBox = _itemObjectPool.TakeOneFromPool<SkillBoxObject>(DropItemType.SkillBox);
            skillBox.InitializeSkillBoxObject(this, spawnPosition);

            _acquirableItemObjects.Add(skillBox);

            skillBox.gameObject.SetActive(true);
            return skillBox;
        }

        public void CreateStarCoreObject(Vector2 spawnPosition)
        {
            spawnPosition = ConfineToWalkableArea(spawnPosition);

            var starCore = _itemObjectPool.TakeOneFromPool<StarCoreObject>(DropItemType.StarCore);
            starCore.InitializeStarCoreObject(spawnPosition);
            starCore.gameObject.SetActive(true);

            _acquirableItemObjects.Add(starCore);
        }

        public void RemoveAcquirableItemObject(AcquirableItemObject itemObject)
        {
            if (!_acquirableItemObjects.Remove(itemObject))
            {
                // 대부분의 경우 이미 제거되어 있다.
                // 이대로 ObjectPool로 되돌려주면 된다.
            }

            _itemObjectPool.PutBack(itemObject);
        }

        private readonly List<AcquirableItemObject> v_EnumarableTakenItems = new List<AcquirableItemObject>();
        /// <summary>
        /// <paramref name="distance"/>내에 있는 획득가능한 오브젝트를 스테이지에서 제거하고 반환해준다.
        /// 반환된 오브젝트는 스테이지의 관리책임에서 벗어남에 유의할 것.
        /// 이 함수가 리턴하고 난 뒤로는 반환된 오브젝트의 관리 책임은 호출측에 있다. 책임지고 오브젝트 풀에 반환해주어야 함.
        /// </summary>
        /// <remarks>리턴된 오브젝트들은 호출측에서 책임지고 오브젝트풀에 반환해주어야 한다.</remarks>
        /// <returns>리턴된 Enumerable은 공용 임시 객체를 사용하고 있음에 유의할 것. TakeAcquirableItemObjectsInDistance()에 대해 중첩된 호출을 해서는 안 된다. (nested call 금지). 실제 로직에서 TakeAcquirableItemObjectsInDistance를 중첩호출하는 로직이 존재할 이유도 없다.</returns>
        public IEnumerable<AcquirableItemObject> TakeAcquirableItemObjectsInDistance(Vector2 position, float distance)
        {
            float distanceSquared = distance * distance;

            v_EnumarableTakenItems.Clear();
            foreach (var itemObject in _acquirableItemObjects)
            {
                var distanceVector = (Vector2)itemObject.transform.position - position;
                if (distanceVector.sqrMagnitude <= distanceSquared)
                {
                    v_EnumarableTakenItems.Add(itemObject);
                }
            }

            foreach (var itemObject in v_EnumarableTakenItems)
            {
                _acquirableItemObjects.Remove(itemObject);
            }

            // 아 이렇게 리턴해준 것 바로 안쓰고 다른짓하면 꼬일텐데... 어카지.
            return v_EnumarableTakenItems;
        }

        private readonly List<AcquirableItemObject> v_EnumarableFoundItems = new List<AcquirableItemObject>();
        /// <summary>
        /// 스테이지에서 관리하는 획득가능한 오브젝트를 찾아 참조를 순회합니다.
        /// 참조를 순회하는 것으로, 오브젝트의 관리책임은 여전히 스테이지가 가집니다.
        /// 
        /// 오브젝트의 관리책임을 함께 가져오고 싶은 경우 <seealso cref="TakeAcquirableItemObjectsInDistance"/>를 사용할 것.
        /// </summary>
        public IEnumerable<AcquirableItemObject> ForAcquirableItemObjectsInDistance(Vector2 position, float distance)
        {
            float distanceSquared = distance * distance;

            v_EnumarableFoundItems.Clear();
            foreach (var itemObject in _acquirableItemObjects)
            {
                var distanceVector = (Vector2)itemObject.transform.position - position;
                if (distanceVector.sqrMagnitude <= distanceSquared)
                {
                    v_EnumarableFoundItems.Add(itemObject);
                }
            }

            return v_EnumarableFoundItems;
        }

        private readonly List<ExpObject> v_ExpObjects = new List<ExpObject>();
        /// <remarks>리턴된 경험치 오브젝트들은 호출측에서 책임지고 오브젝트풀에 반환해주어야 한다.</remarks>
        public IEnumerable<ExpObject> TakeAllExpObjects()
        {
            v_ExpObjects.Clear();
            foreach (var itemObject in _acquirableItemObjects)
            {
                if (itemObject.DropItemType.IsExpItem())
                {
                    v_ExpObjects.Add((ExpObject)itemObject);
                }
            }

            foreach (var expObject in v_ExpObjects)
            {
                _acquirableItemObjects.Remove(expObject);
            }

            return v_ExpObjects;
        }

        private readonly List<AcquirableItemObject> v_RandomElementObjects = new List<AcquirableItemObject>();
        /// <remarks>리턴된 오브젝트들은 호출측에서 책임지고 오브젝트풀에 반환해주어야 한다.</remarks>
        public IEnumerable<AcquirableItemObject> TakeAllRandomElementObjects()
        {
            v_RandomElementObjects.Clear();
            foreach (var itemObject in _acquirableItemObjects)
            {
                if (itemObject.DropItemType == DropItemType.RandomEquipmentElement)
                {
                    v_RandomElementObjects.Add(itemObject);
                }
            }

            foreach (var elementObject in v_RandomElementObjects)
            {
                _acquirableItemObjects.Remove(elementObject);
            }
            return v_RandomElementObjects;
        }

    }
}
