using Shared.DataTables;
using UnityEngine;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.SummonOnDieAIs
{
    public class DashAttackSummonOnDieCombatAIStrategy : SummonOnDieMonsterAIStrategyBase
    {
        private static readonly float TARGET_CHECK_PERIOD = 1.0f;
        private static readonly float DASH_PRE_DELAY = 1.0f;
        private static readonly float DASH_POST_DELAY = 1.0f;
        private static readonly float DASH_COOL_TIME = 3.0f;

        private Character _target;
        private float _findTargetAt;
        private float _dashEndedAt;
        private bool _isDashing;

        public DashAttackSummonOnDieCombatAIStrategy(Character target) : base()
        {
            _target = target;
        }

        public override void Begin(Stage stage, Monster owner)
        {
            _findTargetAt = 0.0f;
            _dashEndedAt = 0.0f;
            _isDashing = false;
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
                _dashEndedAt = now;
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
                return new DashAttackSummonOnDieIdleAIStrategy();
            }

            if (_dashEndedAt + DASH_COOL_TIME < now && !_isDashing)
            {
                owner.DoDashAction(stage, _target, owner.StaticData.Param1, DASH_PRE_DELAY, owner.StaticData.Param2, DASH_POST_DELAY);
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

        public override MonsterAIStrategyBase OnDead(Stage stage, Monster owner)
        {
            this.Summon(stage, owner);
            return null;
        }

    }
}
