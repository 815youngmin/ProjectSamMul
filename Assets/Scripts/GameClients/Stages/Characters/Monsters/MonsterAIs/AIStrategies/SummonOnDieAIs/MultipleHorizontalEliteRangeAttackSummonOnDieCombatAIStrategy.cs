using Shared.DataTables;
using UnityEngine;
using Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.SummonOnDieAIs;
using Z.GameClients.Stages.CombatSystems;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies
{
    public class MultipleHorizontalEliteRangeAttackSummonOnDieCombatAIStrategy : SummonOnDieMonsterAIStrategyBase
    {
        // 내가 지금 공격할 대상
        private Character _target;

        private float _attackDelay;

        private float _findTargetCheckAt;
        private readonly static float _findTargetCheckDuration = 1.0f;

        private int _projectileAmount;
        private float _fireAngle;
        private bool _isRemovableBySpinBladeObject;


        public MultipleHorizontalEliteRangeAttackSummonOnDieCombatAIStrategy(Character target)
        {
            this._target = target;
            _findTargetCheckAt = Time.time;
        }

        public override void Begin(Stage stage, Monster owner)
        {
            _attackDelay = 1.0f / owner.Stats.CharacterAttackSpeed.Value;
            _projectileAmount = (int)owner.StaticData.Param1;
            _fireAngle = owner.StaticData.Param2;
            _isRemovableBySpinBladeObject = false;
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
                if (this._target != enemy)
                {
                    return enemy ? new MultipleHorizontalEliteRangeAttackSummonOnDieCombatAIStrategy(enemy) : new MultipleHorizontalEliteRangeAttackSummonOnDieIdleAIStrategy();
                }

                _findTargetCheckAt = _findTargetCheckDuration + now;
            }

            if (!this._target || this._target.IsImmuneToHit)
            {
                return new MultipleHorizontalEliteRangeAttackSummonOnDieIdleAIStrategy();
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
                        if (owner.TryMultipleHorizontalRangeAttack(stage, _target, _projectileAmount, _fireAngle, _isRemovableBySpinBladeObject, withIndicator: false))
                        {
                            _attackDelay = 1.0f / owner.Stats.CharacterAttackSpeed.Value;
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
            if (null != attacker && this._target != attacker)
            {
                return new MultipleHorizontalEliteRangeAttackSummonOnDieCombatAIStrategy(attacker);
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

