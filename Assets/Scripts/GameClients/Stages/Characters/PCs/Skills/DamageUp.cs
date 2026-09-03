using Shared.StaticDatas;
using Z.GameClients.Stages.Characters.Stats;

namespace Z.GameClients.Stages.Characters.PCs.Skills
{
    public class DamageUp : SkillBase
    {
        private StatModifier _multiplyAttackPower;
        private StatModifier _multiplyReceivedDamageIncrease;
        public DamageUp(SkillStaticData staticData) : base(staticData)
        {
            _multiplyAttackPower = new StatModifier(staticData.Parameter1, StatModType.PercentAdd);
            _multiplyReceivedDamageIncrease = new StatModifier(staticData.Parameter2, StatModType.Flat);
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);
            owner.Stats.AttackPower.AddModifier(_multiplyAttackPower);
            owner.Stats.ReceivedDamageIncrease.AddModifier(_multiplyReceivedDamageIncrease);
            owner.ReApplyStatsToAcquiredSkills(stage, owner.Stats);
        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);
            owner.Stats.AttackPower.RemoveModifier(_multiplyAttackPower);
            owner.Stats.ReceivedDamageIncrease.RemoveModifier(_multiplyReceivedDamageIncrease);
            owner.ReApplyStatsToAcquiredSkills(stage, owner.Stats);
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
        }
    }
}

