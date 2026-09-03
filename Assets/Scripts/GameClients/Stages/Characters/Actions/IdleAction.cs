using Z.GameClients.Stages.Characters.Animations;

namespace Z.GameClients.Stages.Characters.Actions
{


    public sealed class IdleAction : ActionBase
    {
        public IdleAction(CharacterAnimationController animationController) :
            base(ActionType.Idle, -1f, animationController)
        {
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);

            _animationController.PlayIdleAttackActionInfinitely();
            if (_animationController.IsPlayingMovement)
            {
                _animationController.StopMovement();
            }
        }

        public override void Update(Stage stage, float deltaTime, float now)
        {

        }

        public override ActionBase End(Stage stage)
        {
            return null;
        }

        public override bool Cancel(Stage stage)
        {
            if (!base.Cancel(stage))
            {
                return false;
            }

            return true;
        }
    }

}
