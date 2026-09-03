#nullable enable
using UnityEngine;

namespace Z.Animations.Placeholder
{
    /// <summary>Visual cue a placeholder body shows for a clip. Ordered by display priority.</summary>
    public enum AnimationCue
    {
        None = 0,
        Attack,
        Hit,
        Appear,
        Disappear,
        Dead,
    }

    /// <summary>
    /// Maps clip names to the few visual cues a placeholder body can show:
    /// attack = brighter, hit = red flash, dead = darken and fade, appear/disappear = alpha ramp,
    /// movement = vertical bob.
    /// </summary>
    public static class PlaceholderStyle
    {
        public static bool IsLoopingName(string name)
        {
            string n = name.ToLowerInvariant();
            return n.Contains("idle") || IsMovementName(n);
        }

        public static bool IsMovementName(string name)
        {
            string n = name.ToLowerInvariant();
            return n.Contains("walk") || n.Contains("run") || n.Contains("move");
        }

        public static AnimationCue CueOf(string name)
        {
            string n = name.ToLowerInvariant();
            if (n.Contains("die") || n.Contains("dead") || n.Contains("death"))
            {
                return AnimationCue.Dead;
            }
            if (n.Contains("disappear"))
            {
                return AnimationCue.Disappear;
            }
            if (n.Contains("appear"))
            {
                return AnimationCue.Appear;
            }
            if (n.Contains("hit"))
            {
                return AnimationCue.Hit;
            }
            if (n.Contains("attack") || n.Contains("fire") || n.Contains("skill") || n.Contains("summon"))
            {
                return AnimationCue.Attack;
            }
            return AnimationCue.None;
        }

        /// <summary>Tint for whatever is playing right now; the highest-priority cue across all tracks wins.</summary>
        public static Color Tint(AnimationState? state, Color baseColor)
        {
            var cue = AnimationCue.None;
            float progress = 0f;
            if (state != null)
            {
                foreach (var entry in state.Tracks)
                {
                    if (entry == null || ReferenceEquals(entry.Animation, AnimationState.EmptyAnimation))
                    {
                        continue;
                    }
                    if (entry.Animation.Cue > cue)
                    {
                        cue = entry.Animation.Cue;
                        progress = entry.NormalizedTime;
                    }
                }
            }

            switch (cue)
            {
                case AnimationCue.Attack:
                    return Color.Lerp(baseColor, Color.white, 0.6f);
                case AnimationCue.Hit:
                    return Color.Lerp(new Color(1f, 0.25f, 0.2f, baseColor.a), baseColor, progress);
                case AnimationCue.Appear:
                    return new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * progress);
                case AnimationCue.Disappear:
                    return new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * (1f - progress));
                case AnimationCue.Dead:
                    return new Color(baseColor.r * 0.5f, baseColor.g * 0.5f, baseColor.b * 0.5f, baseColor.a * Mathf.Lerp(1f, 0.25f, progress));
                default:
                    return baseColor;
            }
        }

        /// <summary>Vertical bob, as a fraction of body height, while a movement clip plays.</summary>
        public static float Bob(AnimationState? state)
        {
            if (state == null)
            {
                return 0f;
            }

            foreach (var entry in state.Tracks)
            {
                if (entry != null && entry.Animation.IsMovement)
                {
                    return Mathf.Abs(Mathf.Sin(entry.NormalizedTime * Mathf.PI * 2f)) * 0.05f;
                }
            }
            return 0f;
        }
    }
}
