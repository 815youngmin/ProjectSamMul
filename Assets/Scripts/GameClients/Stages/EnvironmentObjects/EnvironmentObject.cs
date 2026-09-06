#nullable enable
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using SamMul.ObjectPools;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.EnvironmentObjects
{
    public enum EnvironmentObjectType
    {
        QuestTargetPositionFlag,    // marks a target position on the stage floor
    }

    /// <summary>
    /// A static, pooled decoration on the stage floor: one body sprite and one shadow sprite.
    /// </summary>
    public class EnvironmentObject : MonoBehaviour, IPoolible<EnvironmentObjectType>
    {
        private const string SHADOW_SPRITE_PATH = "Stage/Common/ItemShadow.png";

        public EnvironmentObjectType EnvironmentObjectType { get; private set; }
        public SpriteRenderer Body { get; private set; } = null!;
        public SpriteRenderer? Shadow { get; private set; }

        Scene IPoolible<EnvironmentObjectType>.RelatedScene => this.gameObject.scene;
        EnvironmentObjectType IPoolible<EnvironmentObjectType>.PoolKey => this.EnvironmentObjectType;

        public static EnvironmentObject Create(EnvironmentObjectType environmentObjectType)
        {
            var gameObject = new GameObject("EnvironmentObject");
            var environmentObject = gameObject.AddComponent<EnvironmentObject>();
            environmentObject.AllocateSharedResources(environmentObjectType);
            gameObject.SetActive(true);
            return environmentObject;
        }

        public virtual void AllocateSharedResources(EnvironmentObjectType environmentObjectType)
        {
            this.EnvironmentObjectType = environmentObjectType;

            this.Body = CreateChildSprite("EnvironmentObjectBody", "Object");
            this.Body.sprite = ResourcePool.Instance.LoadResource<Sprite>(GetBodySpritePath(environmentObjectType));

            this.Shadow = CreateChildSprite("EnvironmentObjectShadow", "LowShadow");
            this.Shadow.sprite = ResourcePool.Instance.LoadResource<Sprite>(SHADOW_SPRITE_PATH);
            this.Shadow.transform.localScale = new Vector3(1f, 0.65f, 1f);
            this.Shadow.color = new Color(1f, 1f, 1f, 0.5f);
        }

        public virtual void Initialize()
        {
            this.transform.localScale = Vector3.one;
            this.Body.sortingOrder = (int)(this.transform.position.y * -100f);
        }

        public virtual void PuttingBackToPool()
        {
            this.gameObject.SetActive(false);
        }

        private SpriteRenderer CreateChildSprite(string name, string sortingLayerName)
        {
            var child = new GameObject(name);
            child.transform.SetParent(this.transform, worldPositionStays: false);
            child.layer = this.gameObject.layer;

            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sortingLayerID = SortingLayer.NameToID(sortingLayerName);
            return renderer;
        }

        private static string GetBodySpritePath(EnvironmentObjectType environmentObjectType)
        {
            return environmentObjectType switch
            {
                EnvironmentObjectType.QuestTargetPositionFlag => "Stages/Fences/Chapter10Fence.png",
                _ => throw new NotImplementedException($"{environmentObjectType} is not supported."),
            };
        }
    }
}
