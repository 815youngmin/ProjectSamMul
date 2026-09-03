using Shared.DataTables;
using UnityEngine;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.SummonOnDieAIs
{
    public class MeleeSummonOnDieCombatAIStrategy : SummonOnDieMonsterAIStrategyBase
    {
        private static readonly float TARGET_CHECK_PERIOD = 1.0f;

        private Character _target;
        private float _findTargetAt;

        public MeleeSummonOnDieCombatAIStrategy(Character target) : base()
        {
            _target = target;
        }

        public override void Begin(Stage stage, Monster owner)
        {
            _findTargetAt = 0.0f;
        }

        public override MonsterAIStrategyBase Update(Stage stage, Monster owner)
        {
            var now = Time.time;

            if (_findTargetAt < now)
            {
                _findTargetAt = now + TARGET_CHECK_PERIOD;
                _target = stage.FindClosestCharacter(
                    allianceType: owner.Alliance.ToEnemyAlliance(),
                    position: owner.Pos,
                    limitDistance: GameConstants.AI_TARGET_SEARCH_MAX_DISTANCE,
                    condition: character => !character.Action.IsDead && !character.IsImmuneToHit);
            }

            if(_target == null)
            {
               return new MeleeSummonOnDieIdleAIStrategy();
            }

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
            if (attacker != null && _target != attacker)
            {
                _target = attacker;
            }
            return null;
        }

        public override MonsterAIStrategyBase OnDead(Stage stage, Monster owner)
        {
            this.Summon(stage, owner);
            return null;
        }

    }
}
