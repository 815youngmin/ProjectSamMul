using Shared.GameDataTypes;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using SamMul.ObjectPools;

namespace SamMul.GameClients.Stages.ItemObjects
{
    public class ItemObjectPool
    {
        private readonly ObjectPool<DropItemType, ItemObjectBase> _pool;

        public ItemObjectPool()
        {
            _pool = new ObjectPool<DropItemType, ItemObjectBase>(objectFactory: AllocateItemObject);
        }

        // 씬이 정리될 때 호출된다.
        // 씬을 넘어갈 때 유효하지 않은 Pooling Object들을 모두 Destroy해준다. (prefab은 남김)
        public void ClearBeforeChangingScene(Scene relatedScene)
        {
            _pool.DestroyAll(relatedScene, destroyFunction: (ItemObjectBase element) =>
            {
                GameObject.Destroy(element.gameObject);
            });
        }

        public TItemType TakeOneFromPool<TItemType>(DropItemType key) where TItemType : ItemObjectBase
        {
            var newItem = _pool.TakeOne<TItemType>(key);
            return newItem;
        }

        private ItemObjectBase AllocateItemObject(DropItemType itemType)
        {
            switch(itemType)
            {
                case DropItemType.ExpS:
                case DropItemType.ExpM:
                case DropItemType.ExpL:
                case DropItemType.ExpXL:
                    return ExpObject.Create(itemType);
                case DropItemType.ItemBox:
                    return ItemBoxObject.Create();
                case DropItemType.ExpMagnet:
                    return ExpMagnetObject.Create();
                case DropItemType.HpResorative:
                    return HpResorativeObject.Create();
                case DropItemType.Bomb:
                    return BombObject.Create();
                case DropItemType.Gold:
                    return GoldObject.Create();
                case DropItemType.Fence:
                    return FenceObject.Create();
                case DropItemType.SkillBox:
                    return SkillBoxObject.Create();
                case DropItemType.Tutorial3SkillBox:
                    return SkillBoxObject.Create();
                case DropItemType.Tutorial5SkillBox:
                    return SkillBoxObject.Create();
                case DropItemType.Gem:
                    return GemObject.Create();
                case DropItemType.RandomEquipmentElement:
                    return RandomEquipmentElementObject.Create();
                case DropItemType.StarCore:
                    return StarCoreObject.Create();
                default:
                    throw new NotImplementedException($"{itemType} 구현 안 됨.");
            }
        }

        public void PutBack(ItemObjectBase itemObject)
        {
            _pool.PutBack(itemObject);
        }

    }
}
