using UnityEngine;
using Z.GameClients.Stages.Characters.Animations;

namespace Z.GameClients.Stages.Characters.Actions
{

    public sealed class DeadAction : ActionBase
    {
        private readonly Character _owner;
        private readonly Vector2 _hitVector;

        public DeadAction(Vector2 hitVector, Character owner, CharacterAnimationController animationController) : base(ActionType.Dead, -1f, animationController)
        {
            _hitVector = hitVector;
            _owner = owner;
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);

            _owner.KnockBack(_hitVector, _owner.DeadForceConversionRate);
            _owner.SetImmuneToHit();

            if(_hitVector != Vector2.zero)
            {
                _animationController.UpdateBodyDirectionByMoveDirection(-_hitVector);
            }
            _animationController.StopAllAndPlayDead();

            _owner.ShowShadow(false);
        }

        public override void Update(Stage stage, float deltaTime, float now)
        {

        }

        public override ActionBase End(Stage stage)
        {
            _owner.UnsetImmuneToHit();
            return null;
        }

        public override bool Cancel(Stage stage)
        {
            if (!base.Cancel(stage))
            {
                return false;
            }

            _owner.UnsetImmuneToHit();
            return true;
        }
    }

}
