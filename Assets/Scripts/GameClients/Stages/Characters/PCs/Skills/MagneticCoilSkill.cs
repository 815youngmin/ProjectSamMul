using Shared.StaticDatas;
using Z.GameClients.Stages.Characters.Stats;

namespace Z.GameClients.Stages.Characters.PCs.Skills
{
    public class AcquisitionDistanceUpSkill : SkillBase
    {
        private StatModifier _acquisitionDistanceIncreaseRate; // 획득 범위 증가량 (0~1, 백분율)

        public AcquisitionDistanceUpSkill(SkillStaticData staticData) : base(staticData)
        {
            _acquisitionDistanceIncreaseRate = new StatModifier(staticData.Parameter1, StatModType.PercentAdd);
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);

            owner.Stats.AcquisitionDistance.AddModifier(_acquisitionDistanceIncreaseRate);
        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);

            owner.Stats.AcquisitionDistance.RemoveModifier(_acquisitionDistanceIncreaseRate);
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            // DO NOTHING
        }

    }
}
