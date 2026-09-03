#nullable enable
using System.IO;
using UnityEngine;

namespace Z.Animations.Placeholder
{
    /// <summary>
    /// Replacement for the original loading helper, built on the placeholder runtime. Same call surface,
    /// so call sites only needed their using directive switched.
    /// </summary>
    public static class SpineHelper
    {
        public static SkeletonAnimation LoadSpine(GameObject parentGameObject, string resourcePath)
        {
            return LoadSpine(parentGameObject, resourcePath, "Object", 0);
        }

        public static SkeletonAnimation LoadSpine(GameObject parentGameObject, string resourcePath, string sortingLayerName)
        {
            return LoadSpine(parentGameObject, resourcePath, sortingLayerName, 0);
        }

        public static SkeletonAnimation LoadSpine(GameObject parentGameObject, string resourcePath, string sortingLayerName, int sortingOrder)
        {
            Debug.Assert(parentGameObject);
            var skeletonAnimation = parentGameObject.AddComponent<SkeletonAnimation>();
            skeletonAnimation.skeletonDataAsset = LoadSkeletonDataAsset(resourcePath);
            skeletonAnimation.Initialize(true);

            var renderer = skeletonAnimation.GetComponent<MeshRenderer>();
            renderer.sortingLayerID = SortingLayer.NameToID(sortingLayerName);
            renderer.sortingOrder = sortingOrder;
            return skeletonAnimation;
        }

        /// <summary>
        /// Loads a placeholder asset from Resources (file extension stripped). A missing asset is fine:
        /// the runtime creates clips on demand.
        /// </summary>
        public static SkeletonDataAsset? LoadSkeletonDataAsset(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath))
            {
                return null;
            }

            string extension = Path.GetExtension(resourcePath);
            string path = string.IsNullOrEmpty(extension) ? resourcePath : resourcePath.Substring(0, resourcePath.Length - extension.Length);
            return Resources.Load<SkeletonDataAsset>(path);
        }

        public static bool SetMixHelper(SkeletonAnimation skeletonAnimation, Animation? left, Animation? right, float duration)
        {
            if (left == null || right == null)
            {
                return false;
            }

            skeletonAnimation.AnimationState.Data.SetMix(left, right, duration);
            return true;
        }
    }
}
