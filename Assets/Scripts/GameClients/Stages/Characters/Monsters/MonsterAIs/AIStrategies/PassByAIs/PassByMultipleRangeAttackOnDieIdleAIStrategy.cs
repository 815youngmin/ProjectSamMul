
using DG.Tweening;
using Shared.DataTables;
using UnityEngine;
using Z.GameClients.Stages.CombatSystems;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.PassByAIs
{
    // 일반적인 대기 AI
    // 주변에 적을 탐색하고
    // 사정거리 내로 적이 들어오면 타겟으로 삼고
    // 타겟이 설정되면, 전투 전략으로 전이 
    public class PassByMultipleRangeAttackOnDieIdleAIStrategy : MonsterAIStrategyBase
    {
        Character _target;
        private float _findTargetCheckAt;
        private readonly static float _findTargetCheckDuration = 1.0f;
        private int _projectileAmount;
        private float _rangeAttackDelay;

        public override void Begin(Stage stage, Monster owner)
        {
            owner.StopMovement();
            _findTargetCheckAt = 0.0f;
            _projectileAmount = (int)owner.StaticData.Param1;
            _rangeAttackDelay = owner.StaticData.Param2;
        }

        public override MonsterAIStrategyBase Update(Stage stage, Monster owner)
        {
            float now = Time.time;
            if (_findTargetCheckAt < now)
            {
                _findTargetCheckAt = now + _findTargetCheckDuration;
                Character enemy = stage.FindClosestCharacter(
                    owner.Alliance.ToEnemyAlliance(),
                    owner.Pos,
                    limitDistance: GameConstants.AI_TARGET_SEARCH_MAX_DISTANCE, 
                    condition: character => !character.Action.IsDead);
                _target = enemy;
            }

            if (IsEnemyInSearchDistance(owner, _target))
            {
                return new PassByMultipleRangeAttackOnDieCombatAIStrategy(_target);
            }

            return null;
        }

        public override void End(Stage stage, Monster owner)
        {

        }

        public override MonsterAIStrategyBase OnHitted(Stage stage, Monster owner, Character attacker)
        {
            // 전투 전략으로 전환 
            if(null == attacker)
            {
                return null;
            }

            return new PassByMultipleRangeAttackOnDieCombatAIStrategy(attacker);
        }

        private static readonly float ProjectileSpeed = 10;
        private static readonly float ProjectileAcceleration = 0.0f;
        private static readonly float ProjectileRadius = 0.5f;
        private static readonly float ProjectileAliveDistance = 100;
        private static readonly float ProjectileKnobackPower = 0.1f;

        public override MonsterAIStrategyBase OnDead(Stage stage, Monster owner)
        {
            if (_target != null)
            {
                var rangeAttackSequence = DOTween.Sequence();
                rangeAttackSequence.AppendInterval(_rangeAttackDelay);
                rangeAttackSequence.AppendCallback(() =>
                {
                    for (int i = 0; i < _projectileAmount; i++)
                    {
                        Vector2 dir = Quaternion.Euler(0.0f, 0.0f, 360f / _projectileAmount * i) * Vector2.up;
                        stage.CreateProjectile(
                             owner.StaticData.SpecialAttack1ResourcePath,
                             owner.Alliance,
                             owner,
                             owner.RangeAttackPower,
                             ProjectileKnobackPower,
                             owner.CenterPos,
                             dir,
                             ProjectileSpeed,
                             ProjectileAcceleration,
                             ProjectileRadius,
                             ProjectileAliveDistance,
                             hitChances: 1,
                             splitCount: 0,
                             isRemovableBySpinBladeObject: true,
                             hitSoundPrefabPath: string.Empty
                             );
                    }
                });
            }
            return null;
        }

    }
}