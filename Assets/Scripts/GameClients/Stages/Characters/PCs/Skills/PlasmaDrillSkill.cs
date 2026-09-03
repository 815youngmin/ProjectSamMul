using Shared.StaticDatas;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Stats;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.Characters.PCs.Skills
{
    public class PlasmaDrillSkill : SkillBase
    {
        private int _dartCount;                //발사 개수
        private float _firePeriod;             //발사 간격
        private float _attackPowerRate;        //피해 계수
        private float _moveSpeed;              //발사체 속도
        private float _lifeTime;               //발사체 지속시간
        private float _targetSearchRadius;     //발사 타겟 검색 범위

        private int _fireCount;                         //현재 발사 개수
        private float _lastFireAt;                      //마지막 발사 시간

        public override float Duration => base.Duration / _characterStats.SkillAttackSpeedValue;
        public override float Cooltime => base.Cooltime / _characterStats.SkillAttackSpeedValue;

        private readonly IReadOnlyCharacterStatCalculators _characterStats;

        public PlasmaDrillSkill(SkillStaticData staticData, IReadOnlyCharacterStatCalculators characterStats) : base(staticData)
        {
            _attackPowerRate = staticData.Parameter1;
            _moveSpeed = staticData.Parameter2;
            _dartCount = (int)staticData.Parameter3;
            _lifeTime = staticData.Parameter4;
            _targetSearchRadius = staticData.Parameter5;

            _fireCount = 0;
            _lastFireAt = 0;

            _characterStats = characterStats;
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);
            _firePeriod = Duration / _dartCount;
            _fireCount = 0;
        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);

            int dartRemainingCount = _dartCount - _fireCount;
            if (0 < dartRemainingCount)
            {
                for (int i = 0; i < dartRemainingCount; i++)
                {
                    this.FirePlasmaDrill(owner, stage);
                }
            }
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            if (_fireCount > _dartCount)
            {
                return;
            }

            if (now < _lastFireAt + _firePeriod)
            {
                return;
            }
            _lastFireAt = now;
            _fireCount++;

            if (_fireCount <= 3)
            {
                PlaySkillSoundEffect(owner.Pos);
            }
            this.FirePlasmaDrill(owner, stage);
        }

        private void FirePlasmaDrill(PlayerCharacter owner, Stage stage)
        {
            Vector2 targetDirection;
            Character target = null;
            List<Character> enemies = new List<Character>();
            stage.FindCharactersInArea(owner.Alliance.ToEnemyAlliance(),
                                        new CircularTargetArea(owner.CenterPos, _targetSearchRadius),
                                        condition: character => !character.Action.IsDead && !character.IsImmuneToHit, enemies);

            if (enemies.Count != 0)
            {
                target = enemies[Random.Range(0, enemies.Count)];
            }

            if (target == null)
            {
                var item = owner.FindClosestBreakableItemObjectExceptFence(stage, _targetSearchRadius);
                if (item == null)
                {
                    targetDirection = Random.insideUnitCircle;
                }
                else
                {
                    targetDirection = (Vector2)item.transform.position - owner.Pos;
                }
            }
            else
            {
                targetDirection = target.transform.position - owner.transform.position;
            }
            targetDirection.Normalize();

            var attackRangeRatio = owner.Stats.AttackRangeDistanceRatio.Value;
            float damage = CombatSystem.CalculateSkillAttackDamage(owner.Stats, _attackPowerRate);
            float objectRadius = (this.IsTranscendent ? 0.9f : 0.4f) * attackRangeRatio;
            float movingspeed = _moveSpeed *  owner.Stats.ProjectileMoveSpeedIncreaseRate.Value;
            Vector3 plasmaDrillObjectScale = Vector3.one * attackRangeRatio;
            float lifeTime = _lifeTime *  _characterStats.DurationIncreaseRateValue;
            
            var plasmaDrillObject = stage.CreatePlasmaDrillObject(
                owner.Alliance,
                owner,
                objectRadius,
                movingDirection: targetDirection,
                movingspeed,
                damage,
                knockBackPower: 0f,
                attackPeriod: 0.33f,
                lifeTime,
                this.IsTranscendent,
                StaticData.SkillHitSFXPath
            );
            plasmaDrillObject.transform.localScale = plasmaDrillObjectScale;
        }
    }

}

