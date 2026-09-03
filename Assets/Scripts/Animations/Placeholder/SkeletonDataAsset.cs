#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SamMul.Animations.Placeholder
{
    [Serializable]
    public class PlaceholderClip
    {
        public string name = string.Empty;
        public float duration = SkeletonData.DefaultClipDuration;
        public bool loop;
    }

    /// <summary>
    /// Authoring asset for a placeholder skeleton. Clips missing from the list are created on demand
    /// by <see cref="SkeletonData.FindAnimation"/>, so an empty asset (or none at all) still works.
    /// </summary>
    [CreateAssetMenu(menuName = "Placeholder/Skeleton Data Asset", fileName = "SkeletonData")]
    public class SkeletonDataAsset : ScriptableObject
    {
        public List<PlaceholderClip> clips = new List<PlaceholderClip>();
        public List<string> bones = new List<string>();
        [Tooltip("Body size in world units. Pivot is bottom-center.")]
        public Vector2 size = SkeletonData.DefaultSize;
        public Color bodyColor = SkeletonData.DefaultBodyColor;

        private SkeletonData? _skeletonData;

        public SkeletonData GetSkeletonData(bool quiet)
        {
            _skeletonData ??= SkeletonData.Create(this);
            return _skeletonData;
        }

        public void Clear()
        {
            _skeletonData = null;
        }
    }
}
