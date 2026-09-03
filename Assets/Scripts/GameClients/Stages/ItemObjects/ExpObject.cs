using Shared.GameDataTypes;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.PCs;
using SamMul.UnityHelpers;

namespace SamMul.GameClients.Stages.ItemObjects
{
    public class ExpObject : AcquirableItemObject
    {
        private long _expAmount;
        public long ExpAmount => _expAmount;

        public static ExpObject Create(DropItemType expObjectType)
        {
            Debug.Assert(IsExpObjectType(expObjectType));

            var gameObject = new GameObject("ExpObject");

            var expObject = gameObject.AddComponent<ExpObject>();

            expObject.AllocateSharedResources(expObjectType);
            gameObject.SetActive(true);

            return expObject;
        }

        public override void AllocateSharedResources(DropItemType dropItemType)
        {
            base.AllocateSharedResources(dropItemType);
        }

        public void InitializeExpObject(long expAmount, Vector2 spawnPosition)
        {
            base.InitializeAcquirableItemObject(spawnPosition);

            _expAmount = expAmount;
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();

            _expAmount = 0;
        }

        public override void OnAcquired(PlayerCharacter owner, Stage stage)
        {
            this.RunAcquiredAnimation(owner.Pos, onCompleted: () =>
            {
                // NOTE: GainExp에서는 레벨업 안 한다. 일단 EXP쭉 쌓고, 
                // 이후에 CheckAndIncreaseOneLevel()을 호출할 때마다, 레벨업이 가능하면 레벨업한다.
                owner.ReserveToGainExp(_expAmount);
                stage.RemoveAcquirableItemObject(this);
                UnityGlobal.Sounds.PlayBySoundPrefab("Sounds/SoundEffects/CommonSkills/6-MeatCube_SFX.prefab", owner.Pos);
            });
        }

        public override void OnAcquiredWithSlime(PlayerCharacter owner, Stage stage)
        {
            this.RunAcquiredAnimationWithSlime(owner, onCompleted: () =>
            {
                // NOTE: GainExp에서는 레벨업 안 한다. 일단 EXP쭉 쌓고, 
                // 이후에 CheckAndIncreaseOneLevel()을 호출할 때마다, 레벨업이 가능하면 레벨업한다.
                owner.ReserveToGainExp(_expAmount);
                stage.RemoveAcquirableItemObject(this);
                UnityGlobal.Sounds.PlayBySoundPrefab("Sounds/SoundEffects/CommonSkills/6-MeatCube_SFX.prefab", owner.Pos);
            });
        }

        public static DropItemType GetItemTypeByAmount(long expAmount)
        {
            if (expAmount >= 2400)
            {
                return DropItemType.ExpXL;
            }
            else if (expAmount >= 800)
            {
                return DropItemType.ExpL;
            }
            else if (expAmount >= 200)
            {
                return DropItemType.ExpM;
            }

            return DropItemType.ExpS;
        }
    }
}
