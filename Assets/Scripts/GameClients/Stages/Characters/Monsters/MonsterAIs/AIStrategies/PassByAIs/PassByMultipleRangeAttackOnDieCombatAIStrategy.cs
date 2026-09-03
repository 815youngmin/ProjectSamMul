using DG.Tweening;
using Shared.DataTables;
using Shared.GameDataTypes;
using System;
using UnityEngine;
using Z.GameClients.Stages.CombatSystems;
using Z.GameClients.Stages.StageEvents;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.PassByAIs
{
    public class PassByMultipleRangeAttackOnDieCombatAIStrategy : MonsterAIStrategyBase
    {
        // 내가 지금 공격할 대상
        private readonly Character _target;
        private Vector2 _direction;
        private float _findTargetCheckAt;
        private float _repositionCheckAt;

        private int _projectileAmount;
        private float _rangeAttackDelay;

        private readonly static float _findTargetCheckDuration = 1.0f;
        private readonly static float _repositionCheckDuration = 1.0f;

        public PassByMultipleRangeAttackOnDieCombatAIStrategy(Character target)
        {
            _target = target;
            _findTargetCheckAt = Time.time;
            _repositionCheckAt = Time.time;

        }

        public override void Begin(Stage stage, Monster owner)
        {
            _direction = (_target.Pos - owner.Pos).normalized;

            _projectileAmount = (int)owner.StaticData.Param1;
            _rangeAttackDelay = owner.StaticData.Param2;

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
                    return enemy ? new PassByMultipleRangeAttackOnDieCombatAIStrategy(enemy) : new PassByMultipleRangeAttackOnDieIdleAIStrategy();
                }

                _findTargetCheckAt = now + _findTargetCheckDuration;
            }

            if (_target.Action.IsDead)
            {
                return new PassByMultipleRangeAttackOnDieIdleAIStrategy();
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
        private static readonly float ProjectileSpeed = 10;
        private static readonly float ProjectileAcceleration = 0.0f;
        private static readonly float ProjectileRadius = 0.5f;
        private static readonly float ProjectileAliveDistance = 100;
        private static readonly float ProjectileKnobackPower = 0.1f;

        public override MonsterAIStrategyBase OnDead(Stage stage, Monster owner)
        {
            if (_target != null)
            {
                var rangeAttackSequence = DOTween.Sequence();
                rangeAttackSequence.AppendInterval(_rangeAttackDelay);
                rangeAttackSequence.AppendCallback(() =>
                {
                    for (int i = 0; i < _projectileAmount; i++)
                    {
                        Vector2 dir = Quaternion.Euler(0.0f, 0.0f, 360f / _projectileAmount * i) * Vector2.up;
                        stage.CreateProjectile(
                             owner.StaticData.SpecialAttack1ResourcePath,
                             owner.Alliance,
                             owner,
                             owner.RangeAttackPower,
                             ProjectileKnobackPower,
                             owner.CenterPos,
                             dir,
                             ProjectileSpeed,
                             ProjectileAcceleration,
                             ProjectileRadius,
                             ProjectileAliveDistance,
                             hitChances: 1,
                             splitCount: 0,
                             isRemovableBySpinBladeObject: true,
                             hitSoundPrefabPath: string.Empty
                             );
                    }
                });
            }
            return null;
        }
    }
}