using Z.Animations.Placeholder;
using Animation = Z.Animations.Placeholder.Animation;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.ResourcePools;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class EnchantingGlowBodyObject : AreaEffectObjectBase
    {
        public override bool IsAlive =>Time.time <= _createdAt + _duration;

        private Character _owner;

        private float _createdAt;
        private Vector2 _startPosition;
        private Vector2 _direction;
        private float _attackRadius;
        private float _duration;

        private GameObject _body;
        private SkeletonAnimation _skeletonAnimation;
        private Animation _appearAnimation;
        private Animation _repeatAnimation;
        private Animation _endAnimation;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.EnchantingGlowBodyObject);
            _body = ResourcePool.Instance.InstantiateFromResource("Stages/AreaEffects/MambaSkill/MambaNormalSkillObject.prefab");
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;

            _skeletonAnimation = _body.GetComponent<SkeletonAnimation>();

            _appearAnimation = _skeletonAnimation.skeleton.Data.FindAnimation("fire");
            _repeatAnimation = _skeletonAnimation.skeleton.Data.FindAnimation("idle");
            _endAnimation = _skeletonAnimation.skeleton.Data.FindAnimation("end");

        }

        public void Initialize(
            Character owner,
            Vector2 startPosition,
            Vector2 direction,
            float attackRadius,
            float duration
            )
        {
            base.InitializeAreaObject(owner.Alliance);
            float now = Time.time;
            _createdAt = now;

            _owner = owner;
            _startPosition = startPosition;
            _direction = direction;
            _attackRadius = attackRadius;
            _duration = duration;

            this.transform.position = _startPosition;
            _body.transform.rotation = Quaternion.Euler(0.0f, 0.0f, Vector2.SignedAngle(Vector2.right, _direction));
            _body.transform.localScale = Vector3.one * _attackRadius / 9f;
 
            _skeletonAnimation.AnimationState.SetAnimation(0, _appearAnimation, false);
            var repeatAnimation = _skeletonAnimation.AnimationState.AddAnimation(0, _repeatAnimation, true, 0f);
            repeatAnimation.TimeScale = 0.45f;
            _skeletonAnimation.AnimationState.AddAnimation(0, _endAnimation, false, _duration - _appearAnimation.Duration - _endAnimation.Duration);
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {

        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _skeletonAnimation.AnimationState.SetEmptyAnimation(0, 0f);
        }

    }

}
