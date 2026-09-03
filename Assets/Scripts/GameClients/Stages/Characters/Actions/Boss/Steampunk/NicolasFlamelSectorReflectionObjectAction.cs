using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;
using UnityEngine;
using SamMul.GameClients.Stages.AreaEffectObjects;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.Characters.Actions.Boss.Steampunk
{
    public class NicolasFlamelSectorReflectionObjectAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string ReadyAnimationName = "PrepareSingleExecutionBegin";
        private static readonly string WaitAnimationName = "PrepareSingleExecutionRepeat";
        private static readonly string AttackAnimationName = "SingleExecutionAction";
        private static readonly string EndAnimationName = "SingleExecutionEnd";

        private static readonly float WaitDuration = 0.75f;
        private static readonly float DAMAGE_COEFFICIENT = 1.0f;

        private static readonly int FireCount = 3;
        private static readonly int ProjectileAmount = 3;
        private static readonly float SectorAngle = 60f;
        private static readonly float ProjectileSpeed = 15;
        private static readonly float ProjectileLifeTime = 8f;
        private static readonly float ProjectileRadius = 0.75f;
        private static readonly float ProjectileKnobackPower = 0.1f;
        private static readonly float ProjectileRotateSpeed = 720f;

        private Monster _owner;
        private Character _target;

        private Animation _readyAnimation;
        private Animation _waitAnimation;
        private Animation _attackAnimation;
        private Animation _endAnimation;

        private Bone _fireBone;

        public override void Initialize(Monster owner, Character target, SpineMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
                animationController.FindAnimation(ReadyAnimationName).Duration +
                WaitDuration +
                animationController.FindAnimation(AttackAnimationName).Duration * FireCount +
                animationController.FindAnimation(EndAnimationName).Duration,
                animationController);

            _owner = owner;
            _target = target;

            _readyAnimation = AnimationController.FindAnimation(ReadyAnimationName);
            _waitAnimation = AnimationController.FindAnimation(WaitAnimationName);
            _attackAnimation = AnimationController.FindAnimation(AttackAnimationName);
            _endAnimation = AnimationController.FindAnimation(EndAnimationName);

            _fireBone = AnimationController.Body.skeleton.FindBone("fire");

        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);
            AnimationController.SetAnimation(BodyAnimationTrack.WholeBody, _readyAnimation, false);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _waitAnimation, true, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _attackAnimation, true, WaitDuration);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _endAnimation, false, _attackAnimation.Duration * FireCount);

            for(int i = 0; i < FireCount; i++)
            {
                float time = now + _readyAnimation.Duration + WaitDuration + AnimationController.FindHitTime(_attackAnimation) + _attackAnimation.Duration * i;
                base.AddOneOffSubAction(time, (Stage stage, float deltaTime, float now) =>
                {
                    Vector2 firePos = _fireBone.GetWorldPosition(_owner.Body.transform);
                    Vector2 fireDirection = (_target.Pos - firePos).normalized;
                    this.FireSectorReflectionObject(stage, firePos, fireDirection);
                });
            }
        }

        public override ActionBase End(Stage stage)
        {
            return null;
        }

        private void FireSectorReflectionObject(Stage stage, Vector2 firePos, Vector2 fireDirection)
        {
            var rect = stage.FenceRect != null ? stage.FenceRect.Value : stage.StaticData.GetWalkableArea();

            for (int i = -(ProjectileAmount - 1); i <= ProjectileAmount - 1; i += 2)
            {
                Vector2 dir = Quaternion.Euler(0.0f, 0.0f, 0.5f * (float)i * SectorAngle / ProjectileAmount) * fireDirection;
                stage.CreateReflectionAreaEffectObject(
                    _owner,
                    AreaEffectType.NicolasFlamelReflectionObject,
                    ProjectileRadius,
                    firePos,
                    dir,
                    ProjectileSpeed,
                    ProjectileRotateSpeed,
                    DAMAGE_COEFFICIENT * _owner.SpecialAttackPower,
                    ProjectileKnobackPower,
                    ProjectileLifeTime,
                    rect
                    );
            }
        }
    }
}

