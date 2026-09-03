using Shared.StaticDatas;

namespace SamMul.GameClients.Stages.Characters.PCs.Skills
{
    class HealOverTimeSkill : SkillBase
    {
        private readonly float _recoveryAmount;
        private readonly float _recoveryDuration;
        private float _recoveryAt;

        public HealOverTimeSkill(SkillStaticData staticData) : base(staticData)
        {
            _recoveryAmount = staticData.Parameter1;
            _recoveryDuration= staticData.Parameter2;
            _recoveryAt = 0.0f;
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);
        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            if (now < _recoveryAt)
            {
                return;
            }

            owner.RecoverHP(stage, owner.MaxHP * _recoveryAmount);
            _recoveryAt = now + _recoveryDuration;
        }
    }
}
