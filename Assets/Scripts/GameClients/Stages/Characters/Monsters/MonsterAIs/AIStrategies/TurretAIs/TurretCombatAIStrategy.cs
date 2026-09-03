using Shared.DataTables;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.MoveAndStopAIs;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies
{
    public class TurretCombatAIStrategy : MonsterAIStrategyBase
    {
        private Character _target;
        private float _findTargetAt;
        private float _fireAt;
        private float _startAngle;

        private static readonly float TARGET_CHECK_PERIOD = 1.0f;
        private static readonly float FIRE_PERIOD = 3f;
        private static readonly float ROTATE_ANGLE = 60f;


        public TurretCombatAIStrategy(Character target)
        {
            _target = target;
        }

        public override void Begin(Stage stage, Monster owner)
        {
            _findTargetAt = 0.0f;
            _fireAt = Time.time + FIRE_PERIOD;
            _startAngle = ROTATE_ANGLE;
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
                return new TurretIdleAIStrategy();
            }

            if(_fireAt <= now)
            {
                owner.DoTurretAttack(stage, Vector2.up, _startAngle);
                _startAngle += ROTATE_ANGLE;
                _fireAt = now + FIRE_PERIOD;

                if(_startAngle >= 360f)
                {
                    _startAngle -= 360f;
                }
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

;