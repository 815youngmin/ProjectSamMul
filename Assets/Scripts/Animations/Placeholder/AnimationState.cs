#nullable enable
using System;
using System.Collections.Generic;

namespace Z.Animations.Placeholder
{
    public class AnimationStateData
    {
        public SkeletonData SkeletonData { get; }
        public float DefaultMix { get; set; }

        private readonly Dictionary<(Animation, Animation), float> _mixes = new Dictionary<(Animation, Animation), float>();

        public AnimationStateData(SkeletonData skeletonData)
        {
            SkeletonData = skeletonData;
        }

        public void SetMix(string fromName, string toName, float duration)
        {
            SetMix(SkeletonData.FindAnimation(fromName), SkeletonData.FindAnimation(toName), duration);
        }

        public void SetMix(Animation? from, Animation? to, float duration)
        {
            if (from == null || to == null)
            {
                return;
            }
            _mixes[(from, to)] = duration;
        }

        public float GetMix(Animation? from, Animation? to)
        {
            if (from == null || to == null)
            {
                return DefaultMix;
            }
            return _mixes.TryGetValue((from, to), out float duration) ? duration : DefaultMix;
        }
    }

    /// <summary>
    /// One queued or playing clip on a track. Mixing fields are stored but have no visual effect.
    /// </summary>
    public class TrackEntry
    {
        public int TrackIndex { get; internal set; }
        public Animation Animation { get; internal set; }
        public bool Loop { get; set; }
        public bool HoldPrevious { get; set; }
        public float Delay { get; set; }
        public float TrackTime { get; set; }
        public float TrackEnd { get; set; } = float.MaxValue;
        public float AnimationStart { get; set; }
        public float AnimationEnd { get; set; }
        public float AnimationLast { get; set; } = -1f;
        public float TimeScale { get; set; } = 1f;
        public float Alpha { get; set; } = 1f;
        public float MixTime { get; set; }
        public float MixDuration { get; set; }
        public TrackEntry? Next { get; internal set; }
        public TrackEntry? MixingFrom { get; internal set; }

        internal int CompletedLoops;
        internal bool CompleteFired;

        internal TrackEntry(int trackIndex, Animation animation, bool loop)
        {
            TrackIndex = trackIndex;
            Animation = animation;
            Loop = loop;
            AnimationEnd = animation.Duration;
        }

        public float AnimationTime
        {
            get
            {
                float duration = AnimationEnd - AnimationStart;
                if (Loop)
                {
                    return duration <= 0f ? AnimationStart : TrackTime % duration + AnimationStart;
                }
                return Math.Min(TrackTime + AnimationStart, AnimationEnd);
            }
        }

        public bool IsComplete => TrackTime >= AnimationEnd - AnimationStart;

        /// <summary>Track time at which the current play-through (or the clip, when not looping) ends.</summary>
        public float TrackComplete
        {
            get
            {
                float duration = AnimationEnd - AnimationStart;
                if (Loop)
                {
                    return duration <= 0f ? TrackTime : TrackTime + duration - TrackTime % duration;
                }
                return duration;
            }
        }

        /// <summary>0..1 progress through the current play-through.</summary>
        public float NormalizedTime
        {
            get
            {
                float duration = AnimationEnd - AnimationStart;
                if (duration <= 0f)
                {
                    return 1f;
                }
                return Loop ? TrackTime % duration / duration : Math.Min(TrackTime / duration, 1f);
            }
        }

        public event AnimationState.TrackEntryDelegate? Start;
        public event AnimationState.TrackEntryDelegate? Interrupt;
        public event AnimationState.TrackEntryDelegate? End;
        public event AnimationState.TrackEntryDelegate? Dispose;
        public event AnimationState.TrackEntryDelegate? Complete;
        public event AnimationState.TrackEntryEventDelegate? Event;

        internal void OnStart() => Start?.Invoke(this);
        internal void OnInterrupt() => Interrupt?.Invoke(this);
        internal void OnEnd() => End?.Invoke(this);
        internal void OnDispose() => Dispose?.Invoke(this);
        internal void OnComplete() => Complete?.Invoke(this);
        internal void OnEvent(Event e) => Event?.Invoke(this, e);

        public override string ToString() => Animation.Name;
    }

    /// <summary>
    /// Track-based clip scheduler with the same call surface as the original runtime: SetAnimation replaces,
    /// AddAnimation queues (delay measured on the previous entry's track time), empty animations clear a track,
    /// and Start/Interrupt/End/Complete fire on the entries as time advances.
    /// </summary>
    public class AnimationState
    {
        public delegate void TrackEntryDelegate(TrackEntry trackEntry);
        public delegate void TrackEntryEventDelegate(TrackEntry trackEntry, Event e);

        public static readonly Animation EmptyAnimation = new Animation("<empty>", 0f, false);

        public AnimationStateData Data { get; }
        public ExposedList<TrackEntry?> Tracks { get; } = new ExposedList<TrackEntry?>();
        public float TimeScale { get; set; } = 1f;

        public event TrackEntryDelegate? Start;
        public event TrackEntryDelegate? Interrupt;
        public event TrackEntryDelegate? End;
        public event TrackEntryDelegate? Dispose;
        public event TrackEntryDelegate? Complete;
        public event TrackEntryEventDelegate? Event;

        public AnimationState(AnimationStateData data)
        {
            Data = data;
        }

        public void Update(float delta)
        {
            delta *= TimeScale;
            for (int i = 0; i < Tracks.Count; i++)
            {
                var current = Tracks[i];
                if (current == null)
                {
                    continue;
                }

                float currentDelta = delta * current.TimeScale;
                if (current.Delay > 0f)
                {
                    current.Delay -= currentDelta;
                    if (current.Delay > 0f)
                    {
                        continue;
                    }
                    currentDelta = -current.Delay;
                    current.Delay = 0f;
                }

                current.TrackTime += currentDelta;
                FireCompletes(current);
                if (!ReferenceEquals(Tracks[i], current))
                {
                    continue; // a Complete handler replaced the entry
                }

                var next = current.Next;
                if (next != null)
                {
                    float overshoot = current.TrackTime - next.Delay;
                    if (overshoot >= 0f)
                    {
                        next.Delay = 0f;
                        next.TrackTime = current.TimeScale == 0f ? 0f : overshoot / current.TimeScale * next.TimeScale;
                        current.Next = null;
                        SetCurrent(i, next, interrupt: true);
                        FireCompletes(next);
                    }
                }
                else if (current.TrackTime >= current.TrackEnd)
                {
                    Tracks[i] = null;
                    OnEnd(current);
                    OnDispose(current);
                }
            }
        }

        /// <summary>Nothing to pose in the placeholder; events are raised from <see cref="Update"/>.</summary>
        public bool Apply(Skeleton skeleton) => true;

        public TrackEntry SetAnimation(int trackIndex, string animationName, bool loop)
        {
            var animation = Data.SkeletonData.FindAnimation(animationName);
            if (animation == null)
            {
                throw new ArgumentException("Animation not found: " + animationName, nameof(animationName));
            }
            return SetAnimation(trackIndex, animation, loop);
        }

        public TrackEntry SetAnimation(int trackIndex, Animation animation, bool loop)
        {
            if (animation == null)
            {
                throw new ArgumentNullException(nameof(animation));
            }

            var current = ExpandToIndex(trackIndex);
            if (current != null)
            {
                ClearNext(current);
            }

            var entry = new TrackEntry(trackIndex, animation, loop)
            {
                MixDuration = Data.GetMix(current?.Animation, animation),
            };
            SetCurrent(trackIndex, entry, interrupt: true);
            return entry;
        }

        public TrackEntry AddAnimation(int trackIndex, string animationName, bool loop, float delay)
        {
            var animation = Data.SkeletonData.FindAnimation(animationName);
            if (animation == null)
            {
                throw new ArgumentException("Animation not found: " + animationName, nameof(animationName));
            }
            return AddAnimation(trackIndex, animation, loop, delay);
        }

        public TrackEntry AddAnimation(int trackIndex, Animation animation, bool loop, float delay)
        {
            if (animation == null)
            {
                throw new ArgumentNullException(nameof(animation));
            }

            var last = ExpandToIndex(trackIndex);
            if (last != null)
            {
                while (last.Next != null)
                {
                    last = last.Next;
                }
            }

            var entry = new TrackEntry(trackIndex, animation, loop)
            {
                MixDuration = Data.GetMix(last?.Animation, animation),
            };

            if (last == null)
            {
                SetCurrent(trackIndex, entry, interrupt: true);
            }
            else
            {
                last.Next = entry;
                if (delay <= 0f)
                {
                    delay += last.TrackComplete;
                }
            }

            entry.Delay = delay;
            return entry;
        }

        public TrackEntry SetEmptyAnimation(int trackIndex, float mixDuration)
        {
            var entry = SetAnimation(trackIndex, EmptyAnimation, false);
            entry.MixDuration = mixDuration;
            entry.TrackEnd = mixDuration;
            return entry;
        }

        public TrackEntry AddEmptyAnimation(int trackIndex, float mixDuration, float delay)
        {
            if (delay <= 0f)
            {
                delay -= mixDuration;
            }
            var entry = AddAnimation(trackIndex, EmptyAnimation, false, delay);
            entry.MixDuration = mixDuration;
            entry.TrackEnd = mixDuration;
            return entry;
        }

        public void SetEmptyAnimations(float mixDuration)
        {
            for (int i = 0; i < Tracks.Count; i++)
            {
                if (Tracks[i] != null)
                {
                    SetEmptyAnimation(i, mixDuration);
                }
            }
        }

        public void ClearTrack(int trackIndex)
        {
            if (trackIndex >= Tracks.Count)
            {
                return;
            }

            var current = Tracks[trackIndex];
            if (current == null)
            {
                return;
            }

            ClearNext(current);
            Tracks[trackIndex] = null;
            OnEnd(current);
            OnDispose(current);
        }

        public void ClearTracks()
        {
            for (int i = 0; i < Tracks.Count; i++)
            {
                ClearTrack(i);
            }
        }

        /// <summary>Drops everything queued after <paramref name="entry"/>.</summary>
        public void ClearNext(TrackEntry entry)
        {
            var next = entry.Next;
            while (next != null)
            {
                OnDispose(next);
                next = next.Next;
            }
            entry.Next = null;
        }

        public TrackEntry? GetCurrent(int trackIndex)
        {
            return trackIndex < Tracks.Count ? Tracks[trackIndex] : null;
        }

        private TrackEntry? ExpandToIndex(int index)
        {
            while (Tracks.Count <= index)
            {
                Tracks.Add(null);
            }
            return Tracks[index];
        }

        private void SetCurrent(int index, TrackEntry entry, bool interrupt)
        {
            var from = Tracks[index];
            Tracks[index] = entry;
            if (from != null)
            {
                if (interrupt)
                {
                    OnInterrupt(from);
                }
                OnEnd(from);
                OnDispose(from);
            }
            OnStart(entry);
        }

        private void FireCompletes(TrackEntry entry)
        {
            float duration = entry.AnimationEnd - entry.AnimationStart;
            if (entry.Loop)
            {
                if (duration <= 0f)
                {
                    OnComplete(entry);
                    return;
                }

                int loops = (int)(entry.TrackTime / duration);
                while (entry.CompletedLoops < loops)
                {
                    entry.CompletedLoops++;
                    OnComplete(entry);
                }
            }
            else if (!entry.CompleteFired && entry.TrackTime >= duration)
            {
                entry.CompleteFired = true;
                OnComplete(entry);
            }
        }

        private void OnStart(TrackEntry entry)
        {
            entry.OnStart();
            Start?.Invoke(entry);
        }

        private void OnInterrupt(TrackEntry entry)
        {
            entry.OnInterrupt();
            Interrupt?.Invoke(entry);
        }

        private void OnEnd(TrackEntry entry)
        {
            entry.OnEnd();
            End?.Invoke(entry);
        }

        private void OnDispose(TrackEntry entry)
        {
            entry.OnDispose();
            Dispose?.Invoke(entry);
        }

        private void OnComplete(TrackEntry entry)
        {
            entry.OnComplete();
            Complete?.Invoke(entry);
        }
    }
}
