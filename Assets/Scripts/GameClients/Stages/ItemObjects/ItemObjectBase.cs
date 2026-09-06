using Shared.GameDataTypes;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using SamMul.ObjectPools;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.ItemObjects
{
    // CoinObject, ExpObjet, ItemBoxObject, SkillBoxObject 
    //
    // AcquirableItemObject 타격을 안입고, 가까이가서 획득하면 뭔가 줌 : Coin/Exp/SkillBox 
    // BreakableItemObject 타격을 맞으면 깨지면서 다른 오브젝트 드롭 : ItemBoxObject
    public abstract class ItemObjectBase : MonoBehaviour, IPoolible<DropItemType>
    {
        public DropItemType DropItemType { get; private set; }

        // Body는 ItemObject GameObject의 Child GameObject에 붙인 SpriteRenderer이다.
        // Body의 DoTween 애니메이션이 transform 을 수정하는데, Shadow가 같이 움직이지 않도록 분리한다.
        public SpriteRenderer Body { get; private set; }
        // Shadow 는 ItemObject GameObject의 Child GameObject에 붙인 SpriteRenderer이다.
        // SpriteRenderer를 하나의 GameObject에 두개 이상 붙일 수 없기 떄문에, Child GameObject에 붙인다.
        public SpriteRenderer Shadow { get; private set; }

        public virtual void AllocateSharedResources(DropItemType dropItemType)
        {
            DropItemType = dropItemType;

            this.AllocateBody(dropItemType, 1.0f);

        }

        protected void InitializeBase(Vector2 spawnPosition)
        {
            this.transform.position = spawnPosition;
            this.transform.localScale = new Vector3(1f, 1f, 1f);

            this.gameObject.SetActive(true);
        }

        Scene IPoolible<DropItemType>.RelatedScene => this.gameObject.scene;
        DropItemType IPoolible<DropItemType>.PoolKey => DropItemType;
        public virtual void PuttingBackToPool()
        {
            // Body 스프라이트는 돌려쓴다. 초기화 안함.
            this.gameObject.SetActive(false);
        }

        private void AllocateBody(DropItemType dropItemType, float scale)
        {
            var bodyPrefabPath = GetItemObjectBodyPrefabPath(dropItemType);
            var bodyObject = ResourcePool.Instance.InstantiateFromResource(bodyPrefabPath);
            bodyObject.transform.SetParent(this.gameObject.transform, worldPositionStays: false);
            bodyObject.layer = this.gameObject.layer;
            bodyObject.transform.localScale = new Vector3(scale, scale, scale);

            this.Body = bodyObject.GetComponent<SpriteRenderer>();
            this.Body.sortingLayerID = SortingLayer.NameToID("Item");
        }

        private string GetItemObjectBodyPrefabPath(DropItemType dropItemType)
        {
            switch (dropItemType)
            {
                case DropItemType.ExpS: return "Stage/Exp/ExpSBody.prefab";
                case DropItemType.ExpM: return "Stage/Exp/ExpMBody.prefab";
                case DropItemType.ExpL: return "Stage/Exp/ExpLBody.prefab";
                case DropItemType.ExpXL: return "Stage/Exp/ExpXLBody.prefab";
                case DropItemType.ExpMagnet: return "Stage/Item/Item_Magnet.prefab";
                case DropItemType.Tutorial3SkillBox:
                case DropItemType.Tutorial5SkillBox:
                case DropItemType.SkillBox: return "Stage/Item/Item_Potion.prefab";
                case DropItemType.ItemBox: return "Stage/Item/Item_Box.prefab";
                case DropItemType.HpResorative: return "Stage/Item/Item_HP.prefab"; // HP회복제 (고기)
                case DropItemType.Bomb: return "Stage/Item/Item_Bomb.prefab";
                case DropItemType.Gold: return "Stage/Item/Item_Gold.prefab";
                case DropItemType.Fence: return "Stages/Items/FenceBody.prefab";
                case DropItemType.Gem: return "Stages/Items/Gems/GemBody.prefab";
                case DropItemType.RandomEquipmentElement: return "Stages/Items/EquipmentTicketRandomBody.prefab";
                case DropItemType.DefenceSupportBox: return "Stages/Items/DefenceSupportBoxBody.prefab";
                case DropItemType.StarCore: return "Stages/Items/StarCoreBody.prefab";
                default:
                    {
                        throw new NotImplementedException();
                    }
            }
        }

    }
}
