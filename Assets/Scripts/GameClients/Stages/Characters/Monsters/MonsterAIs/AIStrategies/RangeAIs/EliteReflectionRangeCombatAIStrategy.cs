using Shared.DataTables;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.RangeAIs;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies
{
    public class EliteReflectionRangeCombatAIStrategy : MonsterAIStrategyBase
    { 
        // 내가 지금 공격할 대상
        private Character target;

        private float _attackDelay;

        private float _findTargetCheckAt;
        private readonly static float AttackDelay = 4f;
        private readonly static float FindTargetCheckDuration = 1.0f;

        private EliteReflectionRangeAttackAIBlackboard Blackboard => (EliteReflectionRangeAttackAIBlackboard)base._blackboard;


        public EliteReflectionRangeCombatAIStrategy(Character target, EliteReflectionRangeAttackAIBlackboard blackboard) 
            : base(blackboard)
        {
            this.target = target;
            this._attackDelay = AttackDelay;

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
                    return enemy ? new EliteReflectionRangeCombatAIStrategy(enemy, Blackboard) : new EliteReflectionRangeIdleAIStrategy(Blackboard);
                }

                _findTargetCheckAt = FindTargetCheckDuration + now;
            }

            if (!this.target || this.target.IsImmuneToHit)
            {
                return new EliteReflectionRangeIdleAIStrategy(Blackboard);
            }

            var vectorToTarget = this.target.Pos - owner.Pos;

            var squaredRangeAttackEffectiveRange = owner.StaticData.SpecialAttack1EffectiveRange;
            squaredRangeAttackEffectiveRange *= squaredRangeAttackEffectiveRange;

            var squaredDistanceToTarget = vectorToTarget.sqrMagnitude;
            if (squaredDistanceToTarget < squaredRangeAttackEffectiveRange)
            {
                if (owner.IsAbleToRangeAttackNow(now))
                {
                    this._attackDelay -= Time.deltaTime;
                    if (this._attackDelay <= 0f)
                    {
                        if (owner.TryMultipleReflectionRangeAttack(stage, this.target, Blackboard.projectileAmount, Blackboard.projectileSpeed, Blackboard.projectileLifeTime, Blackboard.projectileRadius,Blackboard.projectileRotateSpeed, Blackboard.projectilePrefabPath))
                        {
                            this._attackDelay = AttackDelay;
                        }
                        return null;
                    }
                }
            }

            if (owner.Action.IsAttacking)
            {
                return null;
            }

            owner.Move(vectorToTarget);

            return null;
        }

        public override void End(Stage stage, Monster owner)
        {

        }

        public override MonsterAIStrategyBase OnHitted(Stage stage, Monster owner, Character attacker)
        {
            if (null != attacker && this.target != attacker)
            {
                return new EliteReflectionRangeCombatAIStrategy(attacker, Blackboard);
            }

            return null;
        }
    }
}
