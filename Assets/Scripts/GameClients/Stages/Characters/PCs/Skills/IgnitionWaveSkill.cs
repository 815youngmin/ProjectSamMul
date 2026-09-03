using DG.Tweening;
using Shared.StaticDatas;
using UnityEngine;
using Z.GameClients.Stages.Characters.Stats;
using Z.GameClients.Stages.CombatSystems;
using Z.GameClients.Stages.ItemObjects;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.Characters.PCs.Skills
{
    public class IgnitionWaveSkill : SkillBase
    {
        private readonly float _attackPowerRate;         // Param1 : 일반 공격 대미지 증가 비율 (피해 계수), 최종대미지 = PC공격력 * (1+Param1)
        private StatModifier _attackSpeedIncreaser;     // Param2 : 공격속도 
        private readonly float _attackRange;           // Param3 : 공격범위 반경
        private readonly float _attackAngle;          // Param4 : 공격범위 각도
        private readonly float _knockBackPower;     // Param5 : 넉백 파워

        private IReadOnlyCharacterStatCalculators _characterStats;
        private float _lastAttackedAt;

        //IgnatiaSS 파라미터
        private float _gradeEffectStunDuration;

        //AddBurnEffectOnIgnitionWave 파라미터
        private float _gradeEffectBurnDuration;      //화상 지속 시간
        private float _gradeEffectBurnDamagePercent; //화상 데미지 퍼센트

        //변장 효과CreateMeteorIgnitionWaveCountDown
        private int _createMeteorStackCount;
        private int _createMeteorStack;
        //변장 효과 DoubleAttackIgnitionWaveCountDown
        private int _doubleAttackStackCount;
        private int _doubleAttackStack;
        private readonly string _meteorSkillSFXPath;
        //변장 효과 IncreaseIgnitionWaveDamage
        private readonly float _increaseIgnitionWaveDamagePercente;
        
        //화상 확산량
        private int _burnSpreadAmount;

        public IgnitionWaveSkill(SkillStaticData staticData, IReadOnlyCharacterStatCalculators characterStats, IReadOnlyCustomParameters customParameters) : base(staticData)
        {
            _attackPowerRate = StaticData.Parameter1;
            _attackSpeedIncreaser = new StatModifier(StaticData.Parameter2, StatModType.Flat);
            _attackRange = StaticData.Parameter3;
            _attackAngle = StaticData.Parameter4;
            _knockBackPower = StaticData.Parameter5;

            _gradeEffectStunDuration = customParameters.GetParameterValue(CustomParameterType.IgnatiaSS_StunDuration);
            _lastAttackedAt = 0.0f;
            _characterStats = characterStats;

            _gradeEffectBurnDuration = customParameters.GetParameterValue(CustomParameterType.AddBurnEffectOnIgnitionWave_BurnDuration);
            _gradeEffectBurnDamagePercent = customParameters.GetParameterValue(CustomParameterType.Ignatia_BurnDamagePercent);

            _createMeteorStackCount = (int)customParameters.GetParameterValue(CustomParameterType.CreateMeteorIgnitionWaveStackCount);
            _createMeteorStack = 0;

            _doubleAttackStackCount = (int)customParameters.GetParameterValue(CustomParameterType.DoubleAttackIgnitionWaveStackCount);
            _doubleAttackStack = 0;

            _meteorSkillSFXPath = StaticDataRepository.Instance.Skills.GetSkills(Shared.GameDataTypes.SkillId.Meteor)[0].SkillSFXPath;

            _increaseIgnitionWaveDamagePercente = customParameters.GetParameterValue(CustomParameterType.IncreaseIgnitionWaveDamagePercent);

            _burnSpreadAmount = (int)customParameters.GetParameterValue(CustomParameterType.IncreaseIgnitionWaveBurnSpreadAmount);
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

        //타겟이 없으면 쿨타임이 돌지 않는다.
        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            if (owner.Action.IsDead)
            {
                return;
            }

            //공격속도 영향 받도록 처리, //TODO. 추후 ReApplyStat을 이용해 저장해놓고 이용하도록 하게 하자.
            float rangeAttackPeriod = 1.0f / _characterStats.CharacterAttackSpeedValue;
            if (now < _lastAttackedAt + rangeAttackPeriod)
            {
                return;
            }

            // 범위 증가 스킬로 공격 거리는 늘려준다. 각도는 늘리지 않는다.
            float resultAttackRange = owner.Stats.AttackRangeDistanceRatio.Value * _attackRange;
            // 0.65f 만큼 들어오면 공격한다. 범위형 공격이기 때문이다.
            var target = owner.FindBasicAttackTarget(stage, resultAttackRange - 0.65f);
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

            float resultDamage = CombatSystem.CalculateCharacterBasicAttackDamage(owner.Stats, _attackPowerRate);
            if(_increaseIgnitionWaveDamagePercente > 0)
            {
                resultDamage = (1.0f + _increaseIgnitionWaveDamagePercente) * resultDamage;
            }            
            float resultBurnDamage = CombatSystem.CalculateBurnAttackDamage(owner.Stats, _gradeEffectBurnDamagePercent);
            float knockbackPower = CombatSystem.CalculateCharacterAttackKnockBackPower(owner.Stats, _knockBackPower);

            if (IsTranscendent)
            {
                float width = 5.0f * owner.Stats.AttackRangeDistanceRatio.Value;
                float attackRange = resultAttackRange; // rangeAttackRange는 범위 확장 스킬에 대한 처리는 되어있다.
                float moveSpeed = 15f * _characterStats.ProjectileMoveSpeedIncreaseRateValue;
                stage.CreateIgnitionWaveTranscendAreaEffectObject(owner, attackDirection, moveSpeed, width, attackRange, resultDamage, knockbackPower, resultBurnDamage, _gradeEffectBurnDuration, _burnSpreadAmount, _gradeEffectStunDuration, StaticData.SkillHitSFXPath);
            }
            else
            {
                stage.CreateIgnitionWaveAreaEffectObject(owner, attackDirection, resultDamage, resultAttackRange, _attackAngle, knockbackPower, resultBurnDamage, _gradeEffectBurnDuration, _burnSpreadAmount, StaticData.SkillHitSFXPath);
            }

            PlaySkillSoundEffect(owner.Pos);

            if(_createMeteorStackCount > 0)
            {
                _createMeteorStack++;
                if(_createMeteorStack >= _createMeteorStackCount)
                {
                    _createMeteorStack = 0;
                    float meteorDamage = CombatSystem.CalculateSkillAttackDamage(owner.Stats, _attackPowerRate * 0.3f);

                    int metemorDropAmount = 0;
                    switch (Level)
                    {
                        case 1: metemorDropAmount = 1; break;
                        case 2: metemorDropAmount = 2; break;
                        case 3: metemorDropAmount = 2; break;
                        case 4: metemorDropAmount = 2; break;
                        case 5: metemorDropAmount = 2; break;
                        case 6: metemorDropAmount = 3; break;
                        default: metemorDropAmount = 0; break;
                    }

                    for(int i = 0; i < metemorDropAmount; i++)
                    {
                        DOVirtual.DelayedCall(i * 0.1f, () =>
                        {
                            this.DropMeteor(owner, stage,
                                meteorDropRadius: 9f,
                                attackDamage: meteorDamage,
                                attackRadius: 2f,
                                areaEffectLifeTime: 3f);
                        });
                    }
                }
            }

            owner.ConditionalEffects.OnBasicSkillUsed(stage, owner);
            _lastAttackedAt = Time.time;

            if (_doubleAttackStackCount > 0)
            {
                _doubleAttackStack++;
                if (_doubleAttackStack >= _doubleAttackStackCount)
                {
                    //거의 바로 발동되도록 최종 공격시간을 조절해준다.
                    _lastAttackedAt = Time.time - rangeAttackPeriod + 0.2f;
                    _doubleAttackStack = 0;
                }
            }
        }

        public void DropMeteor(PlayerCharacter owner, Stage stage,
            float meteorDropRadius,
            float attackDamage,
            float attackRadius,
            float areaEffectLifeTime
            )
        {
            var position = owner.Pos + meteorDropRadius * Random.insideUnitCircle;
            stage.CreateMeteorAreaEffectObject(
                owner: owner,
                dropPosition: position,
                attackDamage: attackDamage,
                attackRadius: attackRadius,
                knockbackPower: CombatSystem.CalculateSkillAttackKnockBackPower(owner.Stats, 1.0f),
                areaEffectLifetime: areaEffectLifeTime,
                isTranscendent: true);
            UnityGlobal.Sounds.PlayBySoundPrefab(_meteorSkillSFXPath, position);
        }
    }
}
