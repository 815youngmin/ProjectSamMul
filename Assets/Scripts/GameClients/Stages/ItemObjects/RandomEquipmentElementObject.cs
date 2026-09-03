using Shared.GameDataTypes;
using UnityEngine;
using Z.GameClients.Stages.Characters.PCs;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.ItemObjects
{
    public class RandomEquipmentElementObject : AcquirableItemObject
    {
        public static RandomEquipmentElementObject Create()
        {
            var gameObject = new GameObject("RandomEquipmentElementObject");
            var randomEquipmentElementObject = gameObject.AddComponent<RandomEquipmentElementObject>();
            randomEquipmentElementObject.AllocateSharedResources(Shared.GameDataTypes.DropItemType.RandomEquipmentElement);
            gameObject.SetActive(true);

            return randomEquipmentElementObject;
        }

        public override void AllocateSharedResources(DropItemType dropItemType)
        {
            base.AllocateSharedResources(dropItemType);
            Body.transform.localScale = Vector3.one * 0.9f;
        }
        public void InitializeRandomEquipmentElementObject(Vector2 spawnPosition)
        {
            this.InitializeAcquirableItemObject(spawnPosition);
        }

        public override void OnAcquired(PlayerCharacter owner, Stage stage)
        {
            this.RunAcquiredAnimation(owner.Pos, onCompleted: () =>
            {
                owner.AcquireRandomEquipmentElement();
                stage.RemoveAcquirableItemObject(this);
            });
        }

        public override void OnAcquiredWithSlime(PlayerCharacter owner, Stage stage)
        {
            this.RunAcquiredAnimationWithSlime(owner, onCompleted: () =>
            {
                owner.AcquireRandomEquipmentElement();
                stage.RemoveAcquirableItemObject(this);
            });
        }
    }
}
