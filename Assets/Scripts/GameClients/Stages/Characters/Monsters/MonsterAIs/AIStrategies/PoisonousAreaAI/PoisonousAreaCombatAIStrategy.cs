using Shared.DataTables;
using UnityEngine;
using Z.GameClients.Stages.CombatSystems;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.PoisonousAreaAI
{
    public class PoisonousAreaCombatAIStrategy : PoisonousAreaBaseAIStrategy
    {
        private static readonly float TARGET_CHECK_PERIOD = 1.0f;

        // 내가 지금 공격할 대상
        private Character _target;
        private float _findTargetAt;

        public PoisonousAreaCombatAIStrategy(Character target)
        {
            _target = target;
            _findTargetAt = Time.time;
        }

        public override void Begin(Stage stage, Monster owner)
        {
        }

        public override MonsterAIStrategyBase Update(Stage stage, Monster owner)
        {
            var now = Time.time;

            if (_findTargetAt < now)
            {
                _target = stage.FindClosestCharacter(
                    allianceType: owner.Alliance.ToEnemyAlliance(),
                    position: owner.Pos,
                    limitDistance: GameConstants.AI_TARGET_SEARCH_MAX_DISTANCE,
                    condition: character => !character.Action.IsDead && !character.IsImmuneToHit);

                if (_target == null)
                {
                    return new PoisonousAreaIdleAIStrategy();
                }

                _findTargetAt = TARGET_CHECK_PERIOD + now;
            }

            owner.Move(_target.Pos - owner.Pos);

            return null;
        }

        public override void End(Stage stage, Monster owner)
        {

        }

        public override MonsterAIStrategyBase OnHitted(Stage stage, Monster owner, Character attacker)
        {
            if (owner.Action.IsDead)
            {
                return null;
            }

            if (attacker != null && _target != attacker)
            {
                _target = attacker;
            }

            return null;
        }

        public override MonsterAIStrategyBase OnDead(Stage stage, Monster owner)
        {
            _ = base.OnDead(stage, owner);

            this.CreatePoisonousAreaEffect(stage, owner);

            return null;
        }
    }
}
