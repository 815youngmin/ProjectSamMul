using Shared.StaticDatas;
using SamMul.GameClients.Stages.Characters.Stats;

namespace SamMul.GameClients.Stages.Characters.PCs.Skills
{
    public class AttackSpeedUpSkill: SkillBase
    {
        // 공격속도 상승 (초당 발사횟수 이만큼 증가 시킴. 덧셈)
        private StatModifier _attackSpeedIncreaser;
        private StatModifier _skillSpeedIncreaser;

        public AttackSpeedUpSkill(SkillStaticData staticData) :base(staticData)
        {
            _attackSpeedIncreaser = new StatModifier(staticData.Parameter1, StatModType.Flat);
            _skillSpeedIncreaser = new StatModifier(staticData.Parameter1, StatModType.Flat);
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);

            owner.Stats.CharacterAttackSpeed.AddModifier(_attackSpeedIncreaser);
            owner.Stats.SkillAttackSpeed.AddModifier(_skillSpeedIncreaser);

            owner.ReApplyStatsToAcquiredSkills(stage, owner.Stats);
        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);

            owner.Stats.CharacterAttackSpeed.RemoveModifier(_attackSpeedIncreaser);
            owner.Stats.SkillAttackSpeed.RemoveModifier(_skillSpeedIncreaser);

            owner.ReApplyStatsToAcquiredSkills(stage, owner.Stats);
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            // DO NOTHING
        }
    }
}
