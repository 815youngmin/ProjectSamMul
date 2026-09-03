using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;

namespace SamMul.GameClients.Stages.Characters.Actions.Boss.Western
{
    public class KidConcentratedFireAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string ReadyAnimationName = "PrepareSingleExecutionBegin2";
        private static readonly string WaitAnimationName = "PrepareSingleExecutionRepeat2";
        private static readonly string AttackAnimationName = "SingleExecutionAction2";
        private static readonly string EndAnimationName = "SingleExecutionEnd2";

        private static readonly float DAMAGE_COEFFICIENT = 1.0f;

        private Monster _owner;
        private Character _target;

        private Animation _readyAnimation;
        private Animation _waitAnimation;
        private Animation _attackAnimation;
        private Animation _endAnimation;

        private List<Vector2> _spawnPositions = new List<Vector2>();
        private List<Vector2> _fireDirections = new List<Vector2>();

        private static readonly int ProjectileAmount = 30;
        private static readonly string ProjectileBodyPath = "Stages/Projectiles/BulletSmallRadius0_2.prefab";
        private static readonly float ProjectileSpeed = 25f;
        private static readonly float ProjectileAcceleration = 1f;
        private static readonly float ProjectileRadius = 0.2f;
        private static readonly float ProjectileKnobackPower = 0.1f;
        private static readonly float ProjectileAliveDistance = 100;
        private static readonly float TargetRadius = 5f;
        private static readonly float SpawnMaxRandomRange = 15;
        private static readonly float SpawnMinRandomRange = 10;


        public override void Initialize(Monster owner, Character target, SpineMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
                animationController.FindAnimation(ReadyAnimationName).Duration +
                animationController.FindAnimation(WaitAnimationName).Duration +
                animationController.FindAnimation(AttackAnimationName).Duration +
                animationController.FindAnimation(EndAnimationName).Duration,
                animationController);

            _owner = owner;
            _target = target;

            _readyAnimation = AnimationController.FindAnimation(ReadyAnimationName);
            _waitAnimation = AnimationController.FindAnimation(WaitAnimationName);
            _attackAnimation = AnimationController.FindAnimation(AttackAnimationName);
            _endAnimation = AnimationController.FindAnimation(EndAnimationName);
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);
            AnimationController.SetAnimation(BodyAnimationTrack.WholeBody, _readyAnimation, false);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _waitAnimation, false, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _attackAnimation, false, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _endAnimation, false, 0f);

            var rect = stage.FenceRect != null ? stage.FenceRect.Value : stage.StaticData.GetWalkableArea();

            float maxDistance = Mathf.Sqrt(rect.width * rect.width + rect.height * rect.height);
            float hitTimeOnAnimation = _readyAnimation.Duration + _waitAnimation.Duration + AnimationController.FindHitTime(_attackAnimation);


            for (int i = 0; i < ProjectileAmount; ++i)
            {
                Vector2 targetPosition = new Vector2(Random.Range(rect.xMin, rect.xMax), Random.Range(rect.yMin, rect.yMax)) ;
                Vector2 spawnPosition = this.GetSpawnPosition(rect);

                Vector2 attackDirection = (targetPosition - spawnPosition).normalized;
                attackDirection.Normalize();
                _spawnPositions.Add(spawnPosition);
                _fireDirections.Add(attackDirection);
                float angle = Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg;
                stage.CreateDirectionalSquareRangeIndicator(spawnPosition, attackDirection, ProjectileRadius * 3f, ProjectileAliveDistance, hitTimeOnAnimation);
            }

            float time = now + hitTimeOnAnimation;
            base.AddOneOffSubAction(time, (Stage stage, float deltaTime, float now) =>
            {
                this.DoFire(stage);
            });

        }

        public override ActionBase End(Stage stage)
        {
            return null;
        }
        private void DoFire(Stage stage)
        {
            for (int i = 0; i < _spawnPositions.Count; i++)
            {
                stage.CreateProjectile(
                 ProjectileBodyPath,
                 _owner.Alliance,
                 _owner,
                 _owner.RangeAttackPower * DAMAGE_COEFFICIENT,
                 ProjectileKnobackPower,
                 spawnPosition: _spawnPositions[i],
                 direction: _fireDirections[i],
                 ProjectileSpeed,
                 ProjectileAcceleration,
                 ProjectileRadius,
                 ProjectileAliveDistance,
                 hitChances: 1,
                 splitCount: 0,
                 isRemovableBySpinBladeObject: false,
                 hitSoundPrefabPath: string.Empty
                 );
            }
        }

        private Vector2 GetSpawnPosition(Rect rect)
        {
            int spawnRandType = Random.Range(0, 4);
            Vector2 spawnPosition = Vector2.zero;

            if (spawnRandType == 0)
            {
                spawnPosition = new Vector2(rect.xMin - Random.Range(SpawnMinRandomRange, SpawnMaxRandomRange), Random.Range(rect.yMin - SpawnMaxRandomRange, rect.yMax + SpawnMaxRandomRange));
            }
            else if (spawnRandType == 1)
            {
                spawnPosition = new Vector2(rect.xMax + Random.Range(SpawnMinRandomRange, SpawnMaxRandomRange), Random.Range(rect.yMin - SpawnMaxRandomRange, rect.yMax + SpawnMaxRandomRange));
            }
            else if (spawnRandType == 2)
            {
                spawnPosition = new Vector2(Random.Range(rect.xMin - SpawnMaxRandomRange, rect.xMax + SpawnMaxRandomRange), rect.yMin - Random.Range(SpawnMinRandomRange, SpawnMaxRandomRange));
            }
            else if (spawnRandType == 3)
            {
                spawnPosition = new Vector2(Random.Range(rect.xMin - SpawnMaxRandomRange, rect.xMax + SpawnMaxRandomRange), rect.yMax + Random.Range(SpawnMinRandomRange, SpawnMaxRandomRange));
            }

            return spawnPosition;
        }
    }
}

