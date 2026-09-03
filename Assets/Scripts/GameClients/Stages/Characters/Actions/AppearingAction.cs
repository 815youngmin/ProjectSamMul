using Z.GameClients.Stages.Characters.Animations;

namespace Z.GameClients.Stages.Characters.Actions
{
    public sealed class AppearingAction : ActionBase
    {
        private readonly Character _owner;

        public AppearingAction(float appearingDuration, Character owner, CharacterAnimationController animationController) :
            base(ActionType.Appearing, appearingDuration, animationController)
        {
            this._owner = owner;
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);

            this._owner.SetImmuneToHit();
            this._animationController.PlayAppear(this.Duration);
        }

        public override void Update(Stage stage, float deltaTime, float now)
        {
        }

        public override ActionBase End(Stage stage)
        {
            _owner.UnsetImmuneToHit();

            return new IdleAction(this._animationController);
        }

        public override bool Cancel(Stage stage)
        {
            if (!base.Cancel(stage))
            {
                return false;
            }

            this._owner.UnsetImmuneToHit();

            return true;
        }
    }

}
