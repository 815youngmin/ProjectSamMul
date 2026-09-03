using UnityEngine.SceneManagement;

namespace Z.ObjectPools
{
    /// <summary>
    /// <see cref="ObjectPool{TPoolKey, TValue}"/>에 넣을 수 있는 오브젝트의 인터페이스입니다.
    /// </summary>
    public interface IPoolible<TPoolKey>
    {
        /// <summary>
        /// 풀에 돌아가기 직전에 호출됩니다. 오브젝트를 정리해주세요.
        /// </summary>
        void PuttingBackToPool();

        /// <summary>
        /// 이 오브젝트가 생성된 씬. 씬 전환 시 정리 대상을 고르는 데 사용합니다.
        /// </summary>
        Scene RelatedScene { get; }

        TPoolKey PoolKey { get; }
    }
}
