using Z.GameClients.Stages.Characters.GroundEffects;
using Z.GameClients.Stages.Characters.Stats;
using Z.Scenes;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.Characters.StatusEffects
{
    public class IncreaseAttackDamageStatusEffect : StatusEffect
    {
        private StatModifier _attackDamageIncreaser;

        public IncreaseAttackDamageStatusEffect(float increment, float duration) : base(StatusEffectType.IncreaseAttackDamage, duration)
        {
            _attackDamageIncreaser = new StatModifier(increment, StatModType.PercentAdd);
        }
        public override void Begin(Stage stage, Character owner, float now)
        {
            base.Begin(stage, owner, now);
            owner.Stats.AttackPower.AddModifier(_attackDamageIncreaser);
            owner.AddInfiniteGroundEffect(CharacterGroundEffectType.ProteinSupplementTarget);

            if (stage.PC == owner)
            {
                var stageUIRoot = UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>();
                stageUIRoot.UpdatePCAttackPower((int)owner.Stats.AttackPower.Value);
            }
        }

        public override void Cancel(Character owner, Stage stage)
        {
            owner.Stats.AttackPower.RemoveModifier(_attackDamageIncreaser);
            owner.RemoveInfiniteGroundEffect(CharacterGroundEffectType.ProteinSupplementTarget);

            if (stage.PC == owner)
            {
                var stageUIRoot = UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>();
                stageUIRoot.UpdatePCAttackPower((int)owner.Stats.AttackPower.Value);
            }
        }

        public override void End(Stage stage, Character owner, float now)
        {
            owner.Stats.AttackPower.RemoveModifier(_attackDamageIncreaser);
            owner.RemoveInfiniteGroundEffect(CharacterGroundEffectType.ProteinSupplementTarget);

            if (stage.PC == owner)
            {
                var stageUIRoot = UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>();
                stageUIRoot.UpdatePCAttackPower((int)owner.Stats.AttackPower.Value);
            }
        }

        public override void Update(Stage stage, Character owner, float now)
        {
        }


    }
}
