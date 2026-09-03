using Shared.StaticDatas;
using UnityEngine;
using SamMul.GameClients.Stages.AreaEffectObjects;
using SamMul.GameClients.Stages.Characters.Stats;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.Characters.PCs.Skills
{
    public class RuneTrapSkill : SkillBase
    {
        private float _mineInterval;
        private float _installRangeRatio;

        private float _damageRate;
        private int _trapCount;
        private float _boomRadius;
        private float _trapLifeDuration;
        private float _knockBackPower;

        private float _nextMineAt;
        private int _installableCount;

        private readonly IReadOnlyCharacterStatCalculators _characterStats;

        public override float Duration => base.Duration / _characterStats.SkillAttackSpeedValue;
        public override float Cooltime => base.Cooltime / _characterStats.SkillAttackSpeedValue;

        public RuneTrapSkill(SkillStaticData staticData, IReadOnlyCharacterStatCalculators characterStats) : base(staticData)
        {
            _mineInterval = 1f;
            _installRangeRatio = 1.5f;

            _damageRate = staticData.Parameter1;
            _trapCount = (int)staticData.Parameter2;
            _boomRadius = staticData.Parameter3;
            _trapLifeDuration = staticData.Parameter4;
            _knockBackPower = staticData.Parameter5;

            _characterStats = characterStats;
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);

            _mineInterval = Duration / _trapCount;
            _nextMineAt = 0.0f;
            _installableCount = _trapCount;
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            if(_nextMineAt <= now && 0 < _installableCount)
            {
                InstallMine(stage, owner);
                _nextMineAt = now + _mineInterval;
            }
        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);
            // NOTE: Deactivate시 남은 지뢰를 다 깔아준다.
            for(; 0 < _installableCount ;)
            {
                this.InstallMine(stage, owner);
            }
        }

        private void InstallMine(Stage stage, PlayerCharacter owner)
        {
            float damage = CombatSystem.CalculateSkillAttackDamage(owner.Stats, _damageRate);
            float radius = _boomRadius * owner.Stats.AttackRangeDistanceRatio.Value;
            float trapLifeDuration = _trapLifeDuration * _characterStats.DurationIncreaseRateValue;
            float knockbackPower = CombatSystem.CalculateSkillAttackKnockBackPower(owner.Stats, _knockBackPower);

            float randomRange = radius * _installRangeRatio;
            var randomOffset = Random.Range(0, 2) % 2 == 0
                ? new Vector2(Random.Range(-randomRange, randomRange), 0f)
                : new Vector2(0f, Random.Range(-randomRange, randomRange));
            Vector2 installPosition = owner.Pos + randomOffset;
            PlaySkillSoundEffect(installPosition);
            if (this.IsTranscendent)
            {
                stage.CreateRuneTrapBombTranscendentAreaEffectObject(stage, owner, installPosition, damage, radius, trapLifeDuration, knockbackPower, StaticData.SkillHitSFXPath);
            }
            else
            {
                RuneTrapBombAreaEffectObject areaEffectObject = 
                                    stage.CreateRuneTrapBombAreaEffectObject(owner, damage, radius, trapLifeDuration, knockbackPower, StaticData.SkillHitSFXPath);
                areaEffectObject.transform.position = installPosition;
            }
            --_installableCount;
        }
    }
}
