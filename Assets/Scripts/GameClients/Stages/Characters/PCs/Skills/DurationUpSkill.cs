using Shared.StaticDatas;
using SamMul.GameClients.Stages.Characters.Stats;

namespace SamMul.GameClients.Stages.Characters.PCs.Skills
{
    class DurationUpSkill : SkillBase
    {
        private StatModifier _durationIncreaseRate;

        public DurationUpSkill(SkillStaticData staticData) : base(staticData)
        {
            _durationIncreaseRate = new StatModifier(staticData.Parameter1, StatModType.PercentAdd);
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);

            owner.Stats.DurationIncreaseRate.AddModifier(_durationIncreaseRate);
            owner.ReApplyStatsToAcquiredSkills(stage, owner.Stats);
        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);
            owner.Stats.DurationIncreaseRate.RemoveModifier(_durationIncreaseRate);
            owner.ReApplyStatsToAcquiredSkills(stage, owner.Stats);
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            // DO NOTHING
        }
    }
}
