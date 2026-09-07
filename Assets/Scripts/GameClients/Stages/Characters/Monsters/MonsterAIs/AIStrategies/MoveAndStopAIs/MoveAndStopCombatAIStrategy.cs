using Shared.DataTables;
using UnityEngine;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.MoveAndStopAIs
{
    public class MoveAndStopCombatAIStrategy : MonsterAIStrategyBase
    {
        private Character _target;
        private float _findTargetAt;
        private float _moveAt;
        private float _stopAt;
        private bool _isMoving;
        private float _moveTime;
        private float _stopTime;

        private static readonly float TARGET_CHECK_PERIOD = 1.0f;
        // 몬스터 테이블 Param1(이동 시간), Param2(정지 시간)이 0보다 크면 그 값을 쓰고, 아니면 기본값을 쓴다.
        private static readonly float MOVE_TIME = 1.75f;
        private static readonly float STOP_TIME = 2f;

        public MoveAndStopCombatAIStrategy(Character target)
        {
            _target = target;
        }

        public override void Begin(Stage stage, Monster owner)
        {
            _findTargetAt = 0.0f;
            _moveTime = owner.StaticData.Param1 > 0f ? owner.StaticData.Param1 : MOVE_TIME;
            _stopTime = owner.StaticData.Param2 > 0f ? owner.StaticData.Param2 : STOP_TIME;
            _moveAt = Time.time;
            _stopAt = _moveAt + _moveTime;
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
                return new MoveAndStopIdleAIStrategy();
            }

            // 움직이고 있는데 멈출 시간이 된 경우
            if (_isMoving && _stopAt < now)
            {
                owner.StopMovement();
                _isMoving = false;
                _moveAt = now + _stopTime;
            }
            // 멈춰 있는데 움직일 시간이 된 경우
            else if (!_isMoving && _moveAt < now)
            {
                _isMoving = true;
                _stopAt = now + _moveTime;
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
