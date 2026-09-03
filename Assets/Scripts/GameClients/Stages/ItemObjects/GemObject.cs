using Shared.GameDataTypes;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.PCs;
using SamMul.UnityHelpers;

namespace SamMul.GameClients.Stages.ItemObjects
{
    public class GemObject : AcquirableItemObject
    {
        private long _gemAmount;
        public long GemAmount => _gemAmount;

        public static GemObject Create()
        {
            var gameObject = new GameObject("GemObject");

            var gemObject = gameObject.AddComponent<GemObject>();

            gemObject.AllocateSharedResources(DropItemType.Gem);
            gameObject.SetActive(true);

            return gemObject;
        }

        public override void AllocateSharedResources(DropItemType dropItemType)
        {
            base.AllocateSharedResources(dropItemType);
        }

        public void InitializeGemObject(long gemAmount, Vector2 spawnPosition)
        {
            base.InitializeAcquirableItemObject(spawnPosition);

            _gemAmount = gemAmount;
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();

            _gemAmount = 0;
        }

        public override void OnAcquired(PlayerCharacter owner, Stage stage)
        {
            this.RunAcquiredAnimation(owner.Pos, onCompleted: () =>
            {
                owner.GainGem(_gemAmount);
                stage.RemoveAcquirableItemObject(this);
                UnityGlobal.Sounds.PlayBySoundPrefab("Sounds/SoundEffects/CommonSkills/7-AcquiredGold_SFX.prefab", owner.Pos);
            });
            UnityGlobal.SpriteAnimations.CreateAndPlaySpriteAnimation("Stages/Items/Coin/CoinGetEffect.prefab", owner.Pos, Vector3.one * 1.5f, null);
        }

        public override void OnAcquiredWithSlime(PlayerCharacter owner, Stage stage)
        {
            this.RunAcquiredAnimationWithSlime(owner, onCompleted: () =>
            {
                owner.GainGem(_gemAmount);
                stage.RemoveAcquirableItemObject(this);
                UnityGlobal.Sounds.PlayBySoundPrefab("Sounds/SoundEffects/CommonSkills/7-AcquiredGold_SFX.prefab", owner.Pos);
            });
            UnityGlobal.SpriteAnimations.CreateAndPlaySpriteAnimation("Stages/Items/Coin/CoinGetEffect.prefab", owner.Pos, Vector3.one * 1.5f, null);

        }

    }
}
