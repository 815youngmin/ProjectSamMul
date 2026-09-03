using Shared.DataTables;
using UnityEngine;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.AreaEffectOnDieAIs
{
    public class AreaEffectOnDieIdleAIStrategy : AreaEffectOnDieBaseAIStrategy
    {
        Character _target;
        private float _findTargetCheckAt;
        private readonly static float _findTargetCheckDuration = 1.0f;

        public AreaEffectOnDieIdleAIStrategy(): base ()
        {
        }

        public override void Begin(Stage stage, Monster owner)
        {
            owner.StopMovement();
            _findTargetCheckAt = 0.0f;
        }

        public override MonsterAIStrategyBase Update(Stage stage, Monster owner)
        {
            float now = Time.time;
            if (_findTargetCheckAt < now)
            {
                _findTargetCheckAt = now + _findTargetCheckDuration;
                Character enemy = stage.FindClosestCharacter(
                    owner.Alliance.ToEnemyAlliance(),
                    owner.Pos,
                    limitDistance: GameConstants.AI_TARGET_SEARCH_MAX_DISTANCE,
                    condition: character => !character.Action.IsDead && !character.IsImmuneToHit);
                _target = enemy;
            }

            if (IsEnemyInSearchDistance(owner, _target))
            {
                return new AreaEffectOnDieCombatAIStrategy(_target);
            }

            return null;
        }

        public override void End(Stage stage, Monster owner)
        {

        }

        public override MonsterAIStrategyBase OnHitted(Stage stage, Monster owner, Character attacker)
        {
            if (owner.Action.IsDead)
            {
                this.CreateAreaEffect(stage, owner);
                return null;
            }

            if (null == attacker)
            {
                return null;
            }

            // 전투 전략으로 전환 
            return new AreaEffectOnDieCombatAIStrategy(attacker);
        }

        public override MonsterAIStrategyBase OnDead(Stage stage, Monster owner)
        {
            _ = base.OnDead(stage, owner);

            this.CreateAreaEffect(stage, owner);

            return null;
        }
    }
}
