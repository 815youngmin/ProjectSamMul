using Z.GameClients.Stages.Characters.GroundEffects;
using Z.GameClients.Stages.Characters.Stats;

namespace Z.GameClients.Stages.Characters.StatusEffects
{
    public class IncreaseAttackSpeedStatusEffect : StatusEffect
    {
        private StatModifier _attackSpeedIncreaser;

        public IncreaseAttackSpeedStatusEffect(float increment, float duration) : base(StatusEffectType.IncreaseAttackSpeed, duration)
        {
            _attackSpeedIncreaser = new StatModifier(increment, StatModType.PercentAdd);
        }
        public override void Begin(Stage stage, Character owner, float now)
        {
            base.Begin(stage, owner, now);
            owner.Stats.CharacterAttackSpeed.AddModifier(_attackSpeedIncreaser);
            owner.AddInfiniteGroundEffect(CharacterGroundEffectType.StimulationPackTarget);
        }

        public override void Cancel(Character owner, Stage stage)
        {
            owner.Stats.CharacterAttackSpeed.RemoveModifier(_attackSpeedIncreaser);
            owner.RemoveInfiniteGroundEffect(CharacterGroundEffectType.StimulationPackTarget);
        }

        public override void End(Stage stage, Character owner, float now)
        {
            owner.Stats.CharacterAttackSpeed.RemoveModifier(_attackSpeedIncreaser);
            owner.RemoveInfiniteGroundEffect(CharacterGroundEffectType.StimulationPackTarget);
        }

        public override void Update(Stage stage, Character owner, float now)
        {
        }

        
    }
}
