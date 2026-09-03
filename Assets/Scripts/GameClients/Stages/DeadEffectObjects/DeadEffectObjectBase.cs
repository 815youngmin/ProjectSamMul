using UnityEngine;
using UnityEngine.SceneManagement;
using SamMul.ObjectPools;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.DeadEffectObjects
{
    public enum DeadEffectType
    {
        Bone,
        Skull,
    }
    public class DeadEffectObjectBase : MonoBehaviour, IPoolible<DeadEffectType>
    {
        public DeadEffectType DeadEffectObjectType { get; private set; }
        public Scene RelatedScene => this.gameObject.scene;

        public DeadEffectType PoolKey => DeadEffectObjectType;

        private SpriteRenderer _spriteRenderer;

        public virtual void PuttingBackToPool()
        {
            this.gameObject.SetActive(false);
        }

        public void AllocateSharedResources(DeadEffectType type, string effectPath)
        {
            DeadEffectObjectType = type;

            _spriteRenderer = this.gameObject.AddComponent<SpriteRenderer>();
            _spriteRenderer.sprite = ResourcePool.Instance.LoadResource<Sprite>(effectPath);
            _spriteRenderer.sortingLayerName = "LowParticle";
        }

        public void Initialize(Vector2 position)
        {
            _spriteRenderer.color = Color.white;
            _spriteRenderer.sortingOrder = (int)((position.y-1f) * -100.0f) + ((int)position.x % 80);
        }

    }

}
