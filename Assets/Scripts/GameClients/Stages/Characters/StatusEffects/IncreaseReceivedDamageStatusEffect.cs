using UnityEngine;
using SamMul.GameClients.Stages.Characters.Stats;
using SamMul.GameClients.Stages.Particles;

namespace SamMul.GameClients.Stages.Characters.StatusEffects
{
    public class IncreaseReceivedDamageStatusEffect : StatusEffect
    {
        private StatModifier _receivedDamageIncreaser;
        
        private ParticleObject _ownerEffect;

        private const string TARGET_EFFECT_PATH = "Stages/ETCEffects/fx_DEFDebuff.prefab";

        public IncreaseReceivedDamageStatusEffect(float increment, float duration) : base(StatusEffectType.IncreaseReceivedDamage, duration)
        {
            _receivedDamageIncreaser = new StatModifier(increment, StatModType.Flat);
        }
        public override void Begin(Stage stage, Character owner, float now)
        {
            base.Begin(stage, owner, now);
            owner.Stats.ReceivedDamageIncrease.AddModifier(_receivedDamageIncreaser);

            _ownerEffect = stage.Particles.CreateParticle(TARGET_EFFECT_PATH, owner.Pos, Vector3.one);
            _ownerEffect.transform.SetParent(owner.transform);
            _ownerEffect.transform.localPosition = Vector3.zero;
        }

        public override void Cancel(Character owner, Stage stage)
        {
            stage.Particles.RemoveParticle(_ownerEffect);
        }

        public override void End(Stage stage, Character owner, float now)
        {
            owner.Stats.ReceivedDamageIncrease.RemoveModifier(_receivedDamageIncreaser);

            stage.Particles.RemoveParticle(_ownerEffect);
        }

        public override void Update(Stage stage, Character owner, float now)
        {
        }
    }
}
