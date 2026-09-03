using Shared.DataTables;
using Shared.StaticDatas;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Stats;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.UnityHelpers;

namespace SamMul.GameClients.Stages.Characters.PCs.Skills
{
    public class BouncingClawSkill : SkillBase
    {
        private readonly float _firstAttackPowerRate;  //피해 계수
        private readonly int _fireAmount;         //발사 개수
        private readonly int _chainAmount;        //전이 개수
        private readonly float _chainRadius;      //전이 범위
        private readonly float _moveSpeed;        //이동 속도
        private readonly float _chainSpeed;       //전이 속도

        private readonly float _slowEffectSpeedChangeRate;  //이동속도 변화 비율 : 0~1.0 사이의 값으로 입력. 0 : 이동속도 정지, 1 : 이동속도 그대로, 0.2 : 0.2배가 됨
        private readonly float _slowEffectDuration;   //이동속도 감소 지속시간, 스턴 지속 시간

        private float _firePeriod;             //발사 간격

        public override float Duration => base.Duration / _characterStats.SkillAttackSpeedValue;
        public override float Cooltime => base.Cooltime / _characterStats.SkillAttackSpeedValue;

        private readonly IReadOnlyCharacterStatCalculators _characterStats;

        private int _currentFireCount;        //현재 발사 개수
        private float _lastFireAt;            //마지막 발사 시간
        private Vector2 _prevMoveDirection;

        public BouncingClawSkill(SkillStaticData staticData, IReadOnlyCharacterStatCalculators characterStats) : base(staticData)
        {
            _firstAttackPowerRate = staticData.Parameter1;
            _fireAmount = (int)staticData.Parameter2;
            _chainAmount = (int)staticData.Parameter3;
            _chainRadius = staticData.Parameter4;

            _slowEffectDuration = staticData.Parameter5;
            _slowEffectSpeedChangeRate = staticData.Parameter6;

            _moveSpeed = 20f;
            _chainSpeed = 40f;

            _characterStats = characterStats;
            
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);
            

            _firePeriod = Duration / _fireAmount;
            _currentFireCount = 0;
            _lastFireAt = 0;
            _prevMoveDirection = Vector2.up;
        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            if (_currentFireCount > _fireAmount)
            {
                return;
            }

            if (now < _lastFireAt + _firePeriod)
            {
                return;
            }

            Character target =  stage.FindClosestCharacter(owner.Alliance.ToEnemyAlliance(),
                                                          owner.CenterPos,
                                                          limitDistance: GameConstants.PC_TARGET_SEARCH_MAX_DISTANCE,
                                                          condition: character => !character.Action.IsDead && !character.IsImmuneToHit);

            Vector2 fireDirection; 
            if(target != null)
            {
                fireDirection = (target.CenterPos - owner.CenterPos).normalized;
            }
            else
            {
                fireDirection = _prevMoveDirection;
            }

            if(owner.MoveDir != Vector2.zero)
            {
                _prevMoveDirection = owner.MoveDir;
            }

            float damage = CombatSystem.CalculateSkillAttackDamage(owner.Stats, _firstAttackPowerRate);
            float chainDamage = damage * 0.5f;
            float moveSpeed = _moveSpeed * _characterStats.ProjectileMoveSpeedIncreaseRateValue;
            float chainSpeed = _chainSpeed * _characterStats.ProjectileMoveSpeedIncreaseRateValue;
            float chainRadius = _chainRadius * owner.Stats.AttackRangeDistanceRatio.Value;

            stage.CreateBouncingClawAreaEffect(
                owner, 
                fireDirection,
                damage, 
                chainDamage, 
                _chainAmount, 
                chainRadius, 
                chainSpeed, 
                moveSpeed, 
                _slowEffectDuration,
                _slowEffectSpeedChangeRate,
                owner.Stats.AttackRangeDistanceRatio.Value,
                IsTranscendent);

            _lastFireAt = now;
            _currentFireCount++;

            UnityGlobal.Sounds.PlayBySoundPrefab(StaticData.SpawnedObjectSFXPath, owner.Pos);
        }
    }

}
