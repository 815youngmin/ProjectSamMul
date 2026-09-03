using Shared.StaticDatas;
using UnityEngine;
using Z.GameClients.Stages.Characters.Stats;
using Z.GameClients.Stages.CombatSystems;

namespace Z.GameClients.Stages.Characters.PCs.Skills
{
    public class MeteorSkill : SkillBase
    {

        private readonly float _meteorAttackPowerRate;  // 미티어 공격 계수.
        private readonly int _meteorCount;              // 미티어 개수.
        private readonly float _meteorAttackRadius;     // 미티어 공격 반지름.
        private readonly float _areaEffectLifetime;     // 장판 지속 시간.
        private readonly float _transcendentFirstRadiusRate;    // 초월 첫 미티어 추가 반지름 계수.
        private readonly float _meteorDropRadius;       //미티어 떨어지는 위치 탐색 범위

        private readonly IReadOnlyCharacterStatCalculators _characterStats;

        private int _remainingMeteorCount;              // 남은 미티어 개수.
        private float _attackPeriod;                    // 공격 주기.
        private float _attackAt;                        // 공격할 시각.

        public override float Duration => base.Duration / _characterStats.SkillAttackSpeedValue;
        public override float Cooltime => base.Cooltime / _characterStats.SkillAttackSpeedValue;

        public MeteorSkill(SkillStaticData staticData, IReadOnlyCharacterStatCalculators characterStats) : base(staticData)
        {
            _meteorAttackPowerRate = staticData.Parameter1;
            _meteorCount = (int)staticData.Parameter2;
            _meteorAttackRadius = staticData.Parameter3;
            _areaEffectLifetime = staticData.Parameter4;
            _transcendentFirstRadiusRate = staticData.Parameter5;
            _meteorDropRadius = staticData.Parameter6;

            _characterStats = characterStats;
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);
            _remainingMeteorCount = _meteorCount;
            _attackPeriod = Duration / _meteorCount;
            _attackAt = now;
        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            if (_remainingMeteorCount <= 0)
            {
                return;
            }

            if (now < _attackAt)
            {
                return;
            }

            _attackAt += _attackPeriod;
            this.DropMeteor(owner, stage);
        }

        public void DropMeteor(PlayerCharacter owner, Stage stage)
        {
            var position = owner.Pos + _meteorDropRadius * Random.insideUnitCircle;
            stage.CreateMeteorAreaEffectObject(
                owner: owner,
                dropPosition: position,
                attackDamage: CombatSystem.CalculateSkillAttackDamage(owner.Stats, _meteorAttackPowerRate),
                attackRadius: ((IsTranscendent && _remainingMeteorCount == _meteorCount) ? _transcendentFirstRadiusRate : 1.0f) *
                    owner.Stats.AttackRangeDistanceRatio.Value * _meteorAttackRadius,
                knockbackPower: CombatSystem.CalculateSkillAttackKnockBackPower(owner.Stats, 1.0f),
                areaEffectLifetime: owner.Stats.DurationIncreaseRate.Value * _areaEffectLifetime,
                isTranscendent: IsTranscendent);
            base.PlaySkillSoundEffect(position);
            --_remainingMeteorCount;
        }
    }
}
