using Shared.DataTables;
using UnityEngine;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.AreaEffectOnDieAIs
{
    public class AreaEffectOnDieCombatAIStrategy : AreaEffectOnDieBaseAIStrategy
    {
        // 내가 지금 공격할 대상
        private Character target;
        private float _findTargetCheckAt;
        private readonly static float _findTargetCheckDuration = 1.0f;

        public AreaEffectOnDieCombatAIStrategy(Character target) 
            : base() 
        {
            this.target = target;
            _findTargetCheckAt = Time.time;
        }

        public override void Begin(Stage stage, Monster owner)
        {
        }

        public override MonsterAIStrategyBase Update(Stage stage, Monster owner)
        {
            var now = Time.time;

            if (_findTargetCheckAt < now)
            {
                Character enemy = stage.FindClosestCharacter(
                    owner.Alliance.ToEnemyAlliance(),
                    owner.Pos,
                    limitDistance: GameConstants.AI_TARGET_SEARCH_MAX_DISTANCE,
                    condition: character => !character.Action.IsDead && !character.IsImmuneToHit);
                if (this.target != enemy)
                {
                    return enemy ? new AreaEffectOnDieCombatAIStrategy(enemy) : new AreaEffectOnDieIdleAIStrategy();
                }

                _findTargetCheckAt = _findTargetCheckDuration + now;
            }

            if (this.target.IsImmuneToHit ||
                this.target.Action.IsDead)
            {
                return new AreaEffectOnDieIdleAIStrategy();
            }

            owner.Move(this.target.Pos - owner.Pos);

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

            if (attacker != null && this.target != attacker)
            {
                return new AreaEffectOnDieCombatAIStrategy(target);
            }

            return null;
        }

        public override MonsterAIStrategyBase OnDead(Stage stage, Monster owner)
        {
            _ = base.OnDead(stage, owner);

            this.CreateAreaEffect(stage, owner);

            return null;
        }
    }
}
