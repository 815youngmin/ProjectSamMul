using Shared.DataTables;
using UnityEngine;
using Z.GameClients.Stages.CombatSystems;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.HealAIs
{
    public class HealCombatAIStrategy : MonsterAIStrategyBase
    {
        private HealAIBlackboard Blackboard => (HealAIBlackboard)base._blackboard;

        private Character _target;
        private float _findTargetAt;

        private float _moveAt;
        private float _stopAt;
        private bool _isMoving;

        private readonly static float TARGET_CHECK_PERIOD = 1.0f;
        private Vector2 _moveDirection;

        public HealCombatAIStrategy(Character target, HealAIBlackboard blackboard) : base(blackboard)
        {
            _target = target;
        }

        public override void Begin(Stage stage, Monster owner)
        {
            _findTargetAt = 0.0f;
            _isMoving = false;
            _moveAt = Time.time;
            _stopAt = _moveAt + Blackboard.MoveTime;
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
                _findTargetAt = now + TARGET_CHECK_PERIOD;
                _target = stage.FindClosestCharacter(
                    owner.Alliance.ToEnemyAlliance(),
                    owner.Pos,
                    limitDistance: GameConstants.AI_TARGET_SEARCH_MAX_DISTANCE,
                    condition: character => !character.Action.IsDead);
            }

            if (_target == null || _target.IsImmuneToHit)
            {
                return new HealIdleAIStrategy(Blackboard);
            }
            // 움직이고 있는데 멈출 시간이 된 경우
            if (_isMoving && _stopAt < now)
            {
                owner.StopMovement();
                _isMoving = false;
                _moveAt = now + Blackboard.StopTime;
            }
            // 멈춰 있는데 움직일 시간이 된 경우
            else if (!_isMoving && _moveAt < now)
            {
                _isMoving = true;
                _stopAt = now + Blackboard.MoveTime;

                if (Vector2.Distance(_target.Pos, owner.Pos) < Blackboard.MaxEscapeDistance)
                {
                    //플레이어와 너무 근접한경우
                    _moveDirection = owner.Pos - _target.Pos;
                }
                else if (Vector2.Distance(_target.Pos, owner.Pos) > Blackboard.MaxProximityDistance)
                {
                    //플레이어와 너무 멀어진 경우
                    _moveDirection = _target.Pos - owner.Pos;
                }
                else
                {
                    //적정 거리
                    _moveDirection = _target.Pos - owner.Pos;
                }
            }

            if (_isMoving)
            {
                owner.Move(_moveDirection);
            }
            else
            {
                owner.DoHealAction(stage, _target, Blackboard.HealRange, Blackboard.HealPercent, Blackboard.StopTime, Blackboard.HealPeriod);
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

