
using Shared.StaticDatas;
using Z.GameClients.Stages.Characters.Stats;

namespace Z.GameClients.Stages.Characters.PCs.Skills
{
    class ExpUpSkill : SkillBase
    {
        private StatModifier _expIncreaseRate;

        public ExpUpSkill(SkillStaticData staticData) : base(staticData)
        {
            _expIncreaseRate = new StatModifier(staticData.Parameter1, StatModType.Flat);
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);

            PlayerCharacter pc = (PlayerCharacter)owner;

            pc.Stats.ExpIncreaseRate.AddModifier(_expIncreaseRate);
        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);

            PlayerCharacter pc = (PlayerCharacter)owner;

            pc.Stats.ExpIncreaseRate.RemoveModifier(_expIncreaseRate);
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            // DO NOTHING
        }
    }
}
