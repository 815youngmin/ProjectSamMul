using Shared.DataTables;
using UnityEngine;
using Z.GameClients.Stages.CombatSystems;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies
{
    public class EliteReflectionRangeAttackAndSummonerCombatAIStrategy : MonsterAIStrategyBase
    {
        private Character _target;
        private float _findTargetAt;
        private float _fireAndSummonAt;

        private readonly static float TARGET_CHECK_PERIOD = 1.0f;
        private float SUMMON_PERIOD;
        private float SummonAttackPowerWeight;
        private float SummonHPWeight;
        private float ReflectionObjectRotateSpeed;
        public EliteReflectionRangeAttackAndSummonerCombatAIStrategy(Character target)
        {
            _target = target;
        }

        public override void Begin(Stage stage, Monster owner)
        {
            _findTargetAt = 0.0f;
            _fireAndSummonAt = Time.time;
            SUMMON_PERIOD = 1.0f /  owner.Stats.CharacterAttackSpeed.Value;
            SummonAttackPowerWeight = owner.StaticData.Param1;
            SummonHPWeight = owner.StaticData.Param2;
            ReflectionObjectRotateSpeed = owner.StaticData.Param3;
        }

        public override MonsterAIStrategyBase OnHitted(Stage stage, Monster owner, Character attacker)
        {
            return null;
        }

        public override MonsterAIStrategyBase Update(Stage stage, Monster owner)
        {
            if (!owner.Action.IsIdle)
            {
                return null;
            }
            var now = Time.time;

            if (_findTargetAt < now)
            {
                _findTargetAt = now + TARGET_CHECK_PERIOD;
                _target = stage.FindClosestCharacter(
                    owner.Alliance.ToEnemyAlliance(),
                    owner.Pos,
                    limitDistance: GameConstants.AI_TARGET_SEARCH_MAX_DISTANCE,
                    condition: character => !character.Action.IsDead);
            }

            if (_target == null || _target.IsImmuneToHit)
            {
                return new EliteReflectionRangeAttackAndSummonerIdleAIStrategy();
            }

            if (_fireAndSummonAt + SUMMON_PERIOD < now )
            {
                _fireAndSummonAt = now;
                owner.DoMultipleReflectionRangeAttackAndMonsterSummon(
                    stage, _target, 
                    projectileAmount: 6, projectileSpeed: 8, projectileLifeTime: 10f, projectileRadius: 0.5f, ReflectionObjectRotateSpeed, owner.StaticData.SpecialAttack1ResourcePath,
                    summonAmount: 2, SummonAttackPowerWeight, SummonHPWeight, owner.StaticData.SpawnMonsterType);
                return null;
            }

            owner.Move(_target.Pos - owner.Pos);
            return null;
        }

        public override void End(Stage stage, Monster owner)
        {

        }
    }

}

