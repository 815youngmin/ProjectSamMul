using Shared.StaticDatas;
using SamMul.GameClients.Stages.Characters.Stats;

namespace SamMul.GameClients.Stages.Characters.PCs.Skills
{
    class GoldAmountUpSkill : SkillBase
    {
        private StatModifier _goldIncreaseRate;

        public GoldAmountUpSkill(SkillStaticData staticData) : base(staticData)
        {
            _goldIncreaseRate = new StatModifier(staticData.Parameter1, StatModType.Flat);
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);

            PlayerCharacter pc = (PlayerCharacter)owner;

            pc.Stats.GoldIncreaseRate.AddModifier(_goldIncreaseRate);
        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);

            PlayerCharacter pc = (PlayerCharacter)owner;

            pc.Stats.GoldIncreaseRate.RemoveModifier(_goldIncreaseRate);
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            // DO NOTHING
        }
    }
}
