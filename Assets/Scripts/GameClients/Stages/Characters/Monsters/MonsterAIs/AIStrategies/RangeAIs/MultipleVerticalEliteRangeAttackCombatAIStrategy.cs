using Shared.DataTables;
using UnityEngine;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies
{
    public class MultipleVerticalEliteRangeAttackCombatAIStrategy : MonsterAIStrategyBase
    {
        // 내가 지금 공격할 대상
        private Character _target;

        private float _attackDelay;

        private float _findTargetCheckAt;
        private readonly static float _findTargetCheckDuration = 1.0f;

        private int _projectileAmount;
        private float _firePeriod;
        private bool _isRemovableBySpinBladeObject;

        public MultipleVerticalEliteRangeAttackCombatAIStrategy(Character target)
        {
            _target = target;
            _findTargetCheckAt = Time.time;
        }

        public override void Begin(Stage stage, Monster owner)
        {
            _attackDelay = 1.0f / owner.Stats.CharacterAttackSpeed.Value;
            _projectileAmount = (int)owner.StaticData.Param1;
            _firePeriod = owner.StaticData.Param2;
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
                    return enemy ? new MultipleVerticalEliteRangeAttackCombatAIStrategy(enemy) : new MultipleVerticalEliteRangeAttackIdleAIStrategy();
                }

                _findTargetCheckAt = _findTargetCheckDuration + now;
            }

            if (!this._target || this._target.IsImmuneToHit)
            {
                return new MultipleVerticalEliteRangeAttackIdleAIStrategy();
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
                        if (owner.TryMultipleVerticalRangeAttack(stage, _target, _projectileAmount, _firePeriod, _isRemovableBySpinBladeObject, withIndicator: false))
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
                return new MultipleVerticalEliteRangeAttackCombatAIStrategy(attacker);
            }

            return null;
        }
    }
}

