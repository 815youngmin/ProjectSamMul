using UnityEngine;
using UnityEngine.SceneManagement;
using SamMul.ObjectPools;

namespace SamMul.GameClients.Stages.IndicatorObjects
{
    public enum IndicatorType
    { 
        SquareAttackRange,
        DashAttackRange,
        DirectionalSquareRange,
        CircularAttackRange,
        BoneToTarget,
        BlinkCircularAttackRange,
    }


    public abstract class IndicatorObjectBase : MonoBehaviour, IPoolible<IndicatorType>
    {
        /// <summary>
        /// 오브젝트가 유효한지 여부. <c>false</c>가 되면, 스테이지에서 제거된다.
        /// </summary>
        public abstract bool IsAlive { get; }

        public IndicatorType IndicatorObjectType { get; private set; }

        public virtual void UpdateLogic(Stage stage, float deltaTime) { }
        /// <summary>
        /// 오브젝트 풀링 시스템에서 처음 할당될 때 공용 리소스들을 초기화합니다.
        /// 재활용되며 사용되는 시점에 인스턴스를 초기화하는 함수는 <see cref="InitializeBase"/> 입니다.
        /// </summary>
        /// <remarks>
        /// <see cref="IndicatorObjectBase"/>를 상속받는 구체화클래스에는 모두 `AllocateSharedResourcesFor{자신클래스이름}(파라미터)`으로 초기화 코드를 만들고,
        /// 해당 초기화 코드의 상단에서 부모클래스의 `AllocateSharedResources{부모클래스}(파라미터)`를 호출해주세요.
        /// 유니티 MonoBehaviour시스템 내에서 생성자를 사용할 수 없으므로, 이와 같은 방법으로 자원 관리합니다.
        /// </remarks>
        protected void AllocateSharedResourcesForBase(IndicatorType type)
        {
            IndicatorObjectType = type;

            this.gameObject.layer = LayerMask.NameToLayer("UI");
        }

        #region IPoolible interfaces
        Scene IPoolible<IndicatorType>.RelatedScene => this.gameObject.scene;

        IndicatorType IPoolible<IndicatorType>.PoolKey => IndicatorObjectType;

        public virtual void PuttingBackToPool()
        {
        }
        #endregion 
    }

}
