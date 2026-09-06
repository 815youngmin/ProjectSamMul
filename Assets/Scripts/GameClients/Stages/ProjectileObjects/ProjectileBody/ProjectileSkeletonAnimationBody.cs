using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;
using UnityEngine;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.ProjectileObjects.ProjectileBody
{
    public class ProjectileSkeletonAnimationBody : ProjectileBodyBase
    {
        private SkeletonAnimation _skeletonAnimation;
        private MeshRenderer _meshRenderer;
        private SpriteRenderer _shadowRenderer;

        private bool _isTimeScaleAcceleration;
        private float _timeScaleAcceleration;

        public void AllocateSharedResources(SkeletonAnimation skeletonAnimation)
        {
            _skeletonAnimation = skeletonAnimation;
            _skeletonAnimation.Initialize(true);
            _meshRenderer = _skeletonAnimation.GetComponent<MeshRenderer>();

            _skeletonAnimation.state.Data.DefaultMix = 0.000000f;
            AllocateShadowComponent();
        }

        private void AllocateShadowComponent()
        {
            var shadowObject = new GameObject("Shadow");
            shadowObject.transform.SetParent(this._body.transform, worldPositionStays: false);

            var shadow = shadowObject.AddComponent<SpriteRenderer>();

            shadow.sprite = ResourcePool.Instance.LoadResource<Sprite>("Stage/Common/ItemShadow.png");
            shadow.color = new Color(shadow.color.r, shadow.color.g, shadow.color.b, 0.50f);
            shadow.sortingLayerID = SortingLayer.NameToID("LowShadow");
            shadow.drawMode = SpriteDrawMode.Simple;
            float scale = (6.0f / shadow.size.x);
            shadow.transform.localScale = new Vector2(scale, scale * 0.8f);

            _shadowRenderer = shadow;
            // NOTE: 그림자가 필요한 오브젝트들만 켜서 사용하도록 한다.
            shadowObject.SetActive(false);
        }

        public override void Initialize()
        {
            base.Initialize();
            _skeletonAnimation.AnimationState.SetAnimation(0, "idle", loop: true);
        }

        public override void PuttingBackToPool()
        {
            _skeletonAnimation.AnimationState.ClearTracks();
            _skeletonAnimation.skeleton.SetToSetupPose();
            _skeletonAnimation.Update(0.0f);
        }

        public override void UpdateLogic(float deltaTime)
        {
            _meshRenderer.sortingOrder = (int)(_body.transform.position.y * -100.0f);

            if(_isTimeScaleAcceleration)
            {
                _skeletonAnimation.AnimationState.TimeScale += (_timeScaleAcceleration * deltaTime);
            }
        }

        public Animation FindAnimation(string animationName)
        {
            return _skeletonAnimation.skeleton.Data.FindAnimation(animationName);
        }

        public void PlayAnimation(Animation animation, bool isLoop, float duration)
        {
            Debug.Assert(animation != null);

            if (!isLoop)
            {
                float timeScale = animation.Duration / duration;
                var entry = _skeletonAnimation.AnimationState.SetAnimation(0, animation, isLoop);
                entry.TimeScale = timeScale;
            }

            _isTimeScaleAcceleration = false;
        }

        public void PlayAnimation(Animation beginAnimation, Animation repeatAnimation, float startTimeScale, float timeScaleAcceleration)
        {
            Debug.Assert(beginAnimation != null);
            Debug.Assert(repeatAnimation != null);

            Debug.Assert(null != beginAnimation || null != repeatAnimation);

            _timeScaleAcceleration = timeScaleAcceleration;

            _skeletonAnimation.AnimationState.SetAnimation(0, beginAnimation, false);
            _skeletonAnimation.AnimationState.AddAnimation(0, repeatAnimation, true, 0.0f);

            _skeletonAnimation.AnimationState.TimeScale = startTimeScale;
            _isTimeScaleAcceleration = true;
        }

        public override void Fade(float value)
        {
            _skeletonAnimation.Skeleton.SetColor(new Color(1.0f, 1.0f, 1.0f, value));
        }
    }
}
