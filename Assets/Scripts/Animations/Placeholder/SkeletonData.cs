#nullable enable
using UnityEngine;

namespace Z.Animations.Placeholder
{
    public class Skin
    {
        public string Name { get; }

        public Skin(string name)
        {
            Name = name;
        }

        public void AddSkin(Skin? other)
        {
            // Placeholder skins carry no attachments, so there is nothing to merge.
        }
    }

    public class Attachment
    {
        public string Name { get; }

        public Attachment(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Static description of a placeholder skeleton. Every Find* lookup creates the missing entry on demand,
    /// so callers never get null back for a non-empty name.
    /// </summary>
    public class SkeletonData
    {
        public const float DefaultClipDuration = 0.5f;
        public static readonly Vector2 DefaultSize = new Vector2(6f, 8f);
        public static readonly Color DefaultBodyColor = new Color(0.75f, 0.85f, 1f);

        public string Name { get; }
        public float Width { get; }
        public float Height { get; }
        public Color BodyColor { get; }
        public ExposedList<Animation> Animations { get; } = new ExposedList<Animation>();
        public ExposedList<string> Bones { get; } = new ExposedList<string>();
        public ExposedList<Skin> Skins { get; } = new ExposedList<Skin>();
        public ExposedList<EventData> Events { get; } = new ExposedList<EventData>();
        public Skin DefaultSkin { get; }

        private SkeletonData(string name, Vector2 size, Color bodyColor)
        {
            Name = name;
            Width = size.x;
            Height = size.y;
            BodyColor = bodyColor;
            DefaultSkin = new Skin("default");
            Skins.Add(DefaultSkin);
        }

        public static SkeletonData Create(SkeletonDataAsset? asset)
        {
            if (asset == null)
            {
                return new SkeletonData("placeholder", DefaultSize, DefaultBodyColor);
            }

            var data = new SkeletonData(asset.name, asset.size, asset.bodyColor);
            foreach (var clip in asset.clips)
            {
                if (!string.IsNullOrEmpty(clip.name))
                {
                    data.Animations.Add(new Animation(clip.name, Mathf.Max(0f, clip.duration), clip.loop));
                }
            }
            foreach (var bone in asset.bones)
            {
                if (!string.IsNullOrEmpty(bone))
                {
                    data.Bones.Add(bone);
                }
            }
            return data;
        }

        public Animation? FindAnimation(string? name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            foreach (var animation in Animations)
            {
                if (animation.Name == name)
                {
                    return animation;
                }
            }

            var created = new Animation(name!, DefaultClipDuration, PlaceholderStyle.IsLoopingName(name!));
            Animations.Add(created);
            return created;
        }

        public EventData? FindEvent(string? name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            foreach (var eventData in Events)
            {
                if (eventData.Name == name)
                {
                    return eventData;
                }
            }

            var created = new EventData(name!);
            Events.Add(created);
            return created;
        }

        public Skin? FindSkin(string? name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            foreach (var skin in Skins)
            {
                if (skin.Name == name)
                {
                    return skin;
                }
            }

            var created = new Skin(name!);
            Skins.Add(created);
            return created;
        }
    }
}
