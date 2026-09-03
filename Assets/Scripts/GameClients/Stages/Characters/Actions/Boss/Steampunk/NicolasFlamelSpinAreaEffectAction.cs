using Z.GameClients.Stages.AreaEffectObjects;
using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.Monsters;
using Z.Animations.Placeholder;
using Animation = Z.Animations.Placeholder.Animation;

namespace Z.GameClients.Stages.Characters.Actions.Boss.Steampunk
{
    public class NicolasFlamelSpinAreaEffectAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string ReadyAnimationName = "SummonExecutionBegin";
        private static readonly string RepeatAnimationName = "SummonExecutionRepeat";
        private static readonly string EndAnimationName = "SummonExecutionEnd";

        private Monster _owner;
        private Character _target;

        private Animation _readyAnimation;
        private Animation _repeatAnimation;
        private Animation _endAnimation;

        private static readonly float DAMAGE_COEFFICIENT = 1.0f;
        private static readonly int FireCount = 3;
        private static readonly float FireDelay = 1f;
        private static readonly int FireAmount = 10;
        private static readonly float CreateRadius = 1f;
        private static readonly float AttackRadius = 0.75f;
        private static readonly float AreaEffectLifeTime = 10f;
        private static readonly float RadiusUpSpeed = 3.0f;
        private static readonly float AngleUpSpeed = 45f;
        private static readonly float RadiusUpAcceleration = 1.0f;
        private static readonly float AngleUpAcceleration = 1.0f;
        private static readonly float BodyRotatingSpeed = 0.0f;
        private int _currnetFireCount;


        public override void Initialize(Monster owner, Character target, SpineMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
                animationController.FindAnimation(ReadyAnimationName).Duration +
                FireCount * FireDelay +
                animationController.FindAnimation(EndAnimationName).Duration,
                animationController);

            _owner = owner;
            _target = target;

            _readyAnimation = AnimationController.FindAnimation(ReadyAnimationName);
            _repeatAnimation = AnimationController.FindAnimation(RepeatAnimationName);
            _endAnimation = AnimationController.FindAnimation(EndAnimationName);
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);
            AnimationController.SetAnimation(BodyAnimationTrack.WholeBody, _readyAnimation, false);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _repeatAnimation, true, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _endAnimation, false, FireCount * FireDelay);
            _currnetFireCount = 0;

            for (int i = 0; i < FireCount; i++)
            {
                float time = now + _readyAnimation.Duration + FireDelay * i;
                base.AddOneOffSubAction(time, (Stage stage, float deltaTime, float now) =>
                {
                    float angleDirection = _currnetFireCount++ % 2 == 0 ? 1 : -1;
                    for (int j = 0; j < FireAmount; j++)
                    {
                        stage.CreateSpinMoveObject(
                            _owner,
                            AreaEffectType.NicolasFlamelSpinObject,
                            _owner.SpecialAttackPower * DAMAGE_COEFFICIENT,
                            _owner.CenterPos,
                            AreaEffectLifeTime,
                            CreateRadius * angleDirection,
                            360f / FireAmount * j * angleDirection,
                            RadiusUpSpeed * angleDirection,
                            AngleUpSpeed * angleDirection,
                            RadiusUpAcceleration * angleDirection,
                            AngleUpAcceleration * angleDirection,
                            AttackRadius,
                            BodyRotatingSpeed);
                    }
                });
            }
        }


        public override ActionBase End(Stage stage)
        {
            return null;
        }
    }
}

