using Z.Animations.Placeholder;
using UnityEngine;
using UnityEngine.TextCore.Text;
using Z.ResourcePools;

namespace Z.GameClients.Stages.Characters.Shields
{
    public class PlayerShield : Shield
    {
        private static readonly string SHIELD_SKELETON_DATA_PATH = "Stages/GradeEffects/shield_SkeletonData.asset";

        private GameObject _shiledObject; 
        private MeshRenderer _meshRenderer;

        /// <summary>
        /// 방어막을 생성합니다.
        /// </summary>
        /// <param name="owner">
        /// 방어막으로 보호받는 캐릭터입니다.
        /// </param>
        public PlayerShield(Character owner) 
            : base(owner)
        {
            _owner = owner;
            _shiledObject = new GameObject("PlayerShiledObject");
            _shiledObject.transform.SetParent(_shieldToggleObject.transform, false);
            _shiledObject.transform.localPosition = owner.CenterPos - owner.Pos;
            _shiledObject.transform.localScale = 0.5f * Vector3.one;

            _shiledObject.AddComponent<MeshFilter>();
            _meshRenderer = _shiledObject.AddComponent<MeshRenderer>();
            _meshRenderer.sortingLayerID = SortingLayer.NameToID("Object");

            var skeletonAnimation = _shiledObject.AddComponent<SkeletonAnimation>();
            skeletonAnimation.skeletonDataAsset = ResourcePool.Instance.LoadResource<SkeletonDataAsset>(SHIELD_SKELETON_DATA_PATH);
            skeletonAnimation.Initialize(true);
            skeletonAnimation.AnimationState.SetAnimation(0, "shield", loop: true);
        }

        /// <summary>
        /// 매 프레임마다 방어막을 업데이트합니다.
        /// </summary>
        public override void UpdateLogic()
        {
            if (!IsActive)
            {
                return;
            }

            _meshRenderer.sortingOrder = (int)(_owner.transform.position.y * -100.0f) + 1;
        } 
    }
}
