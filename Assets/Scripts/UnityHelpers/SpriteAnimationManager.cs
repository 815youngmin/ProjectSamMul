#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Z.ResourcePools;

namespace Z.UnityHelpers
{
    /// <summary>
    /// 일회성 스프라이트 애니메이션을 생성/재생하고, 끝나면 리소스 풀로 돌려보냅니다.
    /// </summary>
    public class SpriteAnimationManager
    {
        private readonly List<SpriteAnimationHandler> _aliveSprites = new List<SpriteAnimationHandler>();

        public void ClearBeforeChangingScene(Scene relatedScene)
        {
            for (int i = _aliveSprites.Count - 1; i >= 0; --i)
            {
                var sprite = _aliveSprites[i];
                if (sprite == null || sprite.gameObject.scene == relatedScene)
                {
                    _aliveSprites.RemoveAt(i);
                    if (sprite != null)
                    {
                        GameObject.Destroy(sprite.gameObject);
                    }
                }
            }
        }

        public void Update()
        {
            for (int i = _aliveSprites.Count - 1; i >= 0; --i)
            {
                var sprite = _aliveSprites[i];
                if (sprite == null)
                {
                    _aliveSprites.RemoveAt(i);
                }
                else if (!sprite.IsAlive)
                {
                    _aliveSprites.RemoveAt(i);
                    this.PutBack(sprite);
                }
            }
        }

        /// <param name="resourcePath"><see cref="SpriteAnimationHandler"/>를 가진 프리팹의 리소스 경로</param>
        public void CreateAndPlaySpriteAnimation(
            string resourcePath,
            Vector2 position,
            Vector2 scale,
            GameObject? parent)
        {
            this.Create(resourcePath, position, Vector3.right, scale, flipX: false, flipY: false, parent, null, null);
        }

        /// <param name="rightVector">이미지의 오른쪽(1,0) 벡터를 향하게 할 방향</param>
        public void CreateAndPlaySpriteAnimation(
            string resourcePath,
            Vector2 position,
            Vector3 rightVector,
            Vector2 scale,
            GameObject? parent,
            SpriteAnimationHandler.EventHandler? hitEventHandler,
            SpriteAnimationHandler.EventHandler? endEventHandler)
        {
            this.Create(resourcePath, position, rightVector, scale, flipX: false, flipY: false, parent, hitEventHandler, endEventHandler);
        }

        public void CreateAndPlaySpriteAnimation(
            string resourcePath,
            Vector2 position,
            Vector3 rightVector,
            Vector2 scale,
            bool flipX,
            bool flipY,
            GameObject? parent,
            SpriteAnimationHandler.EventHandler? hitEventHandler,
            SpriteAnimationHandler.EventHandler? endEventHandler)
        {
            this.Create(resourcePath, position, rightVector, scale, flipX, flipY, parent, hitEventHandler, endEventHandler);
        }

        /// <summary>
        /// 프리팹의 스케일을 그대로 사용합니다.
        /// </summary>
        public void CreateAndPlaySpriteAnimation(
            string resourcePath,
            Vector2 position,
            Vector3 rightVector,
            bool flipX,
            bool flipY,
            GameObject? parent,
            SpriteAnimationHandler.EventHandler? hitEventHandler,
            SpriteAnimationHandler.EventHandler? endEventHandler)
        {
            this.Create(resourcePath, position, rightVector, scale: null, flipX, flipY, parent, hitEventHandler, endEventHandler);
        }

        private void Create(
            string resourcePath,
            Vector2 position,
            Vector3 rightVector,
            Vector2? scale,
            bool flipX,
            bool flipY,
            GameObject? parent,
            SpriteAnimationHandler.EventHandler? hitEventHandler,
            SpriteAnimationHandler.EventHandler? endEventHandler)
        {
            var spriteAnimation = ResourcePool.Instance.InstantiateFromResource<SpriteAnimationHandler>(resourcePath);
            spriteAnimation.AllocateSharedResources(resourcePath);

            var transform = spriteAnimation.transform;
            transform.SetParent(parent != null ? parent.transform : null);
            transform.position = position;
            if (scale.HasValue)
            {
                transform.localScale = scale.Value;
            }
            transform.rotation = Quaternion.identity;
            transform.right = rightVector;
            spriteAnimation.gameObject.SetActive(true);

            var renderer = spriteAnimation.SpriteRenderer;
            renderer.flipX = flipX;
            renderer.flipY = flipY;
            if (renderer.sortingLayerID == SortingLayer.NameToID("Object"))
            {
                renderer.sortingOrder = (int)(transform.position.y * -100.0f);
            }

            spriteAnimation.InitializeAndPlay(hitEventHandler, endEventHandler);
            _aliveSprites.Add(spriteAnimation);
        }

        private void PutBack(SpriteAnimationHandler element)
        {
            if (string.IsNullOrEmpty(element.PoolingKey))
            {
                Debug.LogError($"{nameof(SpriteAnimationManager)}를 통해 생성되지 않은 애니메이션을 반환받았습니다. 파괴합니다.");
                GameObject.Destroy(element.gameObject);
                return;
            }

            ResourcePool.Instance.PutBackInstance(element.PoolingKey, element.gameObject);
        }
    }
}
