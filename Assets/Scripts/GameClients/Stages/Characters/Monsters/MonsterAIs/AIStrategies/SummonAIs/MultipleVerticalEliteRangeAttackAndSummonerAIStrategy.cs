using Shared.DataTables;
using UnityEngine;
using Z.GameClients.Stages.CombatSystems;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.InvisibleAIs
{
    public class MultipleVerticalEliteRangeAttackAndSummonerIdleAIStrategy : BasicIdleAIStrategy
    {
        protected override MonsterAIStrategyBase CombatAIStrategy => new MultipleVerticalEliteRangeAttackAndSummonerCombatAIStrategy();
        protected override float WaitingTime => 2.0f;
    }

    public class MultipleVerticalEliteRangeAttackAndSummonerCombatAIStrategy : MonsterAIStrategyBase
    {
        private static readonly float TARGET_CHECK_PERIOD = 1.0f;

        private Character _target;
        private float _findTargetAt;
        private float _fireAndSummonDelay;
        private float _fireAndSummonAt;

        private int _projectileAmount;
        private float _firePeriod;
        private bool _isRemovableBySpinBladeObject;
        
        private float _summonAttackPowerWeight;
        private float _summonHPWeight;
        private int _summonAmount;

        public override void Begin(Stage stage, Monster owner)
        {
            _findTargetAt = 0.0f;

            _fireAndSummonDelay = 1.0f / owner.Stats.CharacterAttackSpeed.Value;

            _projectileAmount = (int)owner.StaticData.Param1;
            _firePeriod = owner.StaticData.Param2;
            _isRemovableBySpinBladeObject = false;

            //스탯 가중치를 역산해서 계산
            _summonHPWeight = owner.MaxHP / owner.StaticData.MaxHP;
            _summonAttackPowerWeight = owner.CollisionAttackPower / owner.StaticData.CollisionAttackPower;

            _summonAmount = (int)owner.StaticData.Param3;

            _fireAndSummonAt = Time.time;
        }

        public override MonsterAIStrategyBase Update(Stage stage, Monster owner)
        {
            var now = Time.time;

            if (!owner.Action.IsIdle)
            {
                return null;
            }

            if (_findTargetAt < now)
            {
                Character enemy = stage.FindClosestCharacter(
                    owner.Alliance.ToEnemyAlliance(),
                    owner.Pos,
                    limitDistance: GameConstants.AI_TARGET_SEARCH_MAX_DISTANCE,
                    condition: character => !character.Action.IsDead);
                if (enemy != null)
                {
                    _target = enemy;
                }
                else
                {
                    _target = null;
                }

                _findTargetAt = TARGET_CHECK_PERIOD + now;
            }

            if (!this._target || this._target.IsImmuneToHit)
            {
                return new MultipleVerticalEliteRangeAttackAndSummonerIdleAIStrategy();
            }

            var vectorToTarget = this._target.Pos - owner.Pos;

            if (_fireAndSummonAt + _fireAndSummonDelay < now)
            {
                var squaredRangeAttackEffectiveRange = owner.StaticData.SpecialAttack1EffectiveRange;
                squaredRangeAttackEffectiveRange *= squaredRangeAttackEffectiveRange;

                var squaredDistanceToTarget = vectorToTarget.sqrMagnitude;
                if (squaredDistanceToTarget < squaredRangeAttackEffectiveRange)
                {
                    owner.DoMultipleVerticalRangeAttackAndMonsterSummonAction(stage, _target, 
                        _projectileAmount, _firePeriod, _isRemovableBySpinBladeObject, withIndicator: false, 
                        _summonAmount, _summonAttackPowerWeight, _summonHPWeight, owner.StaticData.SpawnMonsterType);

                    _fireAndSummonAt = now;
                    return null;
                }
            }

            owner.Move(vectorToTarget);

            return null;
        }

        public override void End(Stage stage, Monster owner)
        {
        }

        public override MonsterAIStrategyBase OnHitted(Stage stage, Monster owner, Character attacker)
        {
            return null;
        }
    }

}
