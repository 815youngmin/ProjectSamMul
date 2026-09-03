using Shared.GameDataTypes;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.PCs;
using SamMul.GameClients.Stages.Characters.Stats;
using SamMul.UnityHelpers;

namespace SamMul.GameClients.Stages.ItemObjects
{
    public class ExpMagnetObject : AcquirableItemObject
    {

        public static ExpMagnetObject Create()
        {
            var gameObject = new GameObject("ExpMagnetObject");

            var magnetObject = gameObject.AddComponent<ExpMagnetObject>();

            magnetObject.AllocateSharedResources(DropItemType.ExpMagnet);
            gameObject.SetActive(true);

            return magnetObject;
        }

        public override void AllocateSharedResources(DropItemType dropItemType)
        {
            base.AllocateSharedResources(dropItemType);

        }

        public void InitializeExpMagnetObject(Vector2 spawnPosition)
        {
            base.InitializeAcquirableItemObject(spawnPosition);

        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
        }

        public override void OnAcquired(PlayerCharacter owner, Stage stage)
        {
            this.RunAcquiredAnimation(owner.Pos, onCompleted: () =>
            {
                UnityGlobal.Sounds.PlayBySoundPrefab("Sounds/SoundEffects/CommonSkills/13-Magnet_SFX.prefab", owner.Pos);
                stage.RemoveAcquirableItemObject(this);
                owner.CustomParameters.IncreaseParameterValue(CustomParameterType.AcquiredExpMagnets, 1.0f);

                foreach (var acquirableItem in stage.TakeAllExpObjects())
                {
                    acquirableItem.OnAcquired(owner, stage);
                    // NOTE: OnAcquired 호출즉시 경험치가 오르지는 않는다. 획득 애니메이션 재생 다 끝나야 들어온다. 
                }
            });
        }
        public override void OnAcquiredWithSlime(PlayerCharacter owner, Stage stage)
        {
            this.RunAcquiredAnimationWithSlime(owner, onCompleted: () =>
            {
                UnityGlobal.Sounds.PlayBySoundPrefab("Sounds/SoundEffects/CommonSkills/13-Magnet_SFX.prefab", owner.Pos);
                stage.RemoveAcquirableItemObject(this);
                owner.CustomParameters.IncreaseParameterValue(CustomParameterType.AcquiredExpMagnets, 1.0f);

                foreach (var acquirableItem in stage.TakeAllExpObjects())
                {
                    acquirableItem.OnAcquired(owner, stage);
                    // NOTE: OnAcquired 호출즉시 경험치가 오르지는 않는다. 획득 애니메이션 재생 다 끝나야 들어온다. 
                }
            });
        }
    }
}
