using Shared.DataTables;
using UnityEngine;
using Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.DashAIs;
using Z.GameClients.Stages.CombatSystems;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.InvisibleAIs
{
    public class DashAndSummonerIdleAIStrategy : BasicIdleAIStrategy
    {
        protected override MonsterAIStrategyBase CombatAIStrategy => new DashAndSummonerCombatAIStrategy();
        protected override float WaitingTime => 2.0f;
    }

    public class DashAndSummonerCombatAIStrategy : MonsterAIStrategyBase
    {
        private static readonly float TARGET_CHECK_PERIOD = 1.0f;
        private static readonly float DASH_PRE_DELAY = 1.0f;
        private static readonly float DASH_POST_DELAY = 1.0f;
        private static readonly float DASH_AND_SUMMON_COOL_TIME = 3.0f;

        private Character _target;
        private float _findTargetAt;
        private float _actionEndedAt;
        private bool _isDashing;

        private float _summonAttackPowerWeight;
        private float _summonHPWeight;
        private int _summonAmount;
        public override void Begin(Stage stage, Monster owner)
        {
            _findTargetAt = 0.0f;
            _actionEndedAt = 0.0f;
            _isDashing = false;

            //스탯 가중치를 역산해서 계산
            _summonHPWeight = owner.MaxHP / owner.StaticData.MaxHP;
            _summonAttackPowerWeight = owner.CollisionAttackPower / owner.StaticData.CollisionAttackPower;

            _summonAmount = (int)owner.StaticData.Param3;

        }

        public override MonsterAIStrategyBase Update(Stage stage, Monster owner)
        {
            if (!owner.Action.IsIdle)
            {
                return null;
            }

            var now = Time.time;

            if (_isDashing)
            {
                _actionEndedAt = now;
                _isDashing = false;
            }

            if (_findTargetAt < now)
            {
                _findTargetAt = now + TARGET_CHECK_PERIOD;
                _target = stage.FindClosestCharacter(
                    allianceType: owner.Alliance.ToEnemyAlliance(),
                    position: owner.Pos,
                    limitDistance: GameConstants.AI_TARGET_SEARCH_MAX_DISTANCE,
                    condition: character => !character.Action.IsDead && !character.IsImmuneToHit);
            }

            if (_target == null)
            {
                return new DashAndSummonerIdleAIStrategy();
            }

            if (_actionEndedAt + DASH_AND_SUMMON_COOL_TIME < now && !_isDashing)
            {
                owner.DoDashAndMonsterSummonAction(stage, _target, owner.StaticData.Param1, DASH_PRE_DELAY, owner.StaticData.Param2, DASH_POST_DELAY,
                    _summonAmount, _summonAttackPowerWeight, _summonHPWeight, owner.StaticData.SpawnMonsterType);
                _isDashing = true;
            }
            else
            {
                owner.Move(_target.Pos - owner.Pos);
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
    }

}
