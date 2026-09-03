using Shared.StaticDatas;
using SamMul.GameClients.Stages.Characters.Stats;

namespace SamMul.GameClients.Stages.Characters.PCs.Skills
{
    public class ChargeHPBufferOverTimeSkill : SkillBase
    {
        private readonly StatModifier _increaseHpBufferMaxHp;
        private readonly float _chargeHpBufferPercentage;
        private readonly float _chargeHpBufferTimeInterval;

        private float _chargeHpBufferAt;

        public ChargeHPBufferOverTimeSkill(SkillStaticData staticData) : base(staticData)
        {
            _increaseHpBufferMaxHp = new StatModifier(0.01f * staticData.Parameter1, StatModType.PercentAdd);
            _chargeHpBufferPercentage = 0.01f * staticData.Parameter2;
            _chargeHpBufferTimeInterval = staticData.Parameter3;
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);

            if (owner.HPBuffer == null)
            {
                owner.CreateHPBuffer(0.0f);
            }

            float previousMaxHP = owner.HPBuffer.MaxHP.Value;
            owner.HPBuffer.MaxHP.AddModifier(_increaseHpBufferMaxHp);
            float currentMaxHP = owner.HPBuffer.MaxHP.Value;
            owner.HPBuffer.ChargeHP(currentMaxHP - previousMaxHP);

            _chargeHpBufferAt = now + _chargeHpBufferTimeInterval;
        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);

            if (owner.HPBuffer == null)
            {
                return;
            }

            float previousMaxHP = owner.HPBuffer.MaxHP.Value;
            owner.HPBuffer.MaxHP.RemoveModifier(_increaseHpBufferMaxHp);
            float currentMaxHP = owner.HPBuffer.MaxHP.Value;
            owner.HPBuffer.TakeDamage(previousMaxHP - currentMaxHP);
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            if (_chargeHpBufferAt < now)
            {
                _chargeHpBufferAt += _chargeHpBufferTimeInterval;
                owner.HPBuffer.ChargeHP(_chargeHpBufferPercentage * owner.HPBuffer.MaxHP.BaseValue);
            }
        }
    }
}
