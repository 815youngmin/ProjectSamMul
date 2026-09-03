using System;
using System.Collections.Generic;

namespace SamMul.GameClients.Stages
{
    public class RecurringActionManager<T> where T : struct
    {
        private class RecurringAction
        {
            public readonly float BeginAt;
            public readonly float EndAt;
            public readonly float Interval;
            private readonly Action<T> InvokeAction;
            public float PrevInvokeAt { get; private set; }

            /// <summary>
            /// 반복 액션 객체를 생성합니다.
            /// </summary>
            /// <param name="beginAt">
            /// 반복 액션이 실행될 유니티 시각.
            /// </param>
            /// <param name="endAt">
            /// 반복 액션이 종료될 유니티 시각.<br/>
            /// 이 값이 <paramref name="beginAt"/>보다 크면 이 시각이 될 때까지 매 프레임 서브 액션을 실행합니다.
            /// </param>
            /// <param name="interval">
            /// 호출 간격<br/>
            /// </param>
            /// <param name="invokeAction">
            /// 반복 호출할 액션
            /// </param>
            public RecurringAction(float beginAt, float endAt, float interval, Action<T> invokeAction)
            {
                this.BeginAt = beginAt;
                this.EndAt = endAt;
                this.Interval = interval;
                this.InvokeAction = invokeAction;
            }

            /// <summary>
            ///액션을 실행합니다.
            /// <param name="now">
            /// 현재 유니티 시각.
            /// <param name="param">
            /// 액션 호출에 필요한 파라미터
            /// </param>
            public void Invoke(float now, T param)
            {
                PrevInvokeAt = now;
                InvokeAction.Invoke(param);
            }
        }

        private readonly List<RecurringAction> _pendingActions;    // 실행 대기 중인 액션.
        private readonly List<RecurringAction> _runningActions;    // 실행 지속 중인 액션.

        public RecurringActionManager()
        {
            _pendingActions = new List<RecurringAction>();
            _runningActions = new List<RecurringAction>();
        }

        public float AddAction(float beginAt, float duration, float delay, Action<T> invokeAction)
        {
            int index = _pendingActions.Count - 1;
            while (index >= 0)
            {
                if (_pendingActions[index].BeginAt <= beginAt)
                {
                    break;
                }
                --index;
            }
            _pendingActions.Insert(++index, new RecurringAction(beginAt, beginAt + duration, delay, invokeAction));

            return beginAt + duration;
        }

        public void Update(float now, T param)
        {
            // 실행 지속 중인 반복 액션 처리.
            for (int i = 0; i < _runningActions.Count; ++i)
            {
                var invokeAction = _runningActions[i];
                if (now < invokeAction.EndAt &&
                    invokeAction.PrevInvokeAt + invokeAction.Interval <= now)
                {
                    invokeAction.Invoke(now, param);
                }
                else if (invokeAction.EndAt <= now)
                {
                    _runningActions.RemoveAt(i--);
                }
            }

            // 실행 대기 중인 반복 액션 처리.
            while (_pendingActions.Count > 0 && _pendingActions[0].BeginAt < now)
            {
                var invokeAction = _pendingActions[0];
                _pendingActions.RemoveAt(0);

                invokeAction.Invoke(now, param);
                if (now < invokeAction.EndAt)
                {
                    _runningActions.Add(invokeAction);
                }
            }
        }

        public void ClearAction()
        {
            _pendingActions.Clear();
            _runningActions.Clear();
        }
    }
}
