using Shared.StaticDatas;
using Z.GameClients.Stages.Characters.Stats;

namespace Z.GameClients.Stages.Characters.PCs.Skills
{
    class ProjectileSpeedUpSkill : SkillBase
    {
        private StatModifier _projectileMoveSpeedIncreaseRate;

        public ProjectileSpeedUpSkill(SkillStaticData staticData) : base(staticData)
        {
            _projectileMoveSpeedIncreaseRate = new StatModifier(staticData.Parameter1, StatModType.PercentAdd);
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);

            owner.Stats.ProjectileMoveSpeedIncreaseRate.AddModifier(_projectileMoveSpeedIncreaseRate);
            owner.ReApplyStatsToAcquiredSkills(stage, owner.Stats);
        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);

            owner.Stats.ProjectileMoveSpeedIncreaseRate.RemoveModifier(_projectileMoveSpeedIncreaseRate);
            owner.ReApplyStatsToAcquiredSkills(stage, owner.Stats);
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            // DO NOTHING
        }
    }
}