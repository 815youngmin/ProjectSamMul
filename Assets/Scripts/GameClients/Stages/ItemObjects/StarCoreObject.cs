using Shared.GameDataTypes;
using UnityEngine;
using Z.GameClients.Stages.Characters.PCs;
using Z.GameClients.Stages.Characters.Stats;
using Z.ResourcePools;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.ItemObjects
{
    public class StarCoreObject : AcquirableItemObject
    {
        public static StarCoreObject Create()
        {
            var starCore = new GameObject("StarCoreObject").AddComponent<StarCoreObject>();
            starCore.AllocateSharedResources(DropItemType.StarCore);
            return starCore;
        }

        public override void AllocateSharedResources(DropItemType dropItemType)
        {
            base.AllocateSharedResources(dropItemType);
            Body.sortingLayerID = SortingLayer.NameToID("Object");
            this.AllocateShadowComponent();
        }

        private void AllocateShadowComponent()
        {
            var shadowObject = new GameObject("Shadow");
            shadowObject.transform.SetParent(this.transform, worldPositionStays: false);

            var shadow = shadowObject.AddComponent<SpriteRenderer>();
            shadow.sprite = ResourcePool.Instance.LoadResource<Sprite>("Stages/Items/ItemShadow.png");
            shadow.color = new Color(shadow.color.r, shadow.color.g, shadow.color.b, 0.50f);
            shadow.sortingLayerID = SortingLayer.NameToID("LowShadow");
            shadow.drawMode = SpriteDrawMode.Simple;
            shadow.transform.localPosition = new Vector3(0.0f, -0.5f, 0.0f);
            shadow.transform.localScale = new Vector2(3.0f, 1.0f);
        }

        public void InitializeStarCoreObject(Vector2 spawnPosition)
        {
            base.InitializeAcquirableItemObject(spawnPosition);
            this.transform.localScale = new Vector2(1.0f, 1.0f);
            Body.sortingOrder = (int)(transform.position.y * -100.0f) - 10;
        }

        public override void OnAcquired(PlayerCharacter owner, Stage stage)
        {
            this.RunAcquiredAnimation(owner.Pos, onCompleted: () =>
            {
                owner.AcquireStarCore();
                owner.CustomParameters.IncreaseParameterValue(CustomParameterType.AcquiredStarCores, 1.0f);
                stage.RemoveAcquirableItemObject(this);

                UnityGlobal.Sounds.PlayBySoundPrefab("Sounds/SoundEffects/CommonSkills/AcquireStarCore.prefab", owner.Pos);
            });
        }

        public override void OnAcquiredWithSlime(PlayerCharacter owner, Stage stage)
        {
            this.RunAcquiredAnimationWithSlime(owner, onCompleted: () =>
            {
                owner.AcquireStarCore();
                owner.CustomParameters.IncreaseParameterValue(CustomParameterType.AcquiredStarCores, 1.0f);
                stage.RemoveAcquirableItemObject(this);

                UnityGlobal.Sounds.PlayBySoundPrefab("Sounds/SoundEffects/CommonSkills/AcquireStarCore.prefab", owner.Pos);
            });
        }

    }
}
