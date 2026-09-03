using Shared.DataTables;
using UnityEngine;
using Z.GameClients.Stages.Characters.StatusEffects;
using Z.GameClients.Stages.CombatSystems;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.InvisibleAIs
{
    public class InvisibleMoveAndStopCombatAIStrategy : MonsterAIStrategyBase
    {
        private Character _target;
        private float _findTargetAt;
        private float _moveAt;
        private float _stopAt;
        private bool _isMoving;

        private static readonly float TARGET_CHECK_PERIOD = 1.0f;
        private static readonly float MOVE_TIME = 1.75f;
        private static readonly float STOP_TIME = 2f;

        public InvisibleMoveAndStopCombatAIStrategy(Character target)
        {
            _target = target;
        }

        public override void Begin(Stage stage, Monster owner)
        {
            _findTargetAt = 0.0f;
            _moveAt = Time.time;
            _stopAt = _moveAt + MOVE_TIME;
        }

        public override MonsterAIStrategyBase Update(Stage stage, Monster owner)
        {
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
                return new InvisibleMoveAndStopIdleAIStrategy();
            }

            // 움직이고 있는데 멈출 시간이 된 경우
            if (_isMoving && _stopAt < now)
            {
                owner.StopMovement();
                _isMoving = false;
                _moveAt = now + STOP_TIME;
                owner.StatusEffects.AddOrUpdateStatusEffect(stage, owner, StatusEffectType.Invisible, STOP_TIME, now, 0f);
            }
            // 멈춰 있는데 움직일 시간이 된 경우
            else if (!_isMoving && _moveAt < now)
            {
                _isMoving = true;
                _stopAt = now + MOVE_TIME;
            }

            if (_isMoving)
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
