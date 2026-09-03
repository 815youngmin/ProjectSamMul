using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;
using UnityEngine;
using SamMul.GameClients.Stages.AreaEffectObjects;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;

namespace SamMul.GameClients.Stages.Characters.Actions.Boss.Steampunk
{
    public class BattleGolemBoomerangAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string ReadyAnimationName = "PrepareSingleExecutionBegin";
        private static readonly string WaitAnimationName = "PrepareSingleExecutionRepeat";
        private static readonly string AttackAnimationName = "SingleExecutionAction";
        private static readonly string EndAnimationName = "SingleExecutionEnd";

        private static readonly float DAMAGE_COEFFICIENT = 1.0f;
        private static readonly float BoomerangDuration = 2f;
        private static readonly float BoomerangSpeed = 15f;
        private static readonly float BoomerangRadius = 0.75f;
        private static readonly float BoomerangRotateSpeed = 720f;

        private Monster _owner;
        private Character _target;

        private Animation _readyAnimation;
        private Animation _waitAnimation;
        private Animation _attackAnimation;
        private Animation _endAnimation;

        private EventData Hit1FrameEvent;
        private EventData Hit2FrameEvent;
        private EventData Hit3FrameEvent;

        private float _hit1TimeOnAniamtion;
        private float _hit2TimeOnAniamtion;
        private float _hit3TimeOnAniamtion;

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

            this.Hit1FrameEvent = AnimationController.Body.Skeleton.Data.FindEvent("hit1");
            this.Hit2FrameEvent = AnimationController.Body.Skeleton.Data.FindEvent("hit2");
            this.Hit3FrameEvent = AnimationController.Body.Skeleton.Data.FindEvent("hit3");

            _hit1TimeOnAniamtion = AnimationController.FindEventTime(_attackAnimation, Hit1FrameEvent);
            _hit2TimeOnAniamtion = AnimationController.FindEventTime(_attackAnimation, Hit2FrameEvent);
            _hit3TimeOnAniamtion = AnimationController.FindEventTime(_attackAnimation, Hit3FrameEvent);


        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);
            AnimationController.SetAnimation(BodyAnimationTrack.WholeBody, _readyAnimation, false);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _waitAnimation, false, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _attackAnimation, false, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _endAnimation, false, 0f);

            float attackAt = now + _readyAnimation.Duration + _waitAnimation.Duration;
            base.AddOneOffSubAction(attackAt + _hit1TimeOnAniamtion, (Stage stage, float deltaTime, float now) =>
            {
                this.Fire(stage, _owner.CenterPos);
            });

            base.AddOneOffSubAction(attackAt + _hit2TimeOnAniamtion, (Stage stage, float deltaTime, float now) =>
            {
                this.Fire(stage, _owner.CenterPos);
            });

            base.AddOneOffSubAction(attackAt + _hit3TimeOnAniamtion, (Stage stage, float deltaTime, float now) =>
            {
                this.Fire(stage, _owner.CenterPos);
            });

        }

        public override ActionBase End(Stage stage)
        {
            return null;
        }

        private void Fire(Stage stage, Vector2 firePos)
        {
            Vector2 dir = (_target.Pos - firePos).normalized;
            Vector2 endPosition = firePos + dir * (BoomerangSpeed * BoomerangDuration * 0.5f);

            stage.CreateBoomerangObject(
                AreaEffectType.BattleGolemBoomerangObject,
                _owner,
                BoomerangDuration,
                firePos,
                endPosition,
                _owner.RangeAttackPower * DAMAGE_COEFFICIENT,
                BoomerangRadius,
                BoomerangRotateSpeed);
        }

    }
}

