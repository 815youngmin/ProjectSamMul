using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.Monsters;
using Z.Animations.Placeholder;
using Animation = Z.Animations.Placeholder.Animation;

namespace Z.GameClients.Stages.Characters.Actions.Boss.Alibaba
{
    public class GenieHomingSandObjectAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string ReadyAnimationName = "PeriodicExecutionBegin";
        private static readonly string WaitAnimationName = "PeriodicExecutionRepeat";
        private static readonly string AttackAnimationName = "PeriodicExecutionEnd";

        private static readonly float AREAEFFECT_DAMAGE_COEFFICIENT = 1.0f;
        private static readonly float HOMING_OBJECT_DAMAGE_COEFFICIENT = 2f;
        private static readonly float HomingObjectMoveSpeed = 6;
        private static readonly float HomingPower = 1.5f;
        private static readonly float HomingObjectLifeTime = 8f;
        private static readonly float HomingObjectRadius = 1.5f;
        private static readonly float AreaEffectCreationPeriod = 0.5f;
        private static readonly float AreaEffectLifeTime = 4f;
        private static readonly float AreaEffectTickPeriod = 0.25f;
        private static readonly float AreaEffectRadius = 2f;
        private static readonly float LastAreaEffectRadius = 4f;

        private Monster _owner;
        private Character _target;

        private Animation _readyAnimation;
        private Animation _waitAnimation;
        private Animation _attackAnimation;

        public override void Initialize(Monster owner, Character target, SpineMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
                animationController.FindAnimation(ReadyAnimationName).Duration +
                animationController.FindAnimation(WaitAnimationName).Duration +
                animationController.FindAnimation(AttackAnimationName).Duration,
                animationController);

            _owner = owner;
            _target = target;

            _readyAnimation = AnimationController.FindAnimation(ReadyAnimationName);
            _waitAnimation = AnimationController.FindAnimation(WaitAnimationName);
            _attackAnimation = AnimationController.FindAnimation(AttackAnimationName);
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);
            AnimationController.SetAnimation(BodyAnimationTrack.WholeBody, _readyAnimation, false);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _waitAnimation, false, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _attackAnimation, false, 0f);

            float time = now + _readyAnimation.Duration + _waitAnimation.Duration + AnimationController.FindHitTime(_attackAnimation);
            base.AddOneOffSubAction(time, (Stage stage, float deltaTime, float now) =>
            {
                stage.CreateGenieSandHomingObject(_owner, _target, _owner.CenterPos,
                    HomingObjectMoveSpeed,
                    HomingObjectLifeTime,
                    HomingPower,
                    HOMING_OBJECT_DAMAGE_COEFFICIENT * _owner.SpecialAttackPower,
                    HomingObjectRadius,
                    AreaEffectCreationPeriod,
                    AreaEffectLifeTime,
                    AreaEffectTickPeriod,
                    AREAEFFECT_DAMAGE_COEFFICIENT * _owner.SpecialAttackPower,
                    AreaEffectRadius,
                    LastAreaEffectRadius);
            });

        }


        public override ActionBase End(Stage stage)
        {
            return null;
        }
    }
}

