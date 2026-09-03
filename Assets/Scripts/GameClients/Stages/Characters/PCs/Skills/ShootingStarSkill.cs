using Shared.DataTables;
using Shared.StaticDatas;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Stats;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.Characters.PCs.Skills
{
    public class ShootingStarSkill : SkillBase
    {

        private readonly float _explosionPowerRate;         //폭발 데미지 파라미터
        private readonly float _moveSpeed;                  //이동 속도 파라미터
        private readonly float _aliveDistance;              //이동 거리 파라미터
        private readonly float _explosionKnockbackPower;    //폭발 넉백 파라미터
        private readonly float _explosionRadius;            //폭발 범위 파라미터
        private readonly float _collisionPowerRate;         //투사체 충돌 데미지 파라미터
        private readonly float _collisionKnobackPower;

        private readonly int _fireAmount;     //발사 개수
        private float _firePeriod;            //발사 간격
        private int _currentFireCount;        //현재 발사 개수
        private float _lastFireAt;            //마지막 발사 시간

        private Vector2 _prevMoveDirection;

        public override float Duration => base.Duration / _characterStats.SkillAttackSpeedValue;
        public override float Cooltime => base.Cooltime / _characterStats.SkillAttackSpeedValue;

        private readonly IReadOnlyCharacterStatCalculators _characterStats;


        public ShootingStarSkill(SkillStaticData staticData, IReadOnlyCharacterStatCalculators characterStats) : base(staticData)
        {
            _characterStats = characterStats;

            _explosionPowerRate = staticData.Parameter1;
            _moveSpeed = staticData.Parameter2;
            _collisionKnobackPower = staticData.Parameter3;
            _explosionKnockbackPower = staticData.Parameter4;
            _explosionRadius = staticData.Parameter5;
            _collisionPowerRate = staticData.Parameter6;

            _fireAmount = 1;
            _aliveDistance = 35f;
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);

            _firePeriod = Duration / _fireAmount;
            _currentFireCount = 0;
            _lastFireAt = 0;
            _prevMoveDirection = Vector2.up;
            this.PlaySkillSoundEffect(owner.Pos);
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

            Character target = stage.FindClosestCharacter(owner.Alliance.ToEnemyAlliance(),
                owner.CenterPos,
                limitDistance: GameConstants.PC_TARGET_SEARCH_MAX_DISTANCE,
                condition: character => !character.Action.IsDead && !character.IsImmuneToHit);
            Vector2 fireDirection;
            if (target != null)
            {
                fireDirection = (target.CenterPos - owner.CenterPos).normalized;
            }
            else
            {
                var item = owner.FindClosestBreakableItemObjectExceptFence(stage, GameConstants.PC_TARGET_SEARCH_MAX_DISTANCE);
                if (item == null)
                {
                    fireDirection = _prevMoveDirection;
                }
                else
                {
                    fireDirection = ((Vector2)item.transform.position - owner.CenterPos).normalized;
                }
            }

            if (owner.MoveDir != Vector2.zero)
            {
                _prevMoveDirection = owner.MoveDir;
            }

            _lastFireAt = now;
            _currentFireCount++;

            float explosionDamage = CombatSystem.CalculateSkillAttackDamage(owner.Stats, _explosionPowerRate);
            float collisionDamage = CombatSystem.CalculateSkillAttackDamage(owner.Stats, _collisionPowerRate);
            float moveSpeed = _moveSpeed * _characterStats.ProjectileMoveSpeedIncreaseRateValue;
            float aliveDistance = _aliveDistance * owner.Stats.AttackRangeDistanceRatio.Value;
            float explosionRadius = _explosionRadius * owner.Stats.AttackRangeDistanceRatio.Value;
            float explosionKnockbackPower = _explosionKnockbackPower * owner.Stats.SkillAttackKnockBackPower.Value;
            float collisionKnockbackPower = _collisionKnobackPower * owner.Stats.SkillAttackKnockBackPower.Value;


            stage.CreateShootingStarAreaEffect(
                owner,
                fireDirection,
                moveSpeed,
                acceleration: moveSpeed * 0.5f,
                aliveDistance,
                collisionDamage,
                explosionDamage,
                explosionRadius,
                explosionDelay: 2.0f,
                collisionKnockbackPower,
                explosionKnockbackPower,
                scaleUp: owner.Stats.AttackRangeDistanceRatio.Value,
                IsTranscendent);

        }
    }

}
