using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.Characters.PCs;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class ShootingStarExplosionAreaEffectObject : AreaEffectObjectBase
    {
        public override bool IsAlive =>Time.time <= _createdAt + _lifeTime;

        private Character _owner;
        private float _damage;

        private float _createdAt;
        private float _lifeTime;

        private GameObject _transcendBody;
        private GameObject _normalBody;

        private float _knockbackPower;
        private float _radius;
        private Vector2 _direction;
        private bool _isTranscend;

        private SkeletonAnimation _normalSkeletonAnimation;
        private SkeletonAnimation _transcendSkeletonAnimation;

        private Animation _normalExplosionAnimation;
        private Animation _transcendExplosionAnimation;

        private HashSet<Character> _hittedCharacters = new HashSet<Character>();

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.ShootingStarExplosion);
            _normalBody = ResourcePool.Instance.InstantiateFromResource("Stages/AreaEffects/ShootingStar/FX_ShootingStar_Bomb_N.prefab");
            _normalBody.transform.SetParent(this.transform);
            _normalBody.transform.localPosition = Vector2.zero;
            _normalBody.transform.localScale = Vector2.one;

            _normalSkeletonAnimation = _normalBody.GetComponentInChildren<SkeletonAnimation>();
            _normalExplosionAnimation = _normalSkeletonAnimation.skeleton.Data.FindAnimation("Begin");

            _transcendBody = ResourcePool.Instance.InstantiateFromResource("Stages/AreaEffects/ShootingStar/FX_ShootingStar_Bomb_S.prefab");
            _transcendBody.transform.SetParent(this.transform);
            _transcendBody.transform.localPosition = Vector2.zero;
            _transcendBody.transform.localScale = Vector2.one;

            _transcendSkeletonAnimation = _transcendBody.GetComponentInChildren<SkeletonAnimation>();
            _transcendExplosionAnimation = _transcendSkeletonAnimation.skeleton.Data.FindAnimation("Begin");


        }

        public void Initialize(
            PlayerCharacter owner,
            float damage,
            float knobackPower,
            float radius,
            Vector2 position,
            Vector2 direction,
            bool isTranscend
            )
        {
            base.InitializeAreaObject(owner.Alliance);
            float now = Time.time;
            _createdAt = now;
            _owner = owner;
            _damage = damage;
            _knockbackPower = knobackPower;
            _radius = radius;
            _isTranscend = isTranscend;
            _direction = direction;

            this.transform.position = position;

            if(isTranscend)
            {
                _lifeTime = _transcendExplosionAnimation.Duration;
                _transcendSkeletonAnimation.AnimationState.SetAnimation(0, _transcendExplosionAnimation, false);
                _transcendBody.transform.localScale = Vector3.one * _radius / 9f;
                _normalBody.gameObject.SetActive(false);
                _transcendBody.gameObject.SetActive(true);
                transform.right = _direction;
            }
            else
            {
                _lifeTime = _normalExplosionAnimation.Duration;
                _normalSkeletonAnimation.AnimationState.SetAnimation(0, _normalExplosionAnimation, false);

                _normalBody.gameObject.SetActive(true);
                _normalBody.transform.localScale = Vector3.one * _radius / 1.5f;
                _transcendBody.gameObject.SetActive(false);

            }

        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            if(_isTranscend)
            {
                CircularSectorTargetArea targetArea = new CircularSectorTargetArea(this.transform.position, _direction, _radius, angle: 120f);
                CombatSystem.HitOnTargetArea(stage, targetArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, this.transform.position, _knockbackPower, _hittedCharacters, _hittedCharacters, null);
            }
            else
            {
                CircularTargetArea targetArea = new CircularTargetArea(this.transform.position, _radius);
                CombatSystem.HitOnTargetArea(stage, targetArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, this.transform.position, _knockbackPower, _hittedCharacters, _hittedCharacters, null);
            }
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _hittedCharacters.Clear();

            _transcendSkeletonAnimation.AnimationState.ClearTracks();
            _transcendSkeletonAnimation.skeleton.SetToSetupPose();
            _transcendSkeletonAnimation.Update(0);

            _normalSkeletonAnimation.AnimationState.ClearTracks();
            _normalSkeletonAnimation.skeleton.SetToSetupPose();
            _normalSkeletonAnimation.Update(0);

        }
    }

}
