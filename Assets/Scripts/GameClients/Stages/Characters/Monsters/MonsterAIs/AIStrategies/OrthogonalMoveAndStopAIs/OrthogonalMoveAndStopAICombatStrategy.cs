using UnityEngine;
using Z.GameClients.Stages.Characters.Stats;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.MeleeAIs
{
    public class OrthogonalMoveAndStopAICombatStrategy : MonsterAIStrategyBase
    {
        //이동 방향
        private Vector2 _moveDir;
        private Character target;

        private float _moveAt;
        private float _stopAt;
        private bool _isMoving;

        private static readonly float MOVE_TIME = 1f;
        private static readonly float STOP_TIME = 1f;
        private static readonly float SLOW_PERCENT = -0.7f;

        private StatModifier _moveSpeedDecreaser;

        public OrthogonalMoveAndStopAICombatStrategy(Character target, Vector2 moveDir)
        {
            this.target = target;
            _moveDir = moveDir;
            _moveSpeedDecreaser = new StatModifier(SLOW_PERCENT, StatModType.PercentAdd);
        }

        public override void Begin(Stage stage, Monster owner)
        {
            _isMoving = true;
            _moveAt = Time.time;
            _stopAt = _moveAt + MOVE_TIME;
        }

        public override MonsterAIStrategyBase Update(Stage stage, Monster owner)
        {
            float now = Time.time;

            if (this.target.IsImmuneToHit ||
                this.target.Action.IsDead)
            {
                owner.StopMovement();
                return null;
            }

            // 움직이고 있는데 멈출 시간이 된 경우
            if (_isMoving && _stopAt < now)
            {
                _isMoving = false;
                _moveAt = now + STOP_TIME;
                owner.Stats.MoveSpeed.AddModifier(_moveSpeedDecreaser);

            }
            // 멈춰 있는데 움직일 시간이 된 경우
            else if (!_isMoving && _moveAt < now)
            {
                _isMoving = true;
                _stopAt = now + MOVE_TIME;
                owner.Stats.MoveSpeed.RemoveModifier(_moveSpeedDecreaser);
            }

            owner.Move(_moveDir);
 
            return null;
        }

        public override void End(Stage stage, Monster owner)
        {
            owner.Stats.MoveSpeed.RemoveModifier(_moveSpeedDecreaser);
        }

        public override MonsterAIStrategyBase OnHitted(Stage stage, Monster owner, Character attacker)
        {
            return null;
        }

        //재배치 되었으면 사라지도록 처리한다.
        public override MonsterAIStrategyBase OnRePosition(Stage stage, Monster owner)
        {
            owner.DisappearFromStage(stage, withDeadEffect: false);
            return null;
        }

    }
}