using DG.Tweening;
using Shared.GameDataTypes;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;

namespace SamMul.GameClients.Stages.ItemObjects
{
    // 플레이어의 공격에 의해 부서질 수 있는 오브젝트
    public abstract class BreakableItemObject : ItemObjectBase
    {
        private static readonly Vector2 BODY_SPAWN_LOCAL_POSITION = new Vector2(0.0f, 4.0f);
        public const float ITEM_COLLIDER_RADIUS = 1.0f;

        protected abstract Vector2 BodySpawnLocalOffset { get; }

        // 부서질 수 있는 오브젝트는 여러 가지 공격에 대해 피격당할 수 있어야 하고,
        // 피격 판정을 Collider 기반 유니티 물리를 쓰고 있기 때문에 콜라이더를 할당해야 한다. 
        protected CircleCollider2D _itemCollider;

        private Sequence _spawningAnimation;

        public override void AllocateSharedResources(DropItemType dropItemType)
        {
            base.AllocateSharedResources(dropItemType);

            _itemCollider = this.gameObject.AddComponent<CircleCollider2D>();
            _itemCollider.offset = new Vector2(0f, 0f);
            _itemCollider.radius = ITEM_COLLIDER_RADIUS;

            gameObject.layer = LayerMask.NameToLayer("BreakableItem");
            Body.transform.localPosition = BODY_SPAWN_LOCAL_POSITION + BodySpawnLocalOffset;

            _spawningAnimation = DOTween.Sequence()
                .AppendInterval(0.05f)
                .Append(Body.transform.DOLocalMoveY(BodySpawnLocalOffset.y, 0.2f).SetEase(Ease.OutCubic))
                .AppendInterval(0.01f)
                .Append(Body.transform.DOLocalMoveY(BodySpawnLocalOffset.y + 0.1f, 0.1f).SetEase(Ease.InSine))
                .AppendInterval(0.01f)
                .Append(Body.transform.DOLocalMoveY(BodySpawnLocalOffset.y, 0.1f).SetEase(Ease.OutCubic))
                .SetAutoKill(false)
                .Pause();
        }

        protected void InitializeBreakableItemObject(Vector2 spawnPosition)
        {
            base.InitializeBase(spawnPosition);

            Body.transform.localPosition = BODY_SPAWN_LOCAL_POSITION;

            _spawningAnimation.Restart();
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();

            transform.localScale = Vector3.one;

            _spawningAnimation.Pause();
        }

        public virtual void UpdateLogic(Stage stage, float now)
        {
            Body.sortingOrder = (int)(transform.position.y * -100.0f);
        }

        // 부순다!
        public abstract void OnBroken(Character attacker, float damage, Stage stage);

        private void OnDestroy()
        {
            _spawningAnimation.Kill();
            _spawningAnimation = null;
        }
    }
}
