using Shared.StaticDatas;
using Z.GameClients.Stages.Characters.Stats;

namespace Z.GameClients.Stages.Characters.PCs.Skills
{
    class IncommingDamageDownSkill : SkillBase
    {
        private StatModifier _damageReduction;

        public IncommingDamageDownSkill(SkillStaticData staticData) : base(staticData)
        {
            _damageReduction = new StatModifier(staticData.Parameter1, StatModType.Flat);
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);

            owner.Stats.DamageReduction.AddModifier(_damageReduction);
        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);

            owner.Stats.DamageReduction.RemoveModifier(_damageReduction);
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            // DO NOTHING
        }
    }
}
