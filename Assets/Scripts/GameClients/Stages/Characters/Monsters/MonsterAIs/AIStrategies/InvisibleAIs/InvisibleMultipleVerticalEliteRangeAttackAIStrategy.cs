using Shared.DataTables;
using UnityEngine;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.InvisibleAIs
{
    public class InvisibleMultipleVerticalEliteRangeAttackIdleAIStrategy : BasicIdleAIStrategy
    {
        protected override MonsterAIStrategyBase CombatAIStrategy => new InvisibleMultipleVerticalEliteRangeAttackCombatAIStrategy();
        protected override float WaitingTime => 2.0f;
    }

    public class InvisibleMultipleVerticalEliteRangeAttackCombatAIStrategy : MonsterAIStrategyBase
    {
        private static readonly float TARGET_CHECK_PERIOD = 1.0f;

        private Character _target;
        private float _findTargetAt;
        private float _attackDelay;

        private int _projectileAmount;
        private float _firePeriod;
        private bool _isRemovableBySpinBladeObject;

        private float _invisibleAt;
        private static readonly float InvisibleDuration = 2.5f;
        private static readonly float InvisibleCoolTime = 2.5f;

        public override void Begin(Stage stage, Monster owner)
        {
            _findTargetAt = 0.0f;
            _invisibleAt = 0.0f;

            _attackDelay = 1.0f / owner.Stats.CharacterAttackSpeed.Value;
            _projectileAmount = (int)owner.StaticData.Param1;
            _firePeriod = owner.StaticData.Param2;
            _isRemovableBySpinBladeObject = false;

        }

        public override MonsterAIStrategyBase Update(Stage stage, Monster owner)
        {
            var now = Time.time;
            //Attack 여부와 상관없이 투명 효과는 적용되어야 한다.
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
                if (enemy != null)
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
                return new InvisibleMultipleVerticalEliteRangeAttackIdleAIStrategy();
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
            return null;
        }
    }

}
