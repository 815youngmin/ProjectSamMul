using Shared.StaticDatas;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Stats;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.Characters.PCs.Skills
{
    public class ShockBombSkill : SkillBase
    {
        private readonly float _attackPowerRate;    //param1: 피해계수
        private readonly int _throwCount;           //param2: 발사개수
        private readonly float _attackRadius;       //param3: 공격범위
        private readonly float _knockbackPower;     //param4: 넉백수치
        private readonly float _duration;           //param5: 지속시간
        private readonly float _hitPeriod;          //param6: 타격주기 (공격 간격)

        private float _throwPeriod;             
        private readonly float _arrivalTime;

        private int _currentThrowCount;
        private float _lastThrowAt;
        private float _startAngleDegree;
        private float _playerAttackRangeDistanceRatio;

        private IReadOnlyCharacterStatCalculators _characterStats;

        public override float Duration => base.Duration / _characterStats.SkillAttackSpeedValue;
        public override float Cooltime => base.Cooltime / _characterStats.SkillAttackSpeedValue;


        public ShockBombSkill(SkillStaticData staticData, IReadOnlyCharacterStatCalculators characterStats) : base(staticData)
        {
            _attackPowerRate = staticData.Parameter1;
            _throwCount = (int)staticData.Parameter2;
            _attackRadius = staticData.Parameter3;
            _knockbackPower = staticData.Parameter4;
            _duration = staticData.Parameter5;
            _hitPeriod = staticData.Parameter6;

            _arrivalTime = 1.5f;

            _currentThrowCount = 0;
            _lastThrowAt = 0;

            _characterStats = characterStats;
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);
            _startAngleDegree = Random.Range(0, 360f);
            _playerAttackRangeDistanceRatio = ((PlayerCharacter)owner).Stats.AttackRangeDistanceRatio.Value;
            _currentThrowCount = 0;
            _throwPeriod = Duration / _throwCount;
        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);

            int ballRemainingCount = _throwCount - _currentThrowCount;
            if (0 < ballRemainingCount)
            {
                for (int i = 0; i < ballRemainingCount; i++)
                {
                    this.ThrowShockBomb(owner, stage);
                }
            }
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            if (_currentThrowCount > _throwCount)
            {
                return;
            }

            if (now < _lastThrowAt + _throwPeriod)
            {
                return;
            }

            _lastThrowAt = now;
            this.ThrowShockBomb(owner, stage);
        }

        private void ThrowShockBomb(PlayerCharacter owner, Stage stage)
        {
            int currentCreateIndex = _throwCount - _currentThrowCount;
            float angularDistanceToDegree = 360f / _throwCount;
            float radian = (_startAngleDegree + (angularDistanceToDegree * currentCreateIndex)) * Mathf.Deg2Rad;
            float destinationDistance = 4.5f * _playerAttackRangeDistanceRatio;
            Vector2 targetPosition = owner.CenterPos + new Vector2(destinationDistance * Mathf.Cos(radian), destinationDistance * Mathf.Sin(radian));

            float damage = CombatSystem.CalculateSkillAttackDamage(owner.Stats, _attackPowerRate);
            var attackRangeRatio = _attackRadius * owner.Stats.AttackRangeDistanceRatio.Value;
            float knockbackPower = CombatSystem.CalculateSkillAttackKnockBackPower(owner.Stats, _knockbackPower);
            float duration = _duration * _characterStats.DurationIncreaseRateValue;
            float hitPeriod = _hitPeriod / _characterStats.SkillAttackSpeedValue;

            this.PlaySkillSoundEffect(owner.Pos);
            stage.CreateShockBomb(
                owner, 
                targetPosition, 
                arrivalTime: _arrivalTime, 
                damage: damage, 
                radius: attackRangeRatio, 
                knockbackPower: knockbackPower, 
                duration: duration, 
                hitPeriod: hitPeriod,
                isTranscend: this.IsTranscendent, 
                hitSoundPrefabPath: StaticData.SkillHitSFXPath);
            _currentThrowCount++;
        }
    }

}

