using System;
using System.Collections.Generic;
using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.Monsters;

namespace Z.GameClients.Stages.Characters.Actions
{
    /// <summary>
    /// 보스 패턴 등에 사용할 수 있는 스마트 액션 클래스입니다.
    /// 서브 액션을 추가해두면 지정된 타이밍에 실행합니다.
    /// </summary>
    /// <typeparam name="T">
    /// 스마트 액션을 실행할 캐릭터의 애니메이션 컨트롤러 형식입니다.
    /// </typeparam>
    public abstract class SmartAction<T> : ActionBase where T : CharacterAnimationController
    {
        /// <summary>
        /// 액션을 구성하는 서브 액션입니다.
        /// </summary>
        private class SubAction
        {
            public readonly float BeginAt;
            public readonly float EndAt;
            public readonly float Interval;
            private readonly Action<Stage, float, float> _subAction;
            public float PrevInvokeAt { get; private set; }

            /// <summary>
            /// 서브 액션 객체를 생성합니다.
            /// </summary>
            /// <param name="beginAt">
            /// 서브 액션이 실행될 유니티 시각.
            /// </param>
            /// <param name="endAt">
            /// 서브 액션이 종료될 유니티 시각.<br/>
            /// 이 값이 <paramref name="beginAt"/>보다 크면 이 시각이 될 때까지 매 프레임 서브 액션을 실행합니다.
            /// </param>
            /// <param name="subAction">
            /// 서브 액션을 통해 실행할 동작.
            /// </param>
            public SubAction(float beginAt, float endAt,float interval, Action<Stage, float, float> subAction)
            {
                this.BeginAt = beginAt;
                this.EndAt = endAt;
                this.Interval = interval;
                this._subAction = subAction;
            }

            /// <summary>
            /// 서브 액션을 실행합니다.
            /// </summary>
            /// <param name="stage">
            /// 이 서브 액션이 실행될 스테이지.
            /// </param>
            /// <param name="deltaTime">
            /// 전 프레임부터 현 프레임까지 경과한 유니티 시간.
            /// </param>
            /// <param name="now">
            /// 현재 유니티 시각.
            /// </param>
            public void Invoke(Stage stage, float deltaTime, float now)
            {
                PrevInvokeAt = now;
                _subAction.Invoke(stage, deltaTime, now);
            }
        }

        protected T AnimationController => (T)base._animationController;

        private readonly List<SubAction> _pendingSubActions;    // 실행 대기 중인 서브 액션.
        private readonly List<SubAction> _runningSubActions;    // 실행 지속 중인 서브 액션.

        /// <summary>
        /// 스마트 액션을 생성합니다. 이 생성자는 액션을 초기화하지 않기 때문에, <see cref="Initialize(Monster, Character, T)"/>를 오버라이드하여 <see cref="ActionBase.InitializeBase(ActionType, float, CharacterAnimationController)"/>를 호출해야 합니다.
        /// </summary>
        protected SmartAction() : base()
        {
            _pendingSubActions = new List<SubAction>();
            _runningSubActions = new List<SubAction>();
        }

        /// <summary>
        /// 스마트 액션을 초기화합니다.
        /// </summary>
        /// <param name="owner">
        /// 이 액션을 실행하는 몬스터.
        /// </param>
        /// <param name="target">
        /// 이 액션의 대상 캐릭터.
        /// </param>
        /// <param name="animationController">
        /// 이 액션을 실행하는 몬스터의 애니메이션 컨트롤러.
        /// </param>
        /// <remarks>
        /// 액션을 초기화하기 위해 이 함수를 오버라이드하여 <see cref="ActionBase.InitializeBase(ActionType, float, CharacterAnimationController)"/>를 호출해야 합니다.
        /// </remarks>
        public abstract void Initialize(Monster owner, Character target, T animationController);

        /// <summary>
        /// 단 한 번만 실행되는 서브 액션을 추가합니다.<br/>
        /// <paramref name="beginAt"/>이 되면 서브 액션을 단 한 번만 실행합니다.<br/>
        /// <paramref name="beginAt"/>이 같은 서브 액션을 여러 개 추가한 경우 먼저 추가된 서브 액션이 먼저 실행됩니다.
        /// </summary>
        /// <param name="beginAt">
        /// 서브 액션이 실행될 유니티 시각.
        /// </param>
        /// <param name="subAction">
        /// 서브 액션을 통해 실행할 동작. (stage, deltaTime, now)
        /// </param>
        /// <returns>
        /// 추가된 서브 액션이 종료되는 유니티 시각.
        /// </returns>
        protected float AddOneOffSubAction(float beginAt, Action<Stage, float, float> subAction)
            => this.AddSubAction(beginAt, 0.0f, 0.0f, subAction);

        /// <summary>
        /// 일정 시간 동안 지속해서 실행되는 서브 액션을 추가합니다.<br/>
        /// <paramref name="beginAt"/>이 되면 <paramref name="duration"/> 동안 매 프레임 서브 액션을 실행합니다.<br/>
        /// <paramref name="beginAt"/>이 같은 서브 액션을 여러 개 추가한 경우 먼저 추가된 서브 액션이 먼저 실행됩니다.
        /// </summary>
        /// <param name="beginAt">
        /// 서브 액션이 실행될 유니티 시각.
        /// </param>
        /// <param name="duration">
        /// 서브 액션이 지속되는 유니티 시간.
        /// </param>
        /// <param name="subAction">
        /// 서브 액션을 통해 실행할 동작. (stage, deltaTime, now)
        /// </param>
        /// <returns>
        /// 추가된 서브 액션이 종료되는 유니티 시각.
        /// </returns>
        protected float AddDurationalSubAction(float beginAt, float duration, Action<Stage, float, float> subAction)
            => this.AddSubAction(beginAt, duration, 0.0f, subAction);

        /// <summary>
        /// 서브 액션을 추가합니다.<br/>
        /// 서브 액션은 <paramref name="beginAt"/>에 따라 정렬됩니다.<br/>
        /// <paramref name="beginAt"/>이 같은 서브 액션을 여러 개 추가한 경우 먼저 추가된 서브 액션이 먼저 실행됩니다.
        /// </summary>
        /// <param name="beginAt">
        /// 서브 액션이 실행될 유니티 시각.
        /// </param>
        /// <param name="duration">
        /// 서브 액션이 지속되는 유니티 시간.
        /// </param>
        /// <param name="subAction">
        /// 서브 액션을 통해 실행할 동작. (stage, deltaTime, now)
        /// </param>
        /// <returns>
        /// 추가된 서브 액션이 종료되는 유니티 시각.
        /// </returns>
        private float AddSubAction(float beginAt, float duration, float interval, Action<Stage, float, float> subAction)
        {
            int index = _pendingSubActions.Count - 1;
            while (index >= 0)
            {
                if (_pendingSubActions[index].BeginAt <= beginAt)
                {
                    break;
                }
                --index;
            }
            _pendingSubActions.Insert(++index, new SubAction(beginAt, beginAt + duration, interval, subAction));

            return beginAt + duration;
        }

        public override void Update(Stage stage, float deltaTime, float now)
        {
            // 실행 지속 중인 서브 액션 처리.
            for (int i = 0; i < _runningSubActions.Count; ++i)
            {
                var subAction = _runningSubActions[i];
                if (now < subAction.EndAt && 
                    subAction.PrevInvokeAt + subAction.Interval <= now)
                {
                    subAction.Invoke(stage, deltaTime, now);
                }
                else if(subAction.EndAt <= now)
                {
                    _runningSubActions.RemoveAt(i--);
                }
            }

            // 실행 대기 중인 서브 액션 처리.
            while (_pendingSubActions.Count > 0 && _pendingSubActions[0].BeginAt < now)
            {
                var subAction = _pendingSubActions[0];
                _pendingSubActions.RemoveAt(0);

                subAction.Invoke(stage, deltaTime, now);
                if (now < subAction.EndAt)
                {
                    _runningSubActions.Add(subAction);
                }
            }
        }

        public override ActionBase End(Stage stage)
        {
            _pendingSubActions.Clear();
            _runningSubActions.Clear();
            return null;
        }
    }
}
