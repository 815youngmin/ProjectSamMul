using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.Monsters;

namespace Z.GameClients.Stages.Characters.Actions
{
    //소환된 타겟의 소환 마무리 될때까지의 액션이니다.
    public class SummonedAction : ActionBase
    {
        private Monster _owner;

        public SummonedAction(
            CharacterAnimationController animationController,
            Monster owner,
            float summonTime) :
            base(ActionType.Summoned, summonTime, animationController)
        {
            _owner = owner;
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);

            if (_animationController.AppearAnimationDuration > 0.0f)
            {
                _animationController.PlayAppear(this.Duration);
            }
            else
            {
                _animationController.PlayIdleAttackActionInfinitely();
            }

            //무적 처리
            _owner.SetImmuneToHit();
        }


        public override ActionBase End(Stage stage)
        {
            //무적 처리 해제
            _owner.UnsetImmuneToHit();

            return new IdleAction(this._animationController);
        }

        public override void Update(Stage stage, float deltaTime, float now)
        {
        }

        public override bool Cancel(Stage stage)
        {
            if (!base.Cancel(stage))
            {
                return false;
            }

            //무적 처리 해제
            _owner.UnsetImmuneToHit();
            return true;
        }

    }

}

