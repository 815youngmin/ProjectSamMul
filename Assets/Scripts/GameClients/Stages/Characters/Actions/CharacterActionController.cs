using UnityEngine;
using Z.GameClients.Stages.Characters.Animations;

namespace Z.GameClients.Stages.Characters.Actions
{
    public class CharacterActionController
    {
        // 상태변화에 따라 애니메이션을 변경하기 위해 참조를 가지고 있는다.
        private readonly CharacterAnimationController animationController;

        private ActionBase _currentAction;
        public ActionBase CurrentAction => _currentAction;

        public bool IsIdle => _currentAction.ActionType == ActionType.Idle;
        public bool IsAppearing => _currentAction.ActionType == ActionType.Appearing;
        public bool IsDead => _currentAction.ActionType == ActionType.Dead;
        public bool IsStunned => _currentAction.ActionType == ActionType.Stunned;
        public bool IsAttacking => _currentAction.ActionType == ActionType.Attack;
        public bool IsSkilling => _currentAction.ActionType == ActionType.Skill;
        public bool IsReloading => _currentAction.ActionType == ActionType.Reload;
        public bool IsBeingSummoned => _currentAction.ActionType == ActionType.Summoned;

        public CharacterActionController(CharacterAnimationController animationController)
        {
            _currentAction = new IdleAction(animationController);

            _currentAction.Begin(null, Time.time);

            this.animationController = animationController;
        }

        public void Update(Stage stage)
        {
            var now = Time.time;

            _currentAction.Update(stage, Time.deltaTime, now);

            if (now >= _currentAction.EndAt)
            {
                var nextAction = _currentAction.End(stage);
                if (nextAction == null)
                {
                    nextAction = new IdleAction(this.animationController);
                }
                _currentAction = nextAction;
                _currentAction.Begin(stage, now);
                return;
            }
        }

        public virtual void ChangeTo(Stage stage, ActionBase nextAction)
        {
            // previousAction.Cancel() 내부에서 상태가 변경되어 다른 Action이 실행중일 수도 있다.
            var previousAction = _currentAction;
            while (!previousAction.IsCanceled)
            {
                previousAction.Cancel(stage);
                previousAction = _currentAction;
            }

            _currentAction = nextAction;
            _currentAction.Begin(stage, Time.time);
        }
    }

    public class PCActionController : CharacterActionController
    {
        public PCActionController(CharacterAnimationController animationController)
            : base(animationController)
        {
        }

        public override void ChangeTo(Stage stage, ActionBase nextAction)
        {
            base.ChangeTo(stage, nextAction);
        }
    }

}
