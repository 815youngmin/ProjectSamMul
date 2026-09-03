using UnityEngine;
using SamMul.GameClients.Stages.Characters.Stats;

namespace SamMul.GameClients.Stages.Characters.StatusEffects
{
    public class FreezeStatusEffect : StatusEffect
    {
        private StatModifier moveSpeedStatModifier;
        private float beginDamage;

        public FreezeStatusEffect(float duration, float minusMoveSpeedPercentValue, float beginDamage) : base(StatusEffectType.Freeze, duration)
        {
            moveSpeedStatModifier = new StatModifier(-minusMoveSpeedPercentValue, StatModType.PercentAdd);
            this.beginDamage = beginDamage;
        }
        public override void Begin(Stage stage, Character owner, float now)
        {
            base.Begin(stage, owner, now);
            owner.Stats.MoveSpeed.AddModifier(moveSpeedStatModifier);
            owner.Hitted(stage, attacker: null, beginDamage, Vector2.zero, owner.UIPos, hitSoundPrefabPath: string.Empty);
        }
        public override void End(Stage stage, Character owner, float now)
        {
            owner.Stats.MoveSpeed.RemoveModifier(moveSpeedStatModifier);
        }

        public override void Update(Stage stage, Character owner, float now)
        {
        }

        public override void Cancel(Character owner, Stage stage)
        {
            
        }
    }

}
