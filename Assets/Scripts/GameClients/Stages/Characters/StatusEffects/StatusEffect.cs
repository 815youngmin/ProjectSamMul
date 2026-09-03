namespace Z.GameClients.Stages.Characters.StatusEffects
{
    public abstract class StatusEffect
    {
        public readonly StatusEffectType Type;
        public float Duration { get; private set; }
        public float StartedAt { get; private set; }
        public float EndAt => this.StartedAt + this.Duration;

        protected StatusEffect(StatusEffectType type, float duration)
        {
            this.Type = type;
            this.Duration = duration;
            this.StartedAt = 0f;
        }

        public virtual void Begin(Stage stage, Character owner, float now)
        {
            this.StartedAt = now;
        }
        public abstract void Update(Stage stage, Character owner, float now);
        public abstract void End(Stage stage, Character owner, float now);
        public abstract void Cancel(Character owner, Stage stage);

        public virtual void OwnerDead(Character owner, Stage stage) 
        { 
        }
    }

}
