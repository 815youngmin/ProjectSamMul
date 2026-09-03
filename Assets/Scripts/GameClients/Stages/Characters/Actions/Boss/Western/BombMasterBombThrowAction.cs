using UnityEngine;
using Z.GameClients.Stages.AreaEffectObjects;
using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.Monsters;
using Z.Animations.Placeholder;
using Animation = Z.Animations.Placeholder.Animation;

namespace Z.GameClients.Stages.Characters.Actions.Boss.Western
{
    public class BombMasterBombThrowAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string ReadyAnimationName = "PeriodicExecutionBegin";
        private static readonly string AttackAnimationName = "PeriodicExecutionRepeat2";
        private static readonly string EndAnimationName = "PeriodicExecutionEnd";

        private static readonly float BOMB_DAMAGE_COEFFICIENT = 1.0f;
        private static readonly int BombThrowCount = 3;
        private static readonly int BombThrowAmount = 5;
        private static readonly float BombThrowRadius = 7f;
        private static readonly float BombAttackRadius = 1.5f;
        private static readonly float BombExplosionWaitDuration = 1.5f;

        private Monster _owner;
        private Character _target;

        private Animation _readyAnimation;
        private Animation _attackAnimation;
        private Animation _endAnimation;

        public override void Initialize(Monster owner, Character target, SpineMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
                animationController.FindAnimation(ReadyAnimationName).Duration +
                animationController.FindAnimation(AttackAnimationName).Duration * BombThrowCount +
                animationController.FindAnimation(EndAnimationName).Duration,
                animationController);

            _owner = owner;
            _target = target;

            _readyAnimation = AnimationController.FindAnimation(ReadyAnimationName);
            _attackAnimation = AnimationController.FindAnimation(AttackAnimationName);
            _endAnimation = AnimationController.FindAnimation(EndAnimationName);
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);
            AnimationController.SetAnimation(BodyAnimationTrack.WholeBody, _readyAnimation, false);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _attackAnimation, true, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _endAnimation, false, _attackAnimation.Duration * BombThrowCount);

            for(int i = 0; i < BombThrowCount; i++)
            {
                float throwAt = now + _readyAnimation.Duration + _attackAnimation.Duration * i + AnimationController.FindHitTime(_attackAnimation);
                base.AddOneOffSubAction(throwAt, (Stage stage, float deltaTime, float now) =>
                {
                    for(int j = 0; j < BombThrowAmount; j++)
                    {
                        Vector2 throwPosition = _target.Pos + Random.insideUnitCircle * BombThrowRadius;
                        stage.CreateExplosiveAreaEffectObject(AreaEffectType.DynamiteAreaEffectObject,_owner, _owner.CenterPos, throwPosition, BombExplosionWaitDuration, BombAttackRadius, _owner.SpecialAttackPower * BOMB_DAMAGE_COEFFICIENT);
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

