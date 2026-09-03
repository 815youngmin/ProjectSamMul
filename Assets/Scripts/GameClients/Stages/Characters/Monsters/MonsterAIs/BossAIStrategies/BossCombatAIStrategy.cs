using Shared.DataTables;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies;
using Z.GameClients.Stages.CombatSystems;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.BossAIStrategies
{
    public enum MovementType
    {
        MoveToTarget,
        MaintainDistanceFromTarget,
        NotMove
    }

    public abstract class BossCombatAIStrategy : MonsterAIStrategyBase
    {
        protected delegate void Action(Stage stage, Monster owner, Character target);

        protected abstract BossIdleAIStrategy IdleAIStategy { get; }
        protected abstract List<Action> Actions { get; }
        protected abstract float ActionCoolTime { get; }
        protected abstract MovementType MovementType { get; }

        private Action _previousAction;
        private Action _currentAction;
        private float _actionCoolTimeEndsAt;

        private Vector2 _moveDirection;
        private float _setMoveDirectionAt;

        public override void Begin(Stage stage, Monster owner)
        {
            _previousAction = null;
            _currentAction = null;
            _actionCoolTimeEndsAt = float.MinValue;

            _moveDirection = Vector2.zero;
            _setMoveDirectionAt = float.MinValue;
        }

        public override MonsterAIStrategyBase Update(Stage stage, Monster owner)
        {
            if (!owner.Action.IsIdle)
            {
                return null;
            }

            float now = Time.time;

            if (_currentAction != null)
            {
                _previousAction = _currentAction;
                _currentAction = null;
                _actionCoolTimeEndsAt = now + ActionCoolTime;
            }

            var target = stage.FindClosestCharacter(
                allianceType: owner.Alliance.ToEnemyAlliance(),
                position: owner.Pos,
                limitDistance: GameConstants.AI_TARGET_SEARCH_MAX_DISTANCE,
                condition: character => !character.Action.IsDead && !character.IsImmuneToHit);

            if (target == null)
            {
                return IdleAIStategy;
            }

            if (now < _actionCoolTimeEndsAt)
            {
                this.Move(owner, target, now);
                return null;
            }

            _currentAction = this.GetRandomAction();
            _currentAction.Invoke(stage, owner, target);
            _actionCoolTimeEndsAt = now + owner.Action.CurrentAction.Duration + ActionCoolTime;

            return null;
        }

        private void Move(Monster owner, Character target, float now)
        {
            switch (MovementType)
            {
                case MovementType.MoveToTarget:
                    {
                        _moveDirection = target.Pos - owner.Pos;
                        this.OwnerMove(owner, _moveDirection);
                        break;
                    }
                case MovementType.MaintainDistanceFromTarget:
                    {
                        if (_setMoveDirectionAt < now)
                        {
                            _moveDirection = target.Pos + ActionCoolTime * owner.Stats.MoveSpeed.Value * ((owner.Pos - target.Pos).normalized + 0.5f * Random.insideUnitCircle) - owner.Pos;
                            _setMoveDirectionAt = now + 0.5f * ActionCoolTime;
                        }
                        this.OwnerMove(owner, _moveDirection);
                        break;
                    }
                case MovementType.NotMove:
                    {
                        break;
                    }
                default:
                    {
                        throw new System.NotImplementedException($"이동 유형 {MovementType}이 구현되지 않았습니다.");
                    }
            }
        }

        private Action GetRandomAction()
        {
            Debug.Assert(Actions.Count > 0);
            bool excludePreviousAction = Actions.Count > 2 && _previousAction != null;

            if (excludePreviousAction)
            {
                Actions.Remove(_previousAction);
            }

            Action randomAction = Actions[Random.Range(0, Actions.Count)];
            Debug.Assert(randomAction != null);

            if (excludePreviousAction)
            {
                Actions.Add(_previousAction);
            }

            return randomAction;
        }

        public override void End(Stage stage, Monster owner)
        {

        }

        public override MonsterAIStrategyBase OnHitted(Stage stage, Monster owner, Character attacker)
        {
            return null;
        }

        protected virtual void OwnerMove(Monster owner, Vector2 moveDirection)
        {
            owner.Move(_moveDirection);
        }
    }
}
