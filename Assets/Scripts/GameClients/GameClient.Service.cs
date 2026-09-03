#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace SamMul.GameClients
{
    /// <summary>
    /// 메인 스레드에서 비동기 작업을 하나씩 순서대로 실행하는 큐입니다.
    /// </summary>
    public sealed class AsyncJobDispatcher
    {
        private readonly Queue<Func<Task>> _pendingJobs = new Queue<Func<Task>>();
        private Task? _runningJob;

        /// <summary>
        /// 실행 중인 작업이 없으면 바로 시작하고, 있으면 뒤에 예약합니다.
        /// </summary>
        public void RunOrReserveAsyncJob(Func<Task> job)
        {
            _pendingJobs.Enqueue(job);
            this.Dispatch();
        }

        /// <summary>
        /// 매 프레임 호출. 실행 중인 작업이 끝났으면 다음 작업을 시작합니다.
        /// </summary>
        public void Dispatch()
        {
            if (_runningJob != null)
            {
                if (!_runningJob.IsCompleted)
                {
                    return;
                }

                if (_runningJob.IsFaulted && _runningJob.Exception != null)
                {
                    Debug.LogException(_runningJob.Exception);
                }
                _runningJob = null;
            }

            if (_pendingJobs.Count == 0)
            {
                return;
            }

            var job = _pendingJobs.Dequeue();
            try
            {
                _runningJob = job();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                _runningJob = null;
            }
        }
    }
}
