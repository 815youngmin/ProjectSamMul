using Shared.StaticDatas;
using UnityEngine;
using Z.GameClients.Stages.Characters.Stats;
using Z.GameClients.Stages.CombatSystems;

namespace Z.GameClients.Stages.Characters.PCs.Skills
{
    public class LandWhaleSkill : SkillBase
    {
        private float _whaleCreatePeriod;   //고래 생성 간격

        private float _attackPowerRate; //고래 공격력
        private float _whaleRadius;     //고래 범위
        private int _whaleAmount;       //고래 개수
        private float _whaleDuration;   //고래 지속시간
        private float _whaleMoveSpeed;  //고래 이동속도
        private float _whaleCreateDistance; //고래 생성 거리
        private static readonly float WhaleKnockbackPower = 0.1f;

        private static readonly float FireAttackPowerRate = -0.5f;
        private static readonly float FireRadius = 1.5f;
        private static readonly float FireLifeTime = 2f;

        public override float Duration => base.Duration / _characterStats.SkillAttackSpeedValue;
        public override float Cooltime => base.Cooltime / _characterStats.SkillAttackSpeedValue;

        private IReadOnlyCharacterStatCalculators _characterStats;

        private float _whaleCreateCount;
        private float _whaleLastCreateAt;

        public LandWhaleSkill(SkillStaticData staticData, IReadOnlyCharacterStatCalculators characterStats) : base(staticData)
        {
            _attackPowerRate = staticData.Parameter1;
            _whaleRadius = staticData.Parameter2;
            _whaleAmount = (int)staticData.Parameter3;
            _whaleCreateDistance = staticData.Parameter4;
            _whaleDuration = staticData.Parameter5;
            _whaleMoveSpeed = staticData.Parameter6;

            _characterStats = characterStats;
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);
            _whaleCreatePeriod = Duration / _whaleAmount;
            _whaleCreateCount = 0;
            _whaleLastCreateAt = 0;
        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            if (_whaleCreateCount > _whaleAmount)
            {
                return;
            }

            if (now < _whaleLastCreateAt + _whaleCreatePeriod)
            {
                return;
            }
            _whaleLastCreateAt = now;
            _whaleCreateCount++;
            PlaySkillSoundEffect(owner.Pos);
            this.CreateWhale(owner, stage);
        }

        private void CreateWhale(PlayerCharacter owner, Stage stage) 
        {
            Vector2 rightTop = new Vector2(0.7660f, 0.6428f);   //40도 정규화 벡터
            Vector2 leftBottom = new Vector2(-0.7660f, -0.6428f);

            float damage = CombatSystem.CalculateSkillAttackDamage(owner.Stats, _attackPowerRate);
            var attackRangeRatio = owner.Stats.AttackRangeDistanceRatio.Value;
            float objectRadius = _whaleRadius * attackRangeRatio;
            float objectCreateDistance = _whaleCreateDistance * attackRangeRatio;
            float movingSpeed = _whaleMoveSpeed * owner.Stats.ProjectileMoveSpeedIncreaseRate.Value;
            float lifeTime = _whaleDuration * _characterStats.DurationIncreaseRateValue;

            if(IsTranscendent)
            {
                float fireDamage = CombatSystem.CalculateSkillAttackDamage(owner.Stats, FireAttackPowerRate);
                float fireRadius = FireRadius * attackRangeRatio;
                float fireLifeTime = FireLifeTime * _characterStats.DurationIncreaseRateValue;
                float fireCreatePeriod = fireRadius * 2f / movingSpeed;
                stage.CreateLandWhaleTranscendentWhaleObject(
                    owner, damage, owner.CenterPos + rightTop * objectCreateDistance, Vector2.left, movingSpeed, WhaleKnockbackPower, objectRadius, lifeTime, null,
                    fireDamage, fireRadius, fireLifeTime, fireCreatePeriod);
                stage.CreateLandWhaleTranscendentWhaleObject(
                owner, damage, owner.CenterPos + leftBottom * objectCreateDistance, Vector2.right, movingSpeed, WhaleKnockbackPower, objectRadius, lifeTime, null,
                fireDamage, fireRadius, fireLifeTime, fireCreatePeriod);
            }
            else
            {
                stage.CreateLandWhaleBasicObject(
                    owner, damage, owner.CenterPos + rightTop * objectCreateDistance, Vector2.left, movingSpeed, WhaleKnockbackPower, objectRadius, lifeTime, hitSoundPrefabPath: null);
                stage.CreateLandWhaleBasicObject(
                    owner, damage, owner.CenterPos + leftBottom * objectCreateDistance, Vector2.right, movingSpeed, WhaleKnockbackPower, objectRadius, lifeTime, hitSoundPrefabPath: null);
            }
        }
    }
}

