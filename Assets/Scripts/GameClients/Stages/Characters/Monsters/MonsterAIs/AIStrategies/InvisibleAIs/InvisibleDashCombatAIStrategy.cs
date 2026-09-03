using Shared.DataTables;
using UnityEngine;
using Z.GameClients.Stages.CombatSystems;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.InvisibleAIs
{
    public class InvisibleDashCombatAIStrategy : MonsterAIStrategyBase
    {
        private static readonly float TARGET_CHECK_PERIOD = 1.0f;
        private static readonly float DASH_PRE_DELAY = 1.0f;
        private static readonly float DASH_POST_DELAY = 1.0f;
        private static readonly float DASH_COOL_TIME = 3.0f;

        private Character _target;
        private float _findTargetAt;
        private float _dashEndedAt;
        private bool _isDashing;

        private float _invisibleAt;
        private static readonly float InvisibleDuration = 2.5f;
        private static readonly float InvisibleCoolTime = 2.5f;

        public InvisibleDashCombatAIStrategy(Character target) : base()
        {
            _target = target;
        }

        public override void Begin(Stage stage, Monster owner)
        {
            _findTargetAt = 0.0f;
            _dashEndedAt = 0.0f;
            _invisibleAt = 0.0f;
            _isDashing = false;
        }

        public override MonsterAIStrategyBase Update(Stage stage, Monster owner)
        {
            var now = Time.time;
            //대쉬 여부와 상관없이 투명 효과는 적용되어야 한다.
            if(_invisibleAt + InvisibleDuration + InvisibleCoolTime < now)
            {
                _invisibleAt = now;
                owner.StatusEffects.AddOrUpdateStatusEffect(stage, owner, StatusEffects.StatusEffectType.Invisible, InvisibleDuration, now, 0f);
            }

            //대쉬중이면 return
            if (!owner.Action.IsIdle)
            {
                return null;
            }

            if (_isDashing)
            {
                _dashEndedAt = now;
                _isDashing = false;
            }

            if (_findTargetAt < now)
            {
                _findTargetAt = now + TARGET_CHECK_PERIOD;
                _target = stage.FindClosestCharacter(
                    allianceType: owner.Alliance.ToEnemyAlliance(),
                    position: owner.Pos,
                    limitDistance: GameConstants.AI_TARGET_SEARCH_MAX_DISTANCE,
                    condition: character => !character.Action.IsDead && !character.IsImmuneToHit);
            }

            if (_target == null)
            {
                return new InvisibleDashIdleAIStrategy();
            }

            if (_dashEndedAt + DASH_COOL_TIME < now && !_isDashing)
            {
                owner.DoDashAction(stage, _target, owner.StaticData.Param1, DASH_PRE_DELAY, owner.StaticData.Param2, DASH_POST_DELAY);
                _isDashing = true;
            }
            else
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
            return null;
        }
    }
}
