using Shared.GameDataTypes;
using System;
using UnityEngine;
using Z.GameClients.Stages.Characters.PCs;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.ItemObjects
{
    public class GoldObject : AcquirableItemObject
    {
        private static readonly long DEFAULT_GOLD_S_AMOUNT = 1;
        private static readonly long DEFAULT_GOLD_M_AMOUNT = 10;
        private static readonly long DEFAULT_GOLD_L_AMOUNT = 50;

        private Animator _animator;
        private long _amount;
        private Vector2 _spawnPos;

        public static GoldObject Create()
        {
            var gameObject = new GameObject("GoldObject");

            var goldObject = gameObject.AddComponent<GoldObject>();

            goldObject.AllocateSharedResources(DropItemType.Gold);
            gameObject.SetActive(true);

            return goldObject;
        }

        public override void AllocateSharedResources(DropItemType dropItemType)
        {
            base.AllocateSharedResources(dropItemType);

            _animator = Body.GetComponent<Animator>();
        }

        public void InitializeGoldObject(long amount, Vector2 spawnPosition)
        { 
            base.InitializeAcquirableItemObject(spawnPosition);
            _spawnPos = spawnPosition;
            _amount = amount;
            _animator.Play(GetGoldAnimationStateName());
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
        }

        public override void OnAcquired(PlayerCharacter owner, Stage stage)
        {
            this.RunAcquiredAnimation(owner.Pos, onCompleted: () =>
            {
                owner.GainGold(_amount);
                stage.RemoveAcquirableItemObject(this);
                UnityGlobal.Sounds.PlayBySoundPrefab("Sounds/SoundEffects/CommonSkills/7-AcquiredGold_SFX.prefab", owner.Pos);
            });
            UnityGlobal.SpriteAnimations.CreateAndPlaySpriteAnimation("Stages/Items/Coin/CoinGetEffect.prefab", _spawnPos, Vector3.one * 1.5f, null);
        }

        public override void OnAcquiredWithSlime(PlayerCharacter owner, Stage stage)
        {
            this.RunAcquiredAnimationWithSlime(owner, onCompleted: () =>
            {
                owner.GainGold(_amount);
                stage.RemoveAcquirableItemObject(this);
                UnityGlobal.Sounds.PlayBySoundPrefab("Sounds/SoundEffects/CommonSkills/7-AcquiredGold_SFX.prefab", owner.Pos);
            });
            UnityGlobal.SpriteAnimations.CreateAndPlaySpriteAnimation("Stages/Items/Coin/CoinGetEffect.prefab", _spawnPos, Vector3.one * 1.5f, null);

        }

        private string GetGoldAnimationStateName()
        {
            if (_amount >= DEFAULT_GOLD_L_AMOUNT)
            {
                return "GoldCoin";
            }
            else if (_amount >= DEFAULT_GOLD_M_AMOUNT)
            {
                return "SliverCoin";
            }
            else if (_amount >= DEFAULT_GOLD_S_AMOUNT)
            {
                return "BronzeCoin";
            }
            else
            {
                throw new NotImplementedException($"비정상적인 값입니다. 골드의 양은 1보다 커야 합니다.");
            }
        }
    }
}
