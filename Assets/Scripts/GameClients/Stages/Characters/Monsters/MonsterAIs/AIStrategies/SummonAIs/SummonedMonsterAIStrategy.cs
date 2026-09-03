using UnityEngine;
using Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs
{
    public class SummonedMonsterAIStrategy : MonsterAIStrategyBase, ISummonedMonsterCommandSender
    {
        private enum ActionType
        {
            Idle,
            Move,
            RangeAttack,
            ChangeAIStrategy,
        }

        private Monster _owner;
        private ActionType _currentActionType;
        private Vector2 _attackDirection;
        private Vector2 _targetPos;
        private MonsterAIStrategyBase _changeMonsterAIStrategy;

        public override void Begin(Stage stage, Monster owner)
        {
            _owner = owner;
        }

        public override void End(Stage stage, Monster owner)
        {
        }

        public override MonsterAIStrategyBase OnHitted(Stage stage, Monster owner, Character attacker)
        {
            return null;
        }

        public override MonsterAIStrategyBase Update(Stage stage, Monster owner)
        {
            if (owner.Action.IsBeingSummoned)
            {
                return null;
            }

            //MonsterAIStrategyBase 변경하는 타입은 따로 처리한다.
            if (_currentActionType == ActionType.ChangeAIStrategy)
            {
                Debug.Assert(_changeMonsterAIStrategy != null, "변환하려하는 AIStrategy타입이 없습니다. 확인이 필요합니다.");
                return _changeMonsterAIStrategy;
            }

            switch (_currentActionType)
            {
                case ActionType.Idle:
                    break;
                case ActionType.Move:

                    if(Vector2.Distance(owner.Pos, _targetPos) >= owner.Stats.MoveSpeed.Value * Time.deltaTime)
                    {
                        owner.Move(_targetPos - owner.Pos);
                    }
                    else
                    {
                        owner.StopMovement();
                    }
                    break;
                case ActionType.RangeAttack:
                    if(!owner.Action.IsAttacking)
                    {
                        _currentActionType = ActionType.Idle;
                    }
                    break;
                default:
                    break;
            }

            return null;
        }

        public void SendMoveCommand(Stage stage, Vector2 targetPos)
        {
            _targetPos = targetPos;
            _currentActionType = ActionType.Move;
        }

        //대기시 바라볼 방향
        public void SendIdleCommand(Stage stage, Vector2 direction)
        {
            _currentActionType = ActionType.Idle;
        }

        public void SendUpdateDrawOrderCommand()
        {
            _owner.UpdateDrawOrder();
        }

        //해당 함수를 사용하면 ISummonedMonsterCommandSender 인터페이스를 사용 할 수 없습니다.
        public void SendChangeMonsterAIStrategyCommand(Stage stage, MonsterAIStrategyBase monsterAIStrategy)
        {
            _currentActionType = ActionType.ChangeAIStrategy;
            _changeMonsterAIStrategy = monsterAIStrategy;
        }
        public void SendChainCommand(Character chainTarget)
        {
            Debug.Assert(chainTarget != null, "chainTarget이 존재하지 않습니다. 확인이 필요합니다.");
            _owner.ChainTarget(chainTarget);
        }

        public void SendForceKillSelfCommand(Stage stage)
        {
            _owner.ForceKillSelf(stage);
        }

        public void SendColliderEnable(bool enable)
        {
            _owner.GetComponent<CircleCollider2D>().enabled = enable;
        }
    }
}
