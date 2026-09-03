using DG.Tweening;
using Shared.DataTables;
using UnityEngine;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.SummonOnDieAIs
{
    public class MultipleRangeAttackOnDieIdleAIStrategy : SummonOnDieMonsterAIStrategyBase
    {
        private static readonly float TARGET_CHECK_PERIOD = 1.0f;

        private float _findTargetAt;
        private Character _target;
        private int _projectileAmount;
        private float _rangeAttackDelay;

        public MultipleRangeAttackOnDieIdleAIStrategy() : base()
        {

        }

        public override void Begin(Stage stage, Monster owner)
        {
            owner.StopMovement();
            _findTargetAt = 0.0f;
            _projectileAmount = (int)owner.StaticData.Param1;
            _rangeAttackDelay = owner.StaticData.Param2;
        }

        public override MonsterAIStrategyBase Update(Stage stage, Monster owner)
        {
            if (owner.Action.IsBeingSummoned)
            {
                return null;
            }

            float now = Time.time;
            if (_findTargetAt < now)
            {
                _findTargetAt = now + TARGET_CHECK_PERIOD;
                _target = stage.FindClosestCharacter(
                    allianceType: owner.Alliance.ToEnemyAlliance(),
                    position: owner.Pos,
                    limitDistance: GameConstants.AI_TARGET_SEARCH_MAX_DISTANCE,
                    condition: character => !character.Action.IsDead && !character.IsImmuneToHit);

                if (_target != null)
                {
                    return new MultipleRangeAttackOnDieCombatAIStrategy(_target);
                }
            }

            return null;
        }

        public override void End(Stage stage, Monster owner)
        {

        }

        public override MonsterAIStrategyBase OnHitted(Stage stage, Monster owner, Character attacker)
        {
            return null;
        }

        private static readonly float ProjectileSpeed = 10;
        private static readonly float ProjectileAcceleration = 0.0f;
        private static readonly float ProjectileRadius = 0.5f;
        private static readonly float ProjectileAliveDistance = 100;
        private static readonly float ProjectileKnobackPower = 0.1f;

        public override MonsterAIStrategyBase OnDead(Stage stage, Monster owner)
        {
            if(_target != null)
            {
                var rangeAttackSequence = DOTween.Sequence();
                rangeAttackSequence.AppendInterval(_rangeAttackDelay);
                rangeAttackSequence.AppendCallback(()=>
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
