using Shared.StaticDatas;
using Z.GameClients.Stages.AreaEffectObjects;
using Z.GameClients.Stages.Characters.Stats;
using Z.GameClients.Stages.CombatSystems;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.Characters.PCs.Skills
{
    public class DefensiveFieldSkill : SkillBase
    {
        private float _damageRate;
        private float _attackRadius;
        private float _attackSpeed;
        private float _knockBackPower;
        private DefensiveFieldAreaEffectObject _areaEffectObject;

        private IReadOnlyCharacterStatCalculators _characterStats;

        private float _playSoundAt;
        private static readonly float SOUND_PLAY_PERIOD = 4.6f;

        public DefensiveFieldSkill(SkillStaticData staticData, IReadOnlyCharacterStatCalculators characterStats) : base(staticData)
        {
            _damageRate = staticData.Parameter1;
            _attackRadius = staticData.Parameter2;
            _attackSpeed = staticData.Parameter3;
            _knockBackPower = staticData.Parameter4;

            _characterStats = characterStats;
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);
            float damage = CombatSystem.CalculateSkillAttackDamage(owner.Stats, _damageRate);
            float radius = _attackRadius * owner.Stats.AttackRangeDistanceRatio.Value;
            float attackInterval = 1.0f / (_attackSpeed * _characterStats.SkillAttackSpeedValue);
            float knockbackPower = CombatSystem.CalculateSkillAttackKnockBackPower(owner.Stats, _knockBackPower);
            _areaEffectObject =
                stage.CreateDefensiveFieldAreaEffectObject(owner, damage, radius, attackInterval, knockbackPower, IsTranscendent, StaticData.SkillHitSFXPath);

            UnityGlobal.Sounds.PlayBySoundPrefab(StaticData.SkillSFXPath, owner.Pos);
            _playSoundAt = now + SOUND_PLAY_PERIOD;
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            if (_playSoundAt <= now)
            {
                _playSoundAt = now + SOUND_PLAY_PERIOD;
                UnityGlobal.Sounds.PlayBySoundPrefab(StaticData.SkillSFXPath, owner.Pos);
            }
        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);
            _areaEffectObject.Dead();
            _areaEffectObject = null;
        }

        public override void ReApplyStat(PlayerCharacterStatCalculators ownerStats)
        {
            base.ReApplyStat(ownerStats);

            float damage = CombatSystem.CalculateSkillAttackDamage(ownerStats, _damageRate);
            float radius = _attackRadius * ownerStats.AttackRangeDistanceRatio.Value;
            float attackInterval = 1.0f / (_attackSpeed * _characterStats.SkillAttackSpeedValue);
            float knockbackPower = CombatSystem.CalculateSkillAttackKnockBackPower(ownerStats, _knockBackPower);

            if (_areaEffectObject != null)
            {
                _areaEffectObject.ReApplyStat(damage, radius, attackInterval, knockbackPower);
            }
        }
    }
}