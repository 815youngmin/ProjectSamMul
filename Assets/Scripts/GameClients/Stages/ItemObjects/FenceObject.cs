using Shared.GameDataTypes;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.ItemObjects
{
    public class FenceObject : BreakableItemObject
    {
        protected override Vector2 BodySpawnLocalOffset => Vector2.zero;

        private float _lastCollisionAttackedAt;
        private const float _attackDuration = 0.3f;

        public static FenceObject Create()
        {
            var gameObject = new GameObject("FenceObject");

            var expObject = gameObject.AddComponent<FenceObject>();
            expObject.AllocateSharedResources(DropItemType.Fence);
            gameObject.SetActive(true);

            return expObject;
        }

        public override void AllocateSharedResources(DropItemType dropItemType)
        {
            base.AllocateSharedResources(dropItemType);
        }

        public override void UpdateLogic(Stage stage, float now)
        {
            base.UpdateLogic(stage, now);

            if (_lastCollisionAttackedAt + _attackDuration < now)
            {
                if(HitOnCollisionArea(stage))
                {
                    _lastCollisionAttackedAt = now;
                }
            }
        }

        public override void OnBroken(Character attacker, float damage, Stage stage)
        {
            // NOTE: 보스가 죽을때까지 부셔지면 안되는 오브젝트이기 때문에 파괴 처리하지 않고 데미지 팝업만.
            stage.DamagePopups.CreateDamagePopup(null, this.transform.position, damage, Vector2.zero, true);
        }

        public void InitializeFenceObject(Vector2 spawnPosition, Stage stage)
        {
            base.InitializeBreakableItemObject(spawnPosition);
            var spriteRenderer = this.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = ResourcePool.Instance.LoadResource<Sprite>(stage.StaticData.FenceResourcePath);
            }

        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
        }

        private bool HitOnCollisionArea(Stage stage)
        {
            AllianceType targetAlliance = AllianceType.Players;
            // NOTE: 콜라이더로 인한 닿으면 살짝 밀리는 처리가 있어, 0.01정도 낮춤으로 밀린뒤 이동처리가 없을시 맞는 처리가 없게함.
            var collisionArea = new CircularTargetArea(this.transform.position, BreakableItemObject.ITEM_COLLIDER_RADIUS - 0.01f);

            List<Character> hitCharacter= new List<Character>();
            stage.FindAliveCharactersInArea(targetAlliance, collisionArea, hitCharacter);

            if (0 == hitCharacter.Count)
            {
                return false;
            }

            float collisionDamage = 1;
            foreach (var target in hitCharacter)
            {
                Vector2 position = this.transform.position;
                var deltaPosition = (target.Pos - position);
                var hitVector = deltaPosition.normalized;

                // NOTE: FenceObject는 Character 아니므로 null을 넘겨준다.
                target.Hitted(stage, null, collisionDamage, hitVector, position + deltaPosition * 0.4f, string.Empty);
            }

            return true;
        }
    }
}
