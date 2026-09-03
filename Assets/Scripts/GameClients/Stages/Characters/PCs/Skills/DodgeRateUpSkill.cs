using Shared.StaticDatas;
using SamMul.GameClients.Stages.Characters.Stats;

namespace SamMul.GameClients.Stages.Characters.PCs.Skills
{
    public class DodgeRateUpSkill : SkillBase
    {
        private readonly StatModifier _increaseDodgeRate;

        public DodgeRateUpSkill(SkillStaticData staticData) : base(staticData)
        {
            _increaseDodgeRate = new StatModifier(0.01f * staticData.Parameter1, StatModType.Flat);
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);
            owner.Stats.DodgeRate.AddModifier(_increaseDodgeRate);
            owner.ReApplyStatsToAcquiredSkills(stage, owner.Stats);
        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);
            owner.Stats.DodgeRate.RemoveModifier(_increaseDodgeRate);
            owner.ReApplyStatsToAcquiredSkills(stage, owner.Stats);
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {

        }
    }
}
