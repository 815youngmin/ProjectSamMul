#nullable enable
using System;

namespace SamMul.ResourcePools
{
    /// <summary>
    /// 리소스를 로드하여 적용하는 도우미. 같은 주소를 다시 요청하면 무시합니다.
    /// Resources.Load 기반이라 실제로는 동기로 완료됩니다.
    /// </summary>
    public class AsyncResourceLoader
    {
        private string? _resourceAddress;

        /// <param name="applyFallback">실제 리소스를 적용하기 전에 대체 리소스를 적용하는 동작.</param>
        /// <param name="applyActual">로드된 리소스를 적용하는 동작.</param>
        /// <param name="applyActualCondition">적용 여부를 확인하는 함수. null 이면 무조건 적용.</param>
        public void LoadAndApplyResource<TObject>(string resourceAddress, Action? applyFallback, Action<TObject>? applyActual, Func<bool>? applyActualCondition) where TObject : UnityEngine.Object
        {
            if (_resourceAddress == resourceAddress)
            {
                return;
            }
            _resourceAddress = resourceAddress;

            applyFallback?.Invoke();

            var resource = ResourcePool.Instance.LoadResource<TObject>(resourceAddress);
            if (resource == null)
            {
                return;
            }

            if (applyActualCondition?.Invoke() ?? true)
            {
                applyActual?.Invoke(resource);
            }
        }
    }
}
