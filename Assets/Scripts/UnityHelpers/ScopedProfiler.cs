using System;
using UnityEngine.Profiling;

namespace Z.UnityHelpers
{
    /// <summary>
    /// using 블록 범위를 프로파일러 샘플로 묶습니다.
    /// </summary>
    public struct ScopedProfiler : IDisposable
    {
        public ScopedProfiler(string sampleTargetName)
        {
            Profiler.BeginSample(sampleTargetName);
        }

        public void Dispose()
        {
            Profiler.EndSample();
        }
    }
}
