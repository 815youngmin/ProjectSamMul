using System;
using UnityEngine;

namespace SamMul.GameClients.Stages.Characters.Animations
{
    public enum CharacterBodyEffectType : int
    {
        Dead,
        Burn,
        Hitted,
    }

    public readonly struct CharacterBodyEffect
    {
        public readonly CharacterBodyEffectType Type;
        public readonly float CreatedAt;
        public readonly float EndAt;

        public readonly Action bodyEffectApplier;
        public readonly Action<float, float> progressiveBodyEffect;

        private CharacterBodyEffect(
            CharacterBodyEffectType type,
            float createdAt,
            float duration,
            Action bodyEffectApplier,
            Action<float, float> progressiveBodyEffect)
        {
            this.Type = type;
            this.CreatedAt = createdAt;
            this.EndAt = createdAt + duration;
            this.bodyEffectApplier = bodyEffectApplier;
            this.progressiveBodyEffect = progressiveBodyEffect;
        }

        public void Apply()
        {
            this.bodyEffectApplier();
        }

        public void Progress(float now)
        {
            if (this.progressiveBodyEffect == null)
            {
                return;
            }

            float leftTime = this.EndAt - now;
            float progressedTime = now - this.CreatedAt;
            this.progressiveBodyEffect(leftTime, progressedTime);
        }

        public static CharacterBodyEffect Hitted(float duration, Action bodyEffectApplier, Action<float, float> progressiveBodyEffect)
        {
            return new CharacterBodyEffect(CharacterBodyEffectType.Hitted, Time.time, duration, bodyEffectApplier,
                progressiveBodyEffect);
        }

        public static CharacterBodyEffect Burn(float duration, Action bodyEffectApplier)
        {
            return new CharacterBodyEffect(CharacterBodyEffectType.Burn, Time.time, duration, bodyEffectApplier, progressiveBodyEffect: null);
        }

        public static CharacterBodyEffect Dead(Action bodyEffectApplier, Action<float, float> progressiveBodyEffect)
        {
            return new CharacterBodyEffect(CharacterBodyEffectType.Dead, Time.time, 99999.9f, bodyEffectApplier, progressiveBodyEffect);
        }
    }
}
