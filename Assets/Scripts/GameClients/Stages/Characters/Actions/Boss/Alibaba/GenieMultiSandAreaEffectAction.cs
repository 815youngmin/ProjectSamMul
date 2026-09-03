using UnityEngine;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;

namespace SamMul.GameClients.Stages.Characters.Actions.Boss.Alibaba
{
    public class GenieMultiSandAreaEffectAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string ReadyAnimationName = "PrepareSingleExecutionBegin";
        private static readonly string WaitAnimationName = "PrepareSingleExecutionRepeatd";
        private static readonly string AttackAnimationName = "SingleExecutionAction";
        private static readonly string EndAnimationName = "SingleExecutionEnd";

        private static readonly float DAMAGE_COEFFICIENT = 1.0f;

        private static readonly int SandAttackCount = 3;
        private static readonly float SandCreateRadius =10f;
        private static readonly int SandAreaEffectAmount = 6;
        private static readonly float SandIndicatorDuration = 1f;
        private static readonly float SandAttackPeriod = 0.25f;
        private static readonly float SandLifeTime = 5f;
        private static readonly float SandObjectRadius = 2.5f;

        private Monster _owner;
        private Character _target;

        private Animation _readyAnimation;
        private Animation _waitAnimation;
        private Animation _attackAnimation;
        private Animation _endAnimation;


        public override void Initialize(Monster owner, Character target, SpineMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
                animationController.FindAnimation(ReadyAnimationName).Duration +
                animationController.FindAnimation(WaitAnimationName).Duration + 
                animationController.FindAnimation(AttackAnimationName).Duration * SandAttackCount +
                animationController.FindAnimation(EndAnimationName).Duration,
                animationController);

            _owner = owner;
            _target = target;

            _readyAnimation = AnimationController.FindAnimation(ReadyAnimationName);
            _attackAnimation = AnimationController.FindAnimation(AttackAnimationName);
            _waitAnimation = AnimationController.FindAnimation(WaitAnimationName);
            _endAnimation = AnimationController.FindAnimation(EndAnimationName);
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);

            AnimationController.SetAnimation(BodyAnimationTrack.WholeBody, _readyAnimation, false);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _waitAnimation, false, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _attackAnimation, true, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _endAnimation, false, _attackAnimation.Duration * SandAttackCount);

            float time = now + _readyAnimation.Duration +_waitAnimation.Duration + AnimationController.FindHitTime(_attackAnimation);
            for (int i = 0; i < SandAttackCount; i++)
            {
                base.AddOneOffSubAction(time + i * _attackAnimation.Duration, (Stage stage, float deltaTime, float now) =>
                {
                    this.CreateSandAreaEffect(stage, _owner);
                });
            }

        }
        public override ActionBase End(Stage stage)
        {
            return null;
        }

        private void CreateSandAreaEffect(Stage stage, Monster owner)
        {
            for (int i = 0; i < SandAreaEffectAmount; i++)
            {
                Vector2 position = _target.Pos + Random.insideUnitCircle.normalized * Random.Range(0, SandCreateRadius);
                stage.CreateSandAreaEffectObject(owner, delay: 0f, SandIndicatorDuration, owner.SpecialAttackPower * DAMAGE_COEFFICIENT, SandAttackPeriod, SandLifeTime, SandObjectRadius, position);
            }
        }
    }
}

