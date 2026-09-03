using Shared.StaticDatas;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters.Stats;
using Z.GameClients.Stages.CombatSystems;


namespace Z.GameClients.Stages.Characters.PCs.Skills
{
    public class TornadoSkill : SkillBase
    {
        private static readonly float FINDING_RADIUS = 10.0f;

        private readonly float _attackPowerRate;                // 토네이도 공격 추가 대미지 계수.
        private readonly float _attackRadius;                   // 토네이도 공격 반지름.
        private readonly float _attackDuration;                 // 토네이도 공격 지속 시간.
        private readonly float _attackPeriod;                   // 토네이도 공격 대미지 주기.
        private readonly float _transcendentAttackPowerRate;    // 토네이도 초월 공격 추가 대미지 계수.

        private readonly IReadOnlyCharacterStatCalculators _characterStats;
        private readonly List<Character> _enemies;

        public override float Duration => base.Duration / _characterStats.SkillAttackSpeedValue;
        public override float Cooltime => base.Cooltime / _characterStats.SkillAttackSpeedValue;

        public TornadoSkill(SkillStaticData staticData, IReadOnlyCharacterStatCalculators characterStats) : base(staticData)
        {
            _attackPowerRate = staticData.Parameter1;
            _attackRadius = staticData.Parameter2;
            _attackDuration = staticData.Parameter3;
            _attackPeriod = staticData.Parameter4;
            _transcendentAttackPowerRate = staticData.Parameter5;

            _characterStats = characterStats;
            _enemies = new List<Character>();
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);

            // 활성화된 첫 프레임에 바로 적을 찾아 공격한다.
            _enemies.Clear();
            stage.FindAliveCharactersInArea(owner.Alliance.ToEnemyAlliance(), new CircularTargetArea(owner.Pos, radius: FINDING_RADIUS), _enemies);

            Vector2 position;
            // 수색 범위 안에 적이 있으면 가장 체력이 높은 적을 공격한다.
            if (_enemies.Count > 0)
            {
                Character target = _enemies[0];
                foreach (var enemy in _enemies)
                {
                    if (enemy.CurrentHP > target.CurrentHP)
                    {
                        target = enemy;
                    }
                }
                position = target.Pos;
            }
            // 수색 범위 안에 적이 없으면 랜덤 위치를 공격한다.
            else
            {
                var item = owner.FindClosestBreakableItemObjectExceptFence(stage, FINDING_RADIUS);
                if(item != null)
                {
                    position = item.transform.position;
                }
                else
                {
                    position = owner.Pos + FINDING_RADIUS * Random.insideUnitCircle;
                }
            }

            // 토네이도 생성.
            stage.CreateTornadoAreaEffectObject(
                owner: owner,
                position: position,
                attackDamagePerTick: CombatSystem.CalculateSkillAttackDamage(owner.Stats, _attackPowerRate),
                attackRadius: _attackRadius * owner.Stats.AttackRangeDistanceRatio.Value,
                attackDuration: _attackDuration * _characterStats.DurationIncreaseRateValue,
                attackTickPeriod: _attackPeriod / _characterStats.SkillAttackSpeedValue,
                transcendentAttackDamage: CombatSystem.CalculateSkillAttackDamage(owner.Stats, _transcendentAttackPowerRate),
                isTranscendent: IsTranscendent);
        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            // 활성화된 첫 프레임에 바로 적을 찾아 공격하기 때문에, Update에서는 아무 로직도 처리하지 않는다.
        }
    }
}
