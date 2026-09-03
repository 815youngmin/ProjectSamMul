#nullable enable
using System.Collections.Generic;

namespace Z.Animations.Placeholder
{
    /// <summary>
    /// List type kept under the original runtime's name so ported code compiles unchanged.
    /// </summary>
    public class ExposedList<T> : List<T>
    {
    }

    public class EventData
    {
        public string Name { get; }

        public EventData(string name)
        {
            Name = name;
        }

        public override string ToString() => Name;
    }

    public class Event
    {
        public float Time { get; }
        public EventData Data { get; }
        public int Int { get; set; }
        public float Float { get; set; }
        public string? String { get; set; }

        public Event(float time, EventData data)
        {
            Time = time;
            Data = data;
        }
    }

    /// <summary>
    /// A placeholder clip: a name, a duration and a loop hint. Nothing is keyframed.
    /// </summary>
    public class Animation
    {
        public string Name { get; }
        public float Duration { get; set; }
        /// <summary>Loop hint used when the clip is started through <see cref="SkeletonAnimation.AnimationName"/>.</summary>
        public bool Loop { get; set; }
        public ExposedList<Event> Events { get; } = new ExposedList<Event>();
        /// <summary>Visual cue derived from the clip name (see <see cref="PlaceholderStyle"/>).</summary>
        public AnimationCue Cue { get; }
        public bool IsMovement { get; }

        public Animation(string name, float duration, bool loop)
        {
            Name = name;
            Duration = duration;
            Loop = loop;
            Cue = PlaceholderStyle.CueOf(name);
            IsMovement = PlaceholderStyle.IsMovementName(name);
        }

        public override string ToString() => Name;
    }
}
