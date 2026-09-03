using SamMul.GameClients.Stages.Characters.Animations;

namespace SamMul.GameClients.Stages.Characters.Actions
{

    public abstract class ActionBase
    {
        private static readonly float INFINITE = float.MaxValue - 1f;

        public ActionType ActionType { get; private set; }
        public float Duration { get; private set; }
        public float BegunAt { get; private set; }
        public float EndAt { get; private set; }
        protected CharacterAnimationController _animationController { get; private set; }
        public bool IsCanceled { get; private set; }

        private bool _isInitialized;

        /// <summary>
        /// 이 생성자는 액션을 초기화하지 않습니다. <see cref="InitializeBase(ActionType, float, CharacterAnimationController"/>를 직접 호출해야 합니다.
        /// </summary>
        public ActionBase()
        {
            this.BegunAt = -1f;
            this.EndAt = -1f;
            this.IsCanceled = false;

            _isInitialized = false;
        }

        /// <summary>
        /// 이 생성자는 액션을 초기화합니다. <see cref="InitializeBase(ActionType, float, CharacterAnimationController)"/>를 직접 호출하면 안 됩니다.
        /// </summary>
        public ActionBase(ActionType actionType, float duration, CharacterAnimationController animationController) : this()
        {
            this.InitializeBase(actionType, duration, animationController);
        }

        /// <summary>
        /// 액션을 초기화합니다. 매개 변수 없는 생성자를 통해 객체를 생성한 경우 액션을 초기화하지 않기 때문에 이 함수를 직접 호출해야 합니다.
        /// </summary>
        public void InitializeBase(ActionType actionType, float duration, CharacterAnimationController animationController)
        {
            if (_isInitialized)
            {
                throw new LogicErrorException($"액션이 이미 초기화되었는데 다시 초기화가 요청되었습니다.");
            }

            this.ActionType = actionType;
            this.Duration = duration < 0f ? INFINITE : duration;
            this._animationController = animationController;

            _isInitialized = true;
        }

        /// <summary>
        /// 액션 시작시 호출
        /// </summary>
        public virtual void Begin(Stage stage, float now)
        {
            if (!_isInitialized)
            {
                throw new LogicErrorException($"액션이 초기화되지 않고 시작되었습니다.");
            }

            this.BegunAt = now;
            this.EndAt = (this.Duration == INFINITE) ?
                INFINITE :
                (now + this.Duration);
        }

        /// <summary>
        /// 액션이 진행되는 동안 매 프레임 호출
        /// </summary>
        public abstract void Update(Stage stage, float deltaTime, float now);
        /// <summary>
        /// 액션이 종료되는 시점에 호출
        /// 다음에 진행할 액션을 리턴하면, 해당 액션을 동일 프레임에 바로 이어서 재생한다. 
        /// <c>null</c>을 리턴하면, 예약된 액션을 이어서 재생한다. 예약된 액션이 없으면 <see cref="IdleAction"/>을 재생한다.
        /// </summary>
        public abstract ActionBase End(Stage stage);

        /// <summary>
        /// 다른 액션의 실행을 위해 이 액션이 중간에 취소되는 경우 호출된.
        /// <see cref="Cancel"/>이 호출되는 경우, <see cref="End"/>는 호출되지 않는다.
        /// 
        /// 이미 취소된 경우, 중단하고 false를 리턴한다.
        /// </summary>
        public virtual bool Cancel(Stage stage)
        {
            if (this.IsCanceled)
            {
                return false;
            }

            this.IsCanceled = true;
            return true;
        }
    }

}
