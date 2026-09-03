using UnityEngine;
using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.Monsters;
using Z.Animations.Placeholder;
using Animation = Z.Animations.Placeholder.Animation;

namespace Z.GameClients.Stages.Characters.Actions.Boss.Alibaba
{
    public class AladdinMultiSandAreaEffectAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string ReadyAnimationName = "PeriodicExecutionBegin";
        private static readonly string RepeatAnimationName = "PeriodicExecutionRepeat2";
        private static readonly string AttackAnimationName = "PeriodicExecutionRepeat";
        private static readonly string EndAnimationName = "PeriodicExecutionEnd";

        private static readonly float DAMAGE_COEFFICIENT = 1.0f;

        private static readonly int SandAttackCount = 3;
        private static readonly float AttackPeriod = 0.5f;
        private static readonly float SandCreateRadius = 8f;
        private static readonly int SandAreaEffectAmount = 5;
        private static readonly float SandIndicatorDuration = 1f;
        private static readonly float SandAttackPeriod = 0.25f;
        private static readonly float SandLifeTime = 5f;
        private static readonly float SandObjectRadius = 2f;

        private Monster _owner;
        private Character _target;

        private Animation _readyAnimation;
        private Animation _repeatAnimation;
        private Animation _attackAnimation;
        private Animation _endAnimation;


        public override void Initialize(Monster owner, Character target, SpineMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
                animationController.FindAnimation(ReadyAnimationName).Duration +
                animationController.FindHitTime(animationController.FindAnimation(AttackAnimationName)) +
                SandAttackCount * AttackPeriod +
                animationController.FindAnimation(EndAnimationName).Duration,
                animationController);

            _owner = owner;
            _target = target;

            _readyAnimation = AnimationController.FindAnimation(ReadyAnimationName);
            _attackAnimation = AnimationController.FindAnimation(AttackAnimationName);
            _repeatAnimation = AnimationController.FindAnimation(RepeatAnimationName);
            _endAnimation = AnimationController.FindAnimation(EndAnimationName);
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);
            
            float repeatTime = SandAttackCount * AttackPeriod - (_attackAnimation.Duration - AnimationController.FindHitTime(_attackAnimation));
            AnimationController.SetAnimation(BodyAnimationTrack.WholeBody, _readyAnimation, false);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _attackAnimation, false, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _repeatAnimation, true, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _endAnimation, false, repeatTime);

            float time = now + _readyAnimation.Duration + AnimationController.FindHitTime(_attackAnimation);
            for(int i = 0; i < SandAttackCount; i++)
            {
                base.AddOneOffSubAction(time + i * AttackPeriod, (Stage stage, float deltaTime, float now) =>
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
                Vector2 position = _target.Pos+ Random.insideUnitCircle.normalized * Random.Range(0, SandCreateRadius);
                stage.CreateSandAreaEffectObject(owner, delay: 0f, SandIndicatorDuration, owner.SpecialAttackPower * DAMAGE_COEFFICIENT, SandAttackPeriod, SandLifeTime, SandObjectRadius, position);
            }
        }

    }
}

