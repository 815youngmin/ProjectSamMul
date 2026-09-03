using UnityEngine;
using Z.GameClients.Stages.AreaEffectObjects;
using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.Monsters;
using Z.Animations.Placeholder;
using Animation = Z.Animations.Placeholder.Animation;

namespace Z.GameClients.Stages.Characters.Actions.Boss.Western
{
    public class BombMasterHardBombCircleThrowAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string ReadyAnimationName = "PeriodicExecutionBegin";
        private static readonly string AttackAnimationName = "PeriodicExecutionRepeat2";
        private static readonly string EndAnimationName = "PeriodicExecutionEnd";

        private static readonly float BOMB_DAMAGE_COEFFICIENT = 1.0f;
        private static readonly int BombThrowAmount = 8;
        private static readonly float BombThrowRadius = 4f;
        private static readonly float BombAttackRadius = 1.5f;
        private static readonly float BombExplosionWaitDuration = 1.2f;
        private static readonly float BombExplosionDelay = 0.1f;

        private Monster _owner;
        private Character _target;
        private int _bombThrowCount;

        private Animation _readyAnimation;
        private Animation _attackAnimation;
        private Animation _endAnimation;

        public override void Initialize(Monster owner, Character target, SpineMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
                animationController.FindAnimation(ReadyAnimationName).Duration +
                animationController.FindAnimation(AttackAnimationName).Duration +
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
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _attackAnimation, false, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _endAnimation, false, 0f);

            float bombThrowDuration = _attackAnimation.Duration - AnimationController.FindHitTime(_attackAnimation);
            float throwAt = now + _readyAnimation.Duration + AnimationController.FindHitTime(_attackAnimation);

            _bombThrowCount = 0;

            for (int i = 0; i < BombThrowAmount; i++)
            { 
                base.AddOneOffSubAction(throwAt + (bombThrowDuration / BombThrowAmount) * i, (Stage stage, float deltaTime, float now) =>
                {
                    Vector2 dir = Quaternion.Euler(0.0f, 0.0f, -360f / BombThrowAmount * _bombThrowCount) * Vector2.up;
                    Vector2 throwPosition = _target.CenterPos + dir * BombThrowRadius;
                    stage.CreateExplosiveAreaEffectObject(AreaEffectType.DynamiteAreaEffectObject, _owner, _owner.CenterPos, throwPosition, BombExplosionWaitDuration + BombExplosionDelay * _bombThrowCount, BombAttackRadius, _owner.SpecialAttackPower * BOMB_DAMAGE_COEFFICIENT);
                    _bombThrowCount++;
                });
            }

        }

        public override ActionBase End(Stage stage)
        {
            return null;
        }
    }
}
