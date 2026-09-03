using Shared.DataTables;
using Shared.GameDataTypes;
using System;
using UnityEngine;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.GameClients.Stages.StageEvents;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.PassByAIs
{
    public class PassByCombatAIStrategy : MonsterAIStrategyBase
    {
        // 내가 지금 공격할 대상
        private readonly Character _target;
        private Vector2 _direction;
        private float _findTargetCheckAt;
        private float _repositionCheckAt;

        private readonly static float _findTargetCheckDuration = 1.0f;
        private readonly static float _repositionCheckDuration = 1.0f;

        public PassByCombatAIStrategy(Character target)
        {
            _target = target;
            _findTargetCheckAt = Time.time;
            _repositionCheckAt = Time.time;
        }

        public override void Begin(Stage stage, Monster owner)
        {
            _direction = (_target.Pos - owner.Pos).normalized;
        }

        public override MonsterAIStrategyBase Update(Stage stage, Monster owner)
        {
            var now = Time.time;

            if (owner.Action.IsDead)
            {
                return null;
            }

            if (_findTargetCheckAt < now)
            {
                Character enemy = stage.FindClosestCharacter(
                    owner.Alliance.ToEnemyAlliance(),
                    owner.Pos,
                    limitDistance: GameConstants.AI_TARGET_SEARCH_MAX_DISTANCE,
                    condition: character => !character.Action.IsDead);
                if (_target != enemy)
                {
                    return enemy ? new PassByCombatAIStrategy(enemy) : new PassByIdleAIStrategy();
                }

                _findTargetCheckAt = now + _findTargetCheckDuration;
            }

            if (_target.Action.IsDead)
            {
                return new PassByIdleAIStrategy();
            }

            if (_repositionCheckAt < now && stage.StaticData.StageFormType == StageFormType.Rectangle)
            {
                var walkableArea = stage.StaticData.GetWalkableArea();
                walkableArea.max *= 0.9f;
                walkableArea.min *= 0.9f;

                if (!walkableArea.Contains(owner.Pos))
                {
                    var (nearRadius, maxRadius) = MonsterSpawnTools.CalculateSpawnRadius(GameClient.CameraController.OrthographicSize, stage.StaticData.StageFormType);
                    var spawnBoundary = MonsterSpawnTools.CalculateSpawnBoundary(stage, maxRadius, maxRadius);

                    owner.transform.position = MonsterSpawnTools.PickRandomPointFromAnnulusWithinBoundary(spawnBoundary, nearRadius, maxRadius, _target.Pos);
                    _direction = (_target.Pos - owner.Pos).normalized;
                }

                _repositionCheckAt = now + _repositionCheckDuration;
            }

            // 한방향으로 날아간다.
            owner.Move(_direction);

            return null;
        }

        public override void End(Stage stage, Monster owner)
        {

        }

        public override MonsterAIStrategyBase OnHitted(Stage stage, Monster owner, Character attacker)
        {
            // DO Nothing
            return null;
        }

    }
}