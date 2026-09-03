using Shared.StaticDatas;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.CombatSystems;
using Random = UnityEngine.Random;

namespace Z.GameClients.Stages.Characters.PCs.Skills
{
    public class GymBallSkill: SkillBase
    {
        private readonly int _ballCount;                //발사 개수
        private readonly float _firePeriod;             //발사 간격
        private readonly float _attackPowerRate;        //피해 계수
        private readonly float _moveSpeed;              //발사체 속도
        private readonly float _lifeTime;               //발사체 지속시간
        private readonly float _targetSearchRadius;     //발사 타겟 검색 범위
        private readonly float _knockBackPower;         // 넉백수치

        private int _fireCount;                         //현재 발사 개수
        private float _lastFireAt;                      //마지막 발사 시간

        public GymBallSkill(SkillStaticData staticData) : base(staticData)
        {
            _attackPowerRate = staticData.Parameter1;
            _moveSpeed = staticData.Parameter2;
            _ballCount = (int)staticData.Parameter3;
            _knockBackPower = staticData.Parameter4;
            _lifeTime = staticData.Parameter5;

            _targetSearchRadius = 9.0f;
            _firePeriod = 0.1f;

            _fireCount = 0;
            _lastFireAt = 0;
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);
            _fireCount = 0;
        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);

            int ballRemainingCount = _ballCount - _fireCount;
            if (0 < ballRemainingCount)
            {
                for (int i = 0; i < ballRemainingCount; i++)
                {
                    FireBall(owner, stage);
                }
            }
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            if (_fireCount > _ballCount)
            {
                return;
            }

            if (now < _lastFireAt + _firePeriod)
            {
                return;
            }
            _lastFireAt = now;
            _fireCount++;
            this.FireBall(owner, stage);
        }

        private void FireBall(PlayerCharacter owner, Stage stage)
        {
            Vector2 targetDirection;
            Character target = null;
            List<Character> enemies = new List<Character>();
            stage.FindAliveCharactersInArea(owner.Alliance.ToEnemyAlliance(), new CircularTargetArea(owner.CenterPos, _targetSearchRadius), enemies);
            if (enemies.Count != 0)
            {
                target = enemies[Random.Range(0, enemies.Count)];
            }

            if (target == null)
            {
                targetDirection = Random.insideUnitCircle;
            }
            else
            {
                targetDirection = target.transform.position - owner.transform.position;
            }
            targetDirection.Normalize();

            var attackRangeRatio = owner.Stats.AttackRangeDistanceRatio.Value;
            float damage = CombatSystem.CalculateSkillAttackDamage(owner.Stats, _attackPowerRate);
            float movingspeed = _moveSpeed * owner.Stats.ProjectileMoveSpeedIncreaseRate.Value;
            Vector3 ballObjectScale = Vector3.one * attackRangeRatio;

            var ballObject = stage.CreateGymBallObject(
                                        owner.Alliance,
                                        owner,
                                        movingDirection: targetDirection,
                                        movingspeed,
                                        damage,
                                        knockBackPower: 0f,
                                        _lifeTime,
                                        this.IsTranscendent? 9 : 0, 
                                        this.IsTranscendent,
                                        StaticData.SkillHitSFXPath
                                        );
            ballObject.transform.localScale = ballObjectScale;
        }
    }
}
