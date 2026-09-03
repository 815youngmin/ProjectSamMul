using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class SpaceShipIceAreaEffectObject : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + _lifeTime;

        private Character _owner;
        private float _damage;

        private float _createdAt;
        private float _lifeTime;

        private GameObject _body;
        private SkeletonAnimation _skeletonAnimation;

        private static readonly string AttackAnimationName = "barrage_fire_bllizard";
        private static readonly string RepeatAnimationName = "barrage_fire_ice";
        private static readonly string EndAnimationName = "barrage_fire_ice_delet";
        private static readonly float BasicAttackRadius = 0.6f;
        private static readonly float SlowPeriod = 0.25f;

        private Animation _attackAnimation;
        private Animation _repeatAnimation;
        private Animation _endAnimation;

        private float _hitAt;
        private float _slowAt;
        private float _layerOrderChangeAt;
        private float _hitTimeOnAnimation;
        private float _attackRadius;
        private float _slowDuration;
        private float _slowParameter;
        private float _areaEffectDuration;

        private List<Character> _slowCharacters;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.SpaceShipIceObject);
            _body = ResourcePool.Instance.InstantiateFromResource("Stages/SkillEffects/SpaceShip/SpaceShipBllizard.prefab");
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;
            _body.transform.localScale = Vector2.one;
            _skeletonAnimation = _body.GetComponent<SkeletonAnimation>();
            _attackAnimation = _skeletonAnimation.skeleton.Data.FindAnimation(AttackAnimationName);
            _repeatAnimation = _skeletonAnimation.skeleton.Data.FindAnimation(RepeatAnimationName);
            _endAnimation = _skeletonAnimation.skeleton.Data.FindAnimation(EndAnimationName);

            var hitFrameEventData = _skeletonAnimation.Skeleton.Data.FindEvent("hit");
            var hitEvent =  CharacterAnimationController.FindEventInAnimationTimeline(_attackAnimation, hitFrameEventData);
            _hitTimeOnAnimation = hitEvent.Time;

        }

        public void Initialize(
            Character owner,
            Vector2 attackPosition,
            float attackRadius,
            float damage,
            float areaEffectDuration,
            float slowDuration,
            float slowParameter
            )
        {
            base.InitializeAreaObject(owner.Alliance);
            float now = Time.time;

            _owner = owner;
            this.transform.position = attackPosition;
            _attackRadius = attackRadius;
            this.transform.localScale = Vector3.one * attackRadius / BasicAttackRadius;
            _damage = damage;
            _areaEffectDuration = areaEffectDuration;
            _slowDuration = slowDuration;
            _slowParameter = slowParameter;

            _createdAt = now;
            _lifeTime = _attackAnimation.Duration + _areaEffectDuration + _endAnimation.Duration;
            _hitAt = now + _hitTimeOnAnimation;
            _slowAt = _hitAt;
            _layerOrderChangeAt = now + _attackAnimation.Duration;
            _slowCharacters = new List<Character>();

            _skeletonAnimation.AnimationState.SetAnimation(0, _attackAnimation, false);
            _skeletonAnimation.AnimationState.AddAnimation(0, _repeatAnimation, true, 0f);
            _skeletonAnimation.AnimationState.AddAnimation(0, _endAnimation, false, _areaEffectDuration);

            _skeletonAnimation.GetComponent<MeshRenderer>().sortingLayerID = SortingLayer.NameToID("HighParticle");
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;
            if(_hitAt <= now)
            {
                var targetArea = new CircularTargetArea(this.transform.position, _attackRadius);
                CombatSystem.HitOnTargetArea(stage, targetArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, Vector2.zero, 0f, null, null, null);

                _hitAt = float.MaxValue;
            }

            if(_slowAt <= now)
            {
                var targetArea = new CircularTargetArea(this.transform.position, _attackRadius);
                stage.FindAliveCharactersInArea(_owner.Alliance.ToEnemyAlliance(), targetArea, _slowCharacters);

                foreach (var character in _slowCharacters)
                {
                    character.StatusEffects.AddOrUpdateStatusEffect(stage, character, Characters.StatusEffects.StatusEffectType.SlowMove, _slowDuration, now, _slowParameter);
                }
                _slowAt = now + SlowPeriod;
                _slowCharacters.Clear();
            }

            if(_layerOrderChangeAt <= now)
            {
                _skeletonAnimation.GetComponent<MeshRenderer>().sortingLayerID = SortingLayer.NameToID("LowParticle");
                _layerOrderChangeAt = float.MaxValue;
            }

        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
        }

    }

}
