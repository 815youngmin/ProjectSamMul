using Z.Animations.Placeholder;
using Animation = Z.Animations.Placeholder.Animation;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.Characters.Animations;
using Z.ResourcePools;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class ThreekingdomArcherAttackObject : AreaEffectObjectBase
    {
        public enum DirectionType
        {
            Right,
            Left,
            Up,
            Down
        }

        public override bool IsAlive => Time.time <= _createdAt + _lifeTime;

        private Character _owner;
        private float _damage;

        private float _createdAt;
        private float _lifeTime;

        private GameObject _body;
        private SkeletonAnimation _skeletonAnimation;
        private Bone _arrowBone;

        private float _bodyDamage;
        private float _projectileDamage;
        private float _projectileSpeed;
        private float _projectileAcceleration;
        private float _projectileAliveDistance;
        private Vector2 _projectileDirection;


        private float _hitTimeOnAnimation;
        private float _shotAt;
        private EventData _hitFrameEvent;
        private DirectionType _directionType;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.ThreekingdomArcher);
            _body = ResourcePool.Instance.InstantiateFromResource("Stages/CommonAttackEffect/ThreeKingdomArcher/ThreeKingdomAcher.prefab");
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;
            _body.transform.localScale = Vector2.one * 0.75f;
            _skeletonAnimation = _body.GetComponent<SkeletonAnimation>();
            _hitFrameEvent =_skeletonAnimation.Skeleton.Data.FindEvent("hit");
        }

        public void Initialize(
            Character owner,
            Vector2 spawnPosition,
            float bodyDamage,
            float projectileDamage,
            float projectileSpeed,
            float projectileAcceleration,
            float projectileAliveDistance,
            DirectionType direction
            )
        {
            base.InitializeAreaObject(owner.Alliance);
            float now = Time.time;
            _createdAt = now;

            _owner = owner;
            this.transform.position = spawnPosition;

            _bodyDamage = bodyDamage;
            _projectileDamage = projectileDamage;
            _projectileSpeed = projectileSpeed;
            _projectileAcceleration = projectileAcceleration;
            _projectileAliveDistance = projectileAliveDistance;
            _directionType = direction;

            Animation animation = null;
            if(_directionType == DirectionType.Left)
            {
                animation = _skeletonAnimation.Skeleton.Data.FindAnimation("appear_l");
                _arrowBone = _skeletonAnimation.Skeleton.FindBone("wa");
                _projectileDirection = Vector2.left;
            }
            else if (_directionType == DirectionType.Right)
            {
                animation = _skeletonAnimation.Skeleton.Data.FindAnimation("appear_r");
                _arrowBone = _skeletonAnimation.Skeleton.FindBone("wa");
                _projectileDirection = Vector2.right;
            }
            else if (_directionType == DirectionType.Up)
            {
                animation = _skeletonAnimation.Skeleton.Data.FindAnimation("appear_b");
                _arrowBone = _skeletonAnimation.Skeleton.FindBone("b_wa");
                _projectileDirection = Vector2.up;
            }
            else if (_directionType == DirectionType.Down)
            {
                animation = _skeletonAnimation.Skeleton.Data.FindAnimation("appear_f");
                _arrowBone = _skeletonAnimation.Skeleton.FindBone("f_wa");
                _projectileDirection = Vector2.down;
            }


            _lifeTime = animation.Duration;
            _hitTimeOnAnimation = SpineMonsterAnimationController.FindEventInAnimationTimeline(animation, _hitFrameEvent).Time;

            _skeletonAnimation.AnimationState.SetAnimation(0, animation, false);

            _shotAt = now + _hitTimeOnAnimation;
            _skeletonAnimation.GetComponent<MeshRenderer>().sortingOrder = (int)(transform.position.y * -100.0f);
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;
            if(_shotAt <= now )
            {
                this.DoShot(stage);
                _shotAt = float.MaxValue;
            }
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
        }

        private void DoShot(Stage stage)
        {
            Vector2 throwPosition = _arrowBone.GetWorldPosition(this.transform);
            stage.CreateProjectile(
                    "Stages/Projectiles/ArrowRadius0_3.prefab",
                    _owner.Alliance,
                    _owner,
                    _projectileDamage,
                    0f,
                    throwPosition,
                    _projectileDirection,
                    _projectileSpeed,
                    _projectileAcceleration,
                    collidingRadius: 0.3f,
                    _projectileAliveDistance,
                    hitChances: 1,
                    splitCount: 0,
                    isRemovableBySpinBladeObject: false,
                    hitSoundPrefabPath: string.Empty);
        }

    }

}
