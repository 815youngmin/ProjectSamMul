using Shared.DataTables;
using UnityEngine;
using UnityEngine.Profiling;
using Z.GameClients.Stages.CombatSystems;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.MeleeAIs
{
    public class MeleeCombatAIStrategy : MonsterAIStrategyBase
    {
        private static readonly float TARGET_CHECK_PERIOD = 1.0f;

        // 내가 지금 공격할 대상
        private Character _target;
        private float _findTargetAt;

        public MeleeCombatAIStrategy(Character target)
        {
            _target = target;
            _findTargetAt = Time.time;
        }

        public override void Begin(Stage stage, Monster owner)
        {

        }

        public override MonsterAIStrategyBase Update(Stage stage, Monster owner)
        {
            var now = Time.time;

#if USE_SCOPED_PROFILER
            using (new ScopedProfiler("MeleeCombatAI.Update.FindTarget"))
#endif
            {
                if (_findTargetAt < now)
                {
                    _target = stage.FindClosestCharacter(
                        allianceType: owner.Alliance.ToEnemyAlliance(),
                        position: owner.Pos,
                        limitDistance: GameConstants.AI_TARGET_SEARCH_MAX_DISTANCE,
                        condition: character => !character.Action.IsDead && !character.IsImmuneToHit);

                    if (_target == null)
                    {
                        return new MeleeIdleAIStrategy();
                    }

                    _findTargetAt = TARGET_CHECK_PERIOD + now;
                }
            }

#if USE_SCOPED_PROFILER
            using (new ScopedProfiler("MeleeCombatAI.Update.Move"))
#endif
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
            // NOTE: burn 데미지로 들어오는 경우 null이 들어옵니다.
            if (attacker != null && _target != attacker)
            {
                _target = attacker;
            }
            return null;
        }
    }
}