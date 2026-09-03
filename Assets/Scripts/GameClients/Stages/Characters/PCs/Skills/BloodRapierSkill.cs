using Shared.StaticDatas;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Stats;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.GameClients.Stages.ItemObjects;

namespace SamMul.GameClients.Stages.Characters.PCs.Skills
{
    public class BloodRapierSkill : SkillBase
    {
        private readonly float _attackPowerRate;            // Param1 : 일반 공격 대미지 증가 비율 (피해 계수), 최종대미지 = PC공격력 * (1+Param1)
        private StatModifier _attackSpeedIncreaser;         // Param2 : 공격속도 
        private readonly float _attackRange;                // Param3 : 공격범위 반경
        private readonly float _transcendentAttackPeriod;           // Param4 : 초월 공격 시간 간격
        private readonly float _transcendentAttackDamageRatio;      // param5 : 초월 공격 데미지 비율 
        private readonly float _hpDrainRatio;                       // param6 : 초월 공격 회복 비율

        private readonly float _weekKnockBackPower = 0.5f;
        private readonly float _mainKnockBackPower = 1.5f;
        private readonly float _transcendentKnockBackPower = 1.5f;
        private readonly float _transcendentAttackInterval = 0.5f;      // 첫 번재 초월 공격과 두 번째 초월 공격 사이의 시간 간격

        private readonly Vector2 _transcendentAttackDirection1 = new Vector2(1.0f, 1.0f).normalized;
        private readonly Vector2 _transcendentAttackDirection2 = new Vector2(-1.0f, 1.0f).normalized;
        private readonly Vector2 _transcendentAttackDirection3 = new Vector2(-1.0f, -1.0f).normalized;
        private readonly Vector2 _transcendentAttackDirection4 = new Vector2(1.0f, -1.0f).normalized;

        private float _lastAttackedAt;
        private float _lastTranscendentAttackedAt;
        private bool _isFirstTranscendentAttackTime;
        private IReadOnlyCharacterStatCalculators _characterStats;

        //RecoverHpWhenAttackByRedUmbrella 등급 효과
        private readonly float _gradeEffectHPDrainPercent;
        private readonly float _gradeEffectHPDrainAmountPercent;


        public BloodRapierSkill(SkillStaticData staticData, IReadOnlyCharacterStatCalculators characterStats, IReadOnlyCustomParameters parameters) : base(staticData)
        {
            _attackPowerRate = StaticData.Parameter1;
            _attackSpeedIncreaser = new StatModifier(StaticData.Parameter2, StatModType.Flat);
            _attackRange = StaticData.Parameter3;
            _transcendentAttackPeriod = StaticData.Parameter4;
            _transcendentAttackDamageRatio = StaticData.Parameter5;
            _hpDrainRatio = StaticData.Parameter6;

            _characterStats = characterStats;
            _lastAttackedAt = 0.0f;
            _lastTranscendentAttackedAt = 0.0f;
            _isFirstTranscendentAttackTime = true;

            _transcendentAttackPeriod *= 1.0f - parameters.GetParameterValue(CustomParameterType.HalfOffSaleKetchup_TranscendentAttackCountDecreasePercent);
            _gradeEffectHPDrainPercent = parameters.GetParameterValue(CustomParameterType.RecoverHpWhenAttackByRedUmbrella_HPDrainPercent);
            _gradeEffectHPDrainAmountPercent = parameters.GetParameterValue(CustomParameterType.RecoverHpWhenAttackByRedUmbrella_HPDrainAmountPercent);
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);
            owner.Stats.CharacterAttackSpeed.AddModifier(_attackSpeedIncreaser);
        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);
            owner.Stats.CharacterAttackSpeed.RemoveModifier(_attackSpeedIncreaser);
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            if (owner.Action.IsDead)
            {
                return;
            }

            this.MakeNormalAttack(owner, stage, now);
            this.MakeTranscendentAttack(owner, stage, now);
        }

        private void MakeNormalAttack(PlayerCharacter owner, Stage stage, float now)
        {
            float rangeAttackPeriod = 1.0f / _characterStats.CharacterAttackSpeedValue;
            if (now < _lastAttackedAt + rangeAttackPeriod)
            {
                return;
            }

            // 범위 증가 스킬로 공격 거리는 늘려준다. 각도는 늘리지 않는다.
            float resultAttackRange = owner.Stats.AttackRangeDistanceRatio.Value * 5f;
            // 0.65f 만큼 들어오면 공격한다. 범위형 공격이기 때문이다.
            var target = owner.FindBasicAttackTarget(stage, resultAttackRange - 0.65f);
            var damage = CombatSystem.CalculateCharacterBasicAttackDamage(owner.Stats, _attackPowerRate);
            float weekKnockbackPower = CombatSystem.CalculateCharacterAttackKnockBackPower(owner.Stats, _weekKnockBackPower);
            float mainKnockbackPower = CombatSystem.CalculateCharacterAttackKnockBackPower(owner.Stats, _mainKnockBackPower);

            Vector2 attackDirection;
            //몬스터 타겟이 존재하지 않으면 아이템을 타겟으로 잡는다.
            if (target == null)
            {
                BreakableItemObject item;

                item = owner.FindClosestBreakableItemObjectExceptFence(stage, _attackRange);
                if (item == null)
                {
                    return;
                }
                attackDirection = (Vector2)item.transform.position - owner.Pos;
            }
            else
            {
                attackDirection = target.Pos - owner.Pos;
            }

            attackDirection.Normalize();

            PlaySkillSoundEffect(owner.Pos);
            stage.CreateBloodRapierAreaEffectObject(
                owner,
                attackDirection,
                resultAttackRange,
                weekKnockbackPower,
                mainKnockbackPower,
                weekDamage: damage * 0.2f,  //우산이 활짝 펴지기 전 찌르기 데미지
                mainDamage: damage,
                areaRatio: owner.Stats.AttackRangeDistanceRatio.Value,
                hpDrainPercent: _gradeEffectHPDrainPercent,
                hpDrainAmount: _gradeEffectHPDrainAmountPercent,
                hitSoundPrefabPath: string.Empty
                );
            owner.ConditionalEffects.OnBasicSkillUsed(stage, owner);
            _lastAttackedAt = now;
        }

        private void MakeTranscendentAttack(PlayerCharacter owner, Stage stage, float now)
        {
            if (!IsTranscendent)
            {
                return;
            }

            var damage = CombatSystem.CalculateCharacterBasicAttackDamage(owner.Stats, _attackPowerRate);

            if (_isFirstTranscendentAttackTime)
            {
                float transcendentAttackPeriod = _transcendentAttackPeriod / _characterStats.CharacterAttackSpeedValue;
                if (now < _lastTranscendentAttackedAt + transcendentAttackPeriod)
                {
                    return;
                }

                this.PlaySpawnedObjectSoundEffect(owner.Pos);

                float transcendentDamage = damage * _transcendentAttackDamageRatio;
                float transcendentKnockbackPower = CombatSystem.CalculateCharacterAttackKnockBackPower(owner.Stats, _transcendentKnockBackPower);

                stage.CreateBloodRapierTranscendentAreaEffectObject(owner, owner.CenterPos, Vector2.up, transcendentDamage, _hpDrainRatio, transcendentKnockbackPower, StaticData.SkillHitSFXPath);
                stage.CreateBloodRapierTranscendentAreaEffectObject(owner, owner.CenterPos, Vector2.down, transcendentDamage, _hpDrainRatio, transcendentKnockbackPower, StaticData.SkillHitSFXPath);
                stage.CreateBloodRapierTranscendentAreaEffectObject(owner, owner.CenterPos, Vector2.left, transcendentDamage, _hpDrainRatio, transcendentKnockbackPower, StaticData.SkillHitSFXPath);
                stage.CreateBloodRapierTranscendentAreaEffectObject(owner, owner.CenterPos, Vector2.right, transcendentDamage, _hpDrainRatio, transcendentKnockbackPower, StaticData.SkillHitSFXPath);

                _lastTranscendentAttackedAt = now;
                _isFirstTranscendentAttackTime = false;
            }
            else
            {
                if (now < _lastTranscendentAttackedAt + _transcendentAttackInterval)
                {
                    return;
                }

                this.PlaySpawnedObjectSoundEffect(owner.Pos);

                float transcendentDamage = damage * _transcendentAttackDamageRatio;
                float transcendentKnockbackPower = CombatSystem.CalculateCharacterAttackKnockBackPower(owner.Stats, _transcendentKnockBackPower);

                stage.CreateBloodRapierTranscendentAreaEffectObject(owner, owner.CenterPos, _transcendentAttackDirection1, transcendentDamage, _hpDrainRatio, transcendentKnockbackPower, StaticData.SkillHitSFXPath);
                stage.CreateBloodRapierTranscendentAreaEffectObject(owner, owner.CenterPos, _transcendentAttackDirection2, transcendentDamage, _hpDrainRatio, transcendentKnockbackPower, StaticData.SkillHitSFXPath);
                stage.CreateBloodRapierTranscendentAreaEffectObject(owner, owner.CenterPos, _transcendentAttackDirection3, transcendentDamage, _hpDrainRatio, transcendentKnockbackPower, StaticData.SkillHitSFXPath);
                stage.CreateBloodRapierTranscendentAreaEffectObject(owner, owner.CenterPos, _transcendentAttackDirection4, transcendentDamage, _hpDrainRatio, transcendentKnockbackPower, StaticData.SkillHitSFXPath);

                _lastTranscendentAttackedAt = now;
                _isFirstTranscendentAttackTime = true;
            }
        }
    }
}

