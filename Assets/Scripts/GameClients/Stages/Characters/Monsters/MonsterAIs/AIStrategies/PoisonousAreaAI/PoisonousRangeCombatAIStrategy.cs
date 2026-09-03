using Shared.DataTables;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.MeleeAIs;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.RangeAIs
{
    public class PoisonousRangeCombatAIStrategy : MonsterAIStrategyBase
    {
        // 내가 지금 공격할 대상
        private Character target;

        private Vector2 vectorToDestination;
        private float timeAfterSetDestination;

        private float randomAttackDelay;

        private float _findTargetCheckAt;
        private readonly static float _findTargetCheckDuration = 1.0f;

        private float _poisonousAreaEffectLifeTime;
        private float _poisonousAreaEffectRadius;
        private float _projectileLifeTime;

        public PoisonousRangeCombatAIStrategy(Character target)
        {
            this.target = target;
            this.vectorToDestination = target.Pos;
            this.timeAfterSetDestination = 9999f;
            this.randomAttackDelay = Random.Range(0.01f, 2.0f);

            this.target = target;
            _findTargetCheckAt = Time.time;
        }

        public override void Begin(Stage stage, Monster owner)
        {
            _poisonousAreaEffectLifeTime = owner.StaticData.Param1;
            _poisonousAreaEffectRadius = owner.StaticData.Param2;
            _projectileLifeTime = owner.StaticData.Param3;
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
                    condition: character => !character.Action.IsDead);
                if (this.target != enemy)
                {
                    return enemy ? new PoisonousRangeCombatAIStrategy(enemy) : new PoisonousRangeIdleAIStrategy();
                }

                _findTargetCheckAt = _findTargetCheckDuration + now;
            }

            if (!this.target || this.target.IsImmuneToHit)
            {
                return new PoisonousRangeIdleAIStrategy();
            }

            var vectorToTarget = this.target.Pos - owner.Pos;

            var squaredRangeAttackEffectiveRange = owner.StaticData.SpecialAttack1EffectiveRange;
            squaredRangeAttackEffectiveRange *= squaredRangeAttackEffectiveRange;

            var squaredDistanceToTarget = vectorToTarget.sqrMagnitude;
            if (squaredDistanceToTarget < squaredRangeAttackEffectiveRange)
            {
                if (owner.IsAbleToRangeAttackNow(now))
                {
                    this.randomAttackDelay -= Time.deltaTime;
                    if (this.randomAttackDelay <= 0f)
                    {
                        if (owner.TryPoisonousRangeAttack(stage, this.target, _poisonousAreaEffectLifeTime, _poisonousAreaEffectRadius, _projectileLifeTime))
                        {
                            this.randomAttackDelay = Random.Range(0.01f, 2.0f);
                        }
                        return null;
                    }
                }
            }

            if (owner.Action.IsAttacking)
            {
                return null;
            }

            this.timeAfterSetDestination += Time.deltaTime;
            if (this.timeAfterSetDestination > 2.5f)
            {
                this.timeAfterSetDestination = 0f;
                // 원거리 공격 가능한 적절히 가까운 위치를 유지한다.
                float offset = owner.StaticData.SpecialAttack1EffectiveRange * 0.4f;
                this.vectorToDestination = vectorToTarget + (vectorToTarget.normalized * -offset) + new Vector2(Random.Range(-offset, offset), Random.Range(-offset, offset));
            }
            owner.Move(this.vectorToDestination);

            return null;
        }

        public override void End(Stage stage, Monster owner)
        {

        }

        public override MonsterAIStrategyBase OnHitted(Stage stage, Monster owner, Character attacker)
        {
            if (null != attacker && this.target != attacker)
            {
                return new PoisonousRangeCombatAIStrategy(attacker);
            }

            return null;
        }

    }

}