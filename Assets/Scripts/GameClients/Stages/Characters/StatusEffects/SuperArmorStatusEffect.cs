using UnityEngine;
using SamMul.GameClients.Stages.Particles;

namespace SamMul.GameClients.Stages.Characters.StatusEffects
{
    public class SuperArmorStatusEffect : StatusEffect
    {
        private ParticleObject _ownerEffect;

        private const string TARGET_EFFECT_PATH = "Stages/ETCEffects/fx_ServantAura.prefab";

        public SuperArmorStatusEffect(float duration) : base(StatusEffectType.SuperArmor, duration)
        {
            
        }

        public override void Begin(Stage stage, Character owner, float now)
        {
            base.Begin(stage, owner, now);

            owner.SetImmuneToHit();
            owner.SetImmuneToKnockBack();

            _ownerEffect = stage.Particles.CreateParticle(TARGET_EFFECT_PATH, owner.Pos, Vector3.one);
            _ownerEffect.transform.SetParent(owner.transform);
            _ownerEffect.transform.localPosition = Vector3.zero;
        }

        public override void Cancel(Character owner, Stage stage)
        {
            owner.UnsetImmuneToHit();
            owner.UnsetImmuneToKnockBack();

            stage.Particles.RemoveParticle(_ownerEffect);
            _ownerEffect = null;
        }

        public override void End(Stage stage, Character owner, float now)
        {
            owner.UnsetImmuneToHit();
            owner.UnsetImmuneToKnockBack();
            stage.Particles.RemoveParticle(_ownerEffect);
            _ownerEffect = null;
        }

        public override void Update(Stage stage, Character owner, float now)
        {
        }
    }
}
