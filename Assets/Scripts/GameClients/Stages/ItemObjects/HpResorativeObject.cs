using Shared.GameDataTypes;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.PCs;
using SamMul.GameClients.Stages.Characters.Stats;
using SamMul.UnityHelpers;

namespace SamMul.GameClients.Stages.ItemObjects
{
    public class HpResorativeObject : AcquirableItemObject
    {

        public static HpResorativeObject Create()
        {
            var gameObject = new GameObject("HpResorativeObject");

            var hpObject = gameObject.AddComponent<HpResorativeObject>();

            hpObject.AllocateSharedResources(DropItemType.HpResorative);
            gameObject.SetActive(true);

            return hpObject;
        }

        public override void AllocateSharedResources(DropItemType dropItemType)
        {
            base.AllocateSharedResources(dropItemType);

        }

        public void InitializeHpResorativeObject(Vector2 spawnPosition)
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
                owner.RecoverHPFromResorativeObject(stage);
                stage.RemoveAcquirableItemObject(this);
                owner.CustomParameters.IncreaseParameterValue(CustomParameterType.AcquiredHpResorative, 1.0f);
                UnityGlobal.Sounds.PlayBySoundPrefab("Sounds/SoundEffects/CommonSkills/9-AcquiredHP_SFX.prefab", owner.Pos);
            });
        }
        public override void OnAcquiredWithSlime(PlayerCharacter owner, Stage stage)
        {
            this.RunAcquiredAnimationWithSlime(owner, onCompleted: () =>
            {
                owner.RecoverHPFromResorativeObject(stage);
                stage.RemoveAcquirableItemObject(this);
                owner.CustomParameters.IncreaseParameterValue(CustomParameterType.AcquiredHpResorative, 1.0f);
                UnityGlobal.Sounds.PlayBySoundPrefab("Sounds/SoundEffects/CommonSkills/9-AcquiredHP_SFX.prefab", owner.Pos);
            });
        }
    }
}
