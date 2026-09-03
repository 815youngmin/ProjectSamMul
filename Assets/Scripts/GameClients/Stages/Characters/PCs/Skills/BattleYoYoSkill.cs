using Shared.StaticDatas;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Stats;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.Characters.PCs.Skills
{
    public class BattleYoYoSkill : SkillBase
    {
        private readonly float _attackPowerRate;         // Param1 : 일반 공격 대미지 증가 비율 (피해 계수), 최종대미지 = PC공격력 * (1+Param1)
        private StatModifier _attackSpeedIncreaser;      // Param2 : 공격속도 
        private readonly float _knockbackPower;          // param3 : 넉백 파워
        private readonly float _attackRange;             // Param4 : 공격 거리 (요요가 이동하는 거리)
        private readonly float _waitingDuration;         // Param5 : 대기 시간
        private readonly float _attackRadius;            // Param6 : 요요 공격 범위(반지름)

        private readonly float _baseMovingDuration; //타겟까지 이동하는 시간 

        private float _lastAttackedAt;
        private IReadOnlyCharacterStatCalculators _characterStats;

        private readonly float _gradeEffectAttackRangeRatio;

        //IncreaseBattleYoYoDamage 변장 효과 
        private float _increaseBattleYoYoDamagePercente;

        private float _deployYoyoDuration;
        private float _deployYoyoDamagePercent;
        private float DeployYoyoKnobackPower = 0.1f;

        public BattleYoYoSkill(SkillStaticData staticData, IReadOnlyCharacterStatCalculators characterStats, IReadOnlyCustomParameters parameters) : base(staticData)
        {
            _characterStats = characterStats;

            _attackPowerRate = staticData.Parameter1;
            _attackSpeedIncreaser = new StatModifier(staticData.Parameter2, StatModType.Flat);
            _knockbackPower = staticData.Parameter3;
            _attackRange = staticData.Parameter4;
            _waitingDuration = staticData.Parameter5;
            _attackRadius = staticData.Parameter6;
            _baseMovingDuration = 0.215f;

            _gradeEffectAttackRangeRatio = parameters.GetParameterValue(CustomParameterType.BattleYoyoAttackRangeRatio);

            _increaseBattleYoYoDamagePercente = parameters.GetParameterValue(CustomParameterType.IncreaseBattleYoYoDamagePercent);

            _deployYoyoDuration = parameters.GetParameterValue(CustomParameterType.DeployYoyoDuration);
            _deployYoyoDamagePercent = parameters.GetParameterValue(CustomParameterType.DeployYoyoDamagePercent);
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);
            owner.Stats.CharacterAttackSpeed.AddModifier(_attackSpeedIncreaser);
            _lastAttackedAt = 0;
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            if (owner.Action.IsDead)
            {
                return;
            }

            float rangeAttackPeriod = 1.0f / _characterStats.CharacterAttackSpeedValue;
            if (IsTranscendent)
            {
                // 초월공격은 겹치지 않고 나가도록, 최소한의 상수 주기를 설정한다. (밸런스가 너무 안 맞아서)
                rangeAttackPeriod = 3.3f;
            }
            
            if (now < _lastAttackedAt + rangeAttackPeriod)
            {
                return;
            }

            var damage = CombatSystem.CalculateCharacterBasicAttackDamage(owner.Stats, _attackPowerRate);
            if(_increaseBattleYoYoDamagePercente > 0)
            {
                damage = (1.0f + _increaseBattleYoYoDamagePercente) * damage;
            }

            // 범위 증가 스킬로 공격 거리를 늘려준다
            // 공격 오브젝트의 공격 반경(반지름), 오브젝트 스케일값 증가는 AreaEffectObject 내부에서 처리한다.
            float resultAttackRange = _gradeEffectAttackRangeRatio * owner.Stats.AttackRangeDistanceRatio.Value * _attackRange;
            float resultAttackRadius = _gradeEffectAttackRangeRatio * owner.Stats.AttackRangeDistanceRatio.Value * _attackRadius;
            float knockbackPower = CombatSystem.CalculateCharacterAttackKnockBackPower(owner.Stats, _knockbackPower);

            if (IsTranscendent)
            {
                PlaySkillSoundEffect(owner.Pos);
                stage.CreateBattleYoYoTranscendentObject(
                    owner,
                    damage,
                    owner.CenterPos,
                    radiusUpSpeed: 5f,
                    angleUpSpeed: 480f,
                    radiusUpAccelration: 1f,
                    angleUpAccelration: 180f,
                    maxRotateRadius: resultAttackRange,
                    attackRadius: resultAttackRadius,
                    knockbackPower,
                    Vector2.right,
                    StaticData.SkillHitSFXPath
                    );
                stage.CreateBattleYoYoTranscendentObject(
                    owner,
                    damage,
                    owner.CenterPos,
                    radiusUpSpeed: 5f,
                    angleUpSpeed: 480f,
                    radiusUpAccelration: 1f,
                    angleUpAccelration: 180f,
                    maxRotateRadius: resultAttackRange,
                    attackRadius: resultAttackRadius,
                    knockbackPower,
                    Vector2.left,
                    StaticData.SkillHitSFXPath
                    );
            }
            else
            {
                // 0.2m 만큼 여유있게 탐색한다.
                Vector2 targetPosition;
                var target = owner.FindBasicAttackTarget(stage, resultAttackRange + resultAttackRadius);
                if (target == null)
                {
                    var item = owner.FindClosestBreakableItemObjectExceptFence(stage, resultAttackRange + resultAttackRadius);
                    if (item == null)
                    {
                        return;
                    }
                    targetPosition = item.transform.position;
                }
                else
                {
                    targetPosition = target.Pos;
                }

                PlaySkillSoundEffect(owner.Pos);
                float movingDuration = _baseMovingDuration * (1f / _characterStats.ProjectileMoveSpeedIncreaseRateValue);
                float acceleration = 2f * resultAttackRange / (movingDuration * movingDuration);
                float initialMoveSpeed = acceleration * movingDuration;
                float resultWaitingDuration = _waitingDuration * _characterStats.DurationIncreaseRateValue;
                float deployYoyoDamage = damage * _deployYoyoDamagePercent;
                stage.CreateBattleYoYoAreaEffectObject(
                    owner,
                    targetPosition: targetPosition,
                    moveDistance: resultAttackRange,
                    waitingDuration: resultWaitingDuration,
                    acceleration: acceleration,
                    moveSpeed: initialMoveSpeed,
                    attackRadius: resultAttackRadius,
                    knockbackPower,
                    damage,
                    _deployYoyoDuration,
                    DeployYoyoKnobackPower,
                    deployYoyoDamage,
                    StaticData.SkillHitSFXPath
                );
            }
            owner.ConditionalEffects.OnBasicSkillUsed(stage, owner);
            _lastAttackedAt = now;
        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);
            owner.Stats.CharacterAttackSpeed.RemoveModifier(_attackSpeedIncreaser);
        }

    }

}
