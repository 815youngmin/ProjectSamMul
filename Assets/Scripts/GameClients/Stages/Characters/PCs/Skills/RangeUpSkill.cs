using Shared.StaticDatas;
using System.Diagnostics;
using Z.GameClients.Stages.Characters.Stats;

namespace Z.GameClients.Stages.Characters.PCs.Skills
{
    public class RangeUpSkill : SkillBase
    {
        private StatModifier _attackRangeDistanceIncreaser;
        public RangeUpSkill(SkillStaticData staticData) : base(staticData)
        {
            _attackRangeDistanceIncreaser = new StatModifier(staticData.Parameter1, StatModType.PercentAdd);
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);
            Debug.Assert(owner.GetType() == typeof(PlayerCharacter),"owner is not PlayerCharacter type, please check owner type");

            PlayerCharacter playerCharacter = (PlayerCharacter)owner;
            playerCharacter.Stats.AttackRangeDistanceRatio.AddModifier(_attackRangeDistanceIncreaser);

            // NOTE: 획득한 스킬들의 ReApplyStat을 호출한다.
            playerCharacter.ReApplyStatsToAcquiredSkills(stage, ownerStats: playerCharacter.Stats);
        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);
            Debug.Assert(owner.GetType() == typeof(PlayerCharacter), "owner is not PlayerCharacter type, please check owner type");

            PlayerCharacter playerCharacter = (PlayerCharacter)owner;
            playerCharacter.Stats.AttackRangeDistanceRatio.RemoveModifier(_attackRangeDistanceIncreaser);
            playerCharacter.ReApplyStatsToAcquiredSkills(stage, ownerStats: playerCharacter.Stats);
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            // DO NOTHING
        }
    }
}
