using Shared.DataTables;
using UnityEngine;
using Z.GameClients.Stages.Characters.Actions;
using Z.GameClients.Stages.CombatSystems;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.ForwardAreaAttackAIs
{
    public class ForwardAreaAttackEliteCombatAIStrategy : MonsterAIStrategyBase
    {
        private readonly static float TARGET_CHECK_PERIOD = 1.0f;

        private Character _target;
        private float _findTargetAt;
        private float _attackAt;
        private float _attackPeriod;

        public ForwardAreaAttackEliteCombatAIStrategy(Character target) : base()
        {
            _target = target;
        }

        public override void Begin(Stage stage, Monster owner)
        {
            _attackAt = Time.time;
            _attackPeriod = 1.0f / owner.Stats.CharacterAttackSpeed.Value;
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
                    condition: character => !character.Action.IsDead && !character.IsImmuneToHit);
            }

            if (_target == null || _target.IsImmuneToHit)
            {
                return new ForwardAreaAttackEliteIdleAIStrategy();
            }

            if (_attackAt < now)
            {
                owner.Do<ForwardAreaAttackEliteAction>(stage, _target);
                _attackAt = now + _attackPeriod + ForwardAreaAttackEliteAction.ATTACKDURATION;
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
