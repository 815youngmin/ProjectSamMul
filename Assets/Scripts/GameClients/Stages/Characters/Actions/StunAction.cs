using SamMul.GameClients.Stages.Characters.Animations;

namespace SamMul.GameClients.Stages.Characters.Actions
{
    public sealed class StunAction : ActionBase
    {
        public StunAction(CharacterAnimationController animationController, float duration) :
            base(ActionType.Stunned, duration, animationController)
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
            //아무것도 하지 않는다.
        }

        public override ActionBase End(Stage stage)
        {
            return null;
        }
    }
}
