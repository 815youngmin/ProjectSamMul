using Shared.DataTables;
using Shared.GameDataTypes;
using UnityEngine;
using Z.GameClients.Stages.Characters.Stats;
using Z.GameClients.Stages.CombatSystems;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.PassByAIs
{
    public class PassByFastAndNormalMoveCombatAIStrategy : MonsterAIStrategyBase
    {
        // 내가 지금 공격할 대상
        private readonly Character _target;
        private Vector2 _direction;
        private float _findTargetCheckAt;

        private float _normalMoveAt;
        private float _fastMoveAt;

        private readonly static float _findTargetCheckDuration = 1.0f;

        private static readonly float MOVE_TIME = 3f;
        private static readonly float FAST_TIME = 1f;

        private StatModifier _fastMoveStat;

        public PassByFastAndNormalMoveCombatAIStrategy(Character target)
        {
            _target = target;
            _findTargetCheckAt = Time.time;

            _normalMoveAt = Time.time;
            _fastMoveAt = float.MaxValue;
            _fastMoveStat = new StatModifier(1.5f, StatModType.PercentAdd);
        }

        public override void Begin(Stage stage, Monster owner)
        {
            _direction = (_target.Pos - owner.Pos).normalized;
        }
        public override MonsterAIStrategyBase Update(Stage stage, Monster owner)
        {
            var now = Time.time;

            if (owner.Action.IsDead)
            {
                return null;
            }

            if (_findTargetCheckAt < now)
            {
                Character enemy = stage.FindClosestCharacter(
                    owner.Alliance.ToEnemyAlliance(),
                    owner.Pos,
                    limitDistance: GameConstants.AI_TARGET_SEARCH_MAX_DISTANCE,
                    condition: character => !character.Action.IsDead);
                if (_target != enemy)
                {
                    return enemy ? new PassByFastAndNormalMoveCombatAIStrategy(enemy) : new PassByFastAndNormalMoveIdleAIStrategy();
                }

                _findTargetCheckAt = now + _findTargetCheckDuration;
            }

            if (_target.Action.IsDead)
            {
                return new PassByFastAndNormalMoveIdleAIStrategy();
            }
            if (_fastMoveAt < now)
            {
                _normalMoveAt = now + FAST_TIME;
                _fastMoveAt = float.MaxValue;
                owner.Stats.MoveSpeed.AddModifier(_fastMoveStat);
            }
            else if (_normalMoveAt < now)
            {
                _normalMoveAt = float.MaxValue;
                _fastMoveAt = now + MOVE_TIME;
                owner.Stats.MoveSpeed.RemoveModifier(_fastMoveStat);
            }

            // 한방향으로 나아간다.
            owner.Move(_direction);

            return null;
        }

        public override void End(Stage stage, Monster owner)
        {
            owner.Stats.MoveSpeed.RemoveModifier(_fastMoveStat);
            _fastMoveStat = null;
        }

        public override MonsterAIStrategyBase OnHitted(Stage stage, Monster owner, Character attacker)
        {
            // DO Nothing
            return null;
        }
        public override MonsterAIStrategyBase OnRePosition(Stage stage, Monster owner)
        {
            owner.DisappearFromStage(stage, withDeadEffect: false);
            return null;
        }
    }
}