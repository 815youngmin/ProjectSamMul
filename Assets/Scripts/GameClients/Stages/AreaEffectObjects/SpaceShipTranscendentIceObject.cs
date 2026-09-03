using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;
using SamMul.UnityHelpers;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class SpaceShipTranscendentIceObject : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + _lifeTime;

        private Character _owner;
        private float _damage;

        private float _createdAt;
        private float _lifeTime;
        private float _attackDelay;
        private float _hitAt;
        private float _stunDuration;
        private float _scale;

        private GameObject _body;
        private SkeletonAnimation _skeletonAnimation;
        private static readonly string AttackAnimationName = "bllizard_spear";
        private Animation _attackAnimation;
        private float _hitTimeOnAnimation;
        private HashSet<Character> _hittedCharacters;

        private static readonly Vector2 HitBoxOffset = new Vector2(0, 0.5f);
        private static readonly Vector2 HitBox1Size = new Vector2(7.5f, 6f);
        private static readonly Vector2 HitBox2Size = new Vector2(13f, 3f);
        private static readonly Vector2 HitBox3Size = new Vector2(3f, 8f);



        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.SpaceShipTranscendentIceObject);
            _body = ResourcePool.Instance.InstantiateFromResource("Stages/SkillEffects/SpaceShip/SpaceShipSpear.prefab");
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;
            _body.transform.localScale = Vector2.one;
            _skeletonAnimation = _body.GetComponent<SkeletonAnimation>();
            _attackAnimation = _skeletonAnimation.skeleton.Data.FindAnimation(AttackAnimationName);
            var hitFrameEventData = _skeletonAnimation.Skeleton.Data.FindEvent("hit");
            var hitEvent = CharacterAnimationController.FindEventInAnimationTimeline(_attackAnimation, hitFrameEventData);
            _hitTimeOnAnimation = hitEvent.Time;

        }

        public void Initialize(
            Character owner,
            Vector2 attackPosition,
            float attackDelay,
            float stunDuration,
            float damage,
            float scale
            )
        {
            base.InitializeAreaObject(owner.Alliance);
            float now = Time.time;
            _createdAt = now;
            _lifeTime = _attackAnimation.Duration + attackDelay;

            _owner = owner;
            _attackDelay = attackDelay;
            _stunDuration = stunDuration;
            _damage = damage;
            _scale = scale;

            this.transform.position = attackPosition;
            this.transform.localScale = Vector3.one * scale;
            _skeletonAnimation.AnimationState.SetEmptyAnimation(0, 0f);
            _skeletonAnimation.AnimationState.AddAnimation(0, _attackAnimation, false, attackDelay);
            _hittedCharacters = new HashSet<Character>();
            _hitAt = now + attackDelay + _hitTimeOnAnimation;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;
            if (_hitAt <= now)
            {
                var targetArea1 = new SquareTargetArea(this.transform.position + (Vector3)(HitBoxOffset * _scale), HitBox1Size * _scale, 0f);
                CombatSystem.HitOnTargetArea(stage, targetArea1, _owner, _damage, CombatSystem.KnockBackType.Pivot, Vector2.zero, 0f, _hittedCharacters, _hittedCharacters, null);
                
                var targetArea2 = new SquareTargetArea(this.transform.position + (Vector3)(HitBoxOffset * _scale), HitBox2Size * _scale, 0f);
                CombatSystem.HitOnTargetArea(stage, targetArea2, _owner, _damage, CombatSystem.KnockBackType.Pivot, Vector2.zero, 0f, _hittedCharacters, _hittedCharacters, null);
                
                var targetArea3 = new SquareTargetArea(this.transform.position + (Vector3)(HitBoxOffset * _scale), HitBox3Size * _scale, 0f);
                CombatSystem.HitOnTargetArea(stage, targetArea3, _owner, _damage, CombatSystem.KnockBackType.Pivot, Vector2.zero, 0f, _hittedCharacters, _hittedCharacters, null);
                 
                foreach (var hittedCharacter in _hittedCharacters)
				{
					hittedCharacter.StatusEffects.AddOrUpdateStatusEffect(stage, hittedCharacter, Characters.StatusEffects.StatusEffectType.Stun, _stunDuration, now, 0f);
				}

				_hitAt = float.MaxValue;

				UnityGlobal.Sounds.PlayBySoundPrefab("Sounds/SoundEffects/PCs/SpaceShipTranscendentIce_SFX.prefab", transform.position);
			}
		}

		public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _skeletonAnimation.skeleton.SetToSetupPose();
            _skeletonAnimation.AnimationState.SetEmptyAnimation(0, 0f);
            
        }

    }

}
