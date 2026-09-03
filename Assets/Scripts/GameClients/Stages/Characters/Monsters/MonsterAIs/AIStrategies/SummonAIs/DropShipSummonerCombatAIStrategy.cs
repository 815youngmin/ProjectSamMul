using Shared.DataTables;
using UnityEngine;
using Z.GameClients.Stages.CombatSystems;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies
{
    public class DropShipSummonerCombatAIStrategy : MonsterAIStrategyBase
    {
        private Character _target;
        private float _findTargetAt;
        private float _summonAt;

        private readonly static float TARGET_CHECK_PERIOD = 1.0f;
        private readonly static float LifeTime = 60f;
        private float SUMMON_PERIOD;
        private readonly static float SummonDuration = 1.0f;
        private float _createAt;

        public DropShipSummonerCombatAIStrategy(Character target, float createAt)
        {
            _target = target;
            _createAt = createAt;
        }

        public override void Begin(Stage stage, Monster owner)
        {
            _findTargetAt = 0.0f;
            _summonAt = Time.time;
            SUMMON_PERIOD = 1.0f /  owner.Stats.CharacterAttackSpeed.Value;

        }

        public override MonsterAIStrategyBase OnHitted(Stage stage, Monster owner, Character attacker)
        {
            return null;
        }

        public override MonsterAIStrategyBase Update(Stage stage, Monster owner)
        {
            var now = Time.time;
            if (!owner.Action.IsIdle)
            {
                return null;
            }

            if (_createAt + LifeTime <= now)
            {
                owner.DisappearFromStage(stage, withDeadEffect: false);
                return null;
            }

            if (_findTargetAt < now)
            {
                _findTargetAt = now + TARGET_CHECK_PERIOD;
                _target = stage.FindClosestCharacter(
                    owner.Alliance.ToEnemyAlliance(),
                    owner.Pos,
                    limitDistance: GameConstants.AI_TARGET_SEARCH_MAX_DISTANCE,
                    condition: character => !character.Action.IsDead && !character.IsImmuneToHit);
            }

            if (_target == null || _target.IsImmuneToHit)
            {
                return new DropShipSummonerIdleAIStrategy(_createAt);
            }

            if (_summonAt + SUMMON_PERIOD < now )
            {
                _summonAt = now;
                owner.DoDropshipSummonAction(stage, owner, _target, SummonDuration);
                return null;
            }

            owner.Move(_target.Pos - owner.Pos);
            return null;
        }

        public override void End(Stage stage, Monster owner)
        {

        }
    }

}

