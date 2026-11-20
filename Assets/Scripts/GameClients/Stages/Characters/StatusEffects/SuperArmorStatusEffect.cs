using UnityEngine;
using SamMul.GameClients.Stages.Particles;

namespace SamMul.GameClients.Stages.Characters.StatusEffects
{
    public class SuperArmorStatusEffect : StatusEffect
    {

        public SuperArmorStatusEffect(float duration) : base(StatusEffectType.SuperArmor, duration)
        {
            
        }

        public override void Begin(Stage stage, Character owner, float now)
        {
            base.Begin(stage, owner, now);

            owner.SetImmuneToHit();
            owner.SetImmuneToKnockBack();
        }

        public override void Cancel(Character owner, Stage stage)
        {
            owner.UnsetImmuneToHit();
            owner.UnsetImmuneToKnockBack();

        }

        public override void End(Stage stage, Character owner, float now)
        {
            owner.UnsetImmuneToHit();
            owner.UnsetImmuneToKnockBack();

        }

        public override void Update(Stage stage, Character owner, float now)
        {
        }
    }
}
