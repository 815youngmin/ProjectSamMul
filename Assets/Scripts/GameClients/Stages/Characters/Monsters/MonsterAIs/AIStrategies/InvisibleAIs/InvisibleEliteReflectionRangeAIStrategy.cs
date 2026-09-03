using Shared.DataTables;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.RangeAIs;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.InvisibleAIs
{
    public class InvisibleEliteReflectionRangeIdleAIStrategy : BasicIdleAIStrategy
    {
        protected override MonsterAIStrategyBase CombatAIStrategy => new InvisibleEliteReflectionRangeCombatAIStrategy(Blackboard);
        protected override float WaitingTime => 2.0f;

        public InvisibleEliteReflectionRangeIdleAIStrategy(EliteReflectionRangeAttackAIBlackboard blackboard)
        : base(blackboard)
        { }

        private EliteReflectionRangeAttackAIBlackboard Blackboard => (EliteReflectionRangeAttackAIBlackboard)base._blackboard;
    }

    public class InvisibleEliteReflectionRangeCombatAIStrategy : MonsterAIStrategyBase
    {
        private static readonly float TARGET_CHECK_PERIOD = 1.0f;
        private static readonly float AttackDelay = 4f;

        private Character _target;
        private float _findTargetAt;
        private float _attackDelay;

        private float _invisibleAt;
        private static readonly float InvisibleDuration = 2.5f;
        private static readonly float InvisibleCoolTime = 2.5f;
        private EliteReflectionRangeAttackAIBlackboard Blackboard => (EliteReflectionRangeAttackAIBlackboard)base._blackboard;


        public InvisibleEliteReflectionRangeCombatAIStrategy(EliteReflectionRangeAttackAIBlackboard blackboard)
        : base(blackboard)
        { }

        public override void Begin(Stage stage, Monster owner)
        {
            _findTargetAt = 0.0f;
            _invisibleAt = 0.0f;
        }

        public override MonsterAIStrategyBase Update(Stage stage, Monster owner)
        {
            var now = Time.time;
            //대쉬 여부와 상관없이 투명 효과는 적용되어야 한다.
            if (_invisibleAt + InvisibleDuration + InvisibleCoolTime < now)
            {
                _invisibleAt = now;
                owner.StatusEffects.AddOrUpdateStatusEffect(stage, owner, StatusEffects.StatusEffectType.Invisible, InvisibleDuration, now, 0f);
            }


            if (_findTargetAt < now)
            {
                Character enemy = stage.FindClosestCharacter(
                    owner.Alliance.ToEnemyAlliance(),
                    owner.Pos,
                    limitDistance: GameConstants.AI_TARGET_SEARCH_MAX_DISTANCE,
                    condition: character => !character.Action.IsDead);

                if(enemy != null)
                {
                    _target = enemy;
                }
                else
                {
                    _target = null;
                }

                _findTargetAt = TARGET_CHECK_PERIOD + now;
            }

            if (!this._target || this._target.IsImmuneToHit)
            {
                return new InvisibleEliteReflectionRangeIdleAIStrategy(Blackboard);
            }

            var vectorToTarget = this._target.Pos - owner.Pos;

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
                        if (owner.TryMultipleReflectionRangeAttack(stage, this._target, 
                            Blackboard.projectileAmount, Blackboard.projectileSpeed, Blackboard.projectileLifeTime, Blackboard.projectileRadius, Blackboard.projectileRotateSpeed, Blackboard.projectilePrefabPath))
                        {
                            _attackDelay = AttackDelay;
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
            return null;
        }
    }

}
