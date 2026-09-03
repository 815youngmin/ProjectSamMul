using UnityEngine;
using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.Monsters;
using Z.Animations.Placeholder;
using Animation = Z.Animations.Placeholder.Animation;

namespace Z.GameClients.Stages.Characters.Actions.Boss.Steampunk
{
    public class JamesWattMultipleAreaEffectAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string ReadyAnimationName = "PeriodicExecutionBegin";
        private static readonly string WaitAnimationName = "PeriodicExecutionRepeat";
        private static readonly string EndAnimationName = "PeriodicExecutionEnd";

        private static readonly float DAMAGE_COEFFICIENT = 1.0f;
        private static readonly float LightningAreaEffectRadius = 3f;
        private static readonly int LightningAmount = 6;
        private static readonly float LightningThrowDistance = 5f;
        private static readonly float LightningIndicatorDuration = 2f;


        private Monster _owner;
        private Character _target;

        private Animation _readyAnimation;
        private Animation _waitAnimation;
        private Animation _endAnimation;

        public override void Initialize(Monster owner, Character target, SpineMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
                animationController.FindAnimation(ReadyAnimationName).Duration +
                animationController.FindAnimation(WaitAnimationName).Duration +
                animationController.FindAnimation(EndAnimationName).Duration,
                animationController);

            _owner = owner;
            _target = target;

            _readyAnimation = AnimationController.FindAnimation(ReadyAnimationName);
            _waitAnimation = AnimationController.FindAnimation(WaitAnimationName);
            _endAnimation = AnimationController.FindAnimation(EndAnimationName);
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);
            AnimationController.SetAnimation(BodyAnimationTrack.WholeBody, _readyAnimation, false);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _waitAnimation, false, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _endAnimation, false, 0f);

            float bombThrowAt = now + _readyAnimation.Duration;
            base.AddOneOffSubAction(bombThrowAt, (Stage stage, float deltaTime, float now) =>
            {
                this.CreateLightning(stage);
            });

        }

        public override ActionBase End(Stage stage)
        {
            return null;
        }

        private void CreateLightning(Stage stage)
        {
            for (int i = 0; i < LightningAmount; i++)
            {
                Vector2 dir = Quaternion.Euler(0.0f, 0.0f, 360f / LightningAmount * i) * Vector2.up; 
                stage.CreateZeusLightningAreaEffectObject(
                    _owner, _owner.CenterPos + dir * LightningThrowDistance, LightningAreaEffectRadius, DAMAGE_COEFFICIENT * _owner.SpecialAttackPower, delay: 0f, LightningIndicatorDuration);
            }

        }

    }
}

