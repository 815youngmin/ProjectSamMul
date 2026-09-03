using Z.GameClients.Stages.Characters.Animations;


namespace Z.GameClients.Stages.Characters.Actions
{
    public class DisappearingAction : ActionBase
    {
        private readonly Character _owner;

        public DisappearingAction(Character owner, CharacterAnimationController animationController) 
            : base(ActionType.Dead, -1.0f, animationController)     // 사라짐은 죽음과 동등한 상태로 취급한다.
        {
            _owner = owner;
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);
            _owner.SetImmuneToHit();
            _animationController.PlayDisappear();
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
