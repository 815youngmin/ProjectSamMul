using Z.Animations.Placeholder;
using System;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.PCs;
using Z.GameClients.Stages.Characters.StatusEffects;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class TentiSweepAreaEffectObject : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + _animationDuration;

        private PlayerCharacter _owner;
        private SkeletonAnimation _attackEffectBody;

        private Vector2 _attackDirection;
        private Vector2 _attackSize;
        private float _damage;
        private float _areaRatio;
        private float _knockBackPower;
        private float _hitTimeOnAnimation;
        private float _animationDuration;
        private float _createdAt;

        public readonly static float BaseAttackAreaWidth = 5.0f;
        public readonly static float BaseAttackAreaHeight = 0.75f;

        private bool _isHitFired;
        private string _hitSoundPrefabPath;
        private float _normalMonsterStunDuration;
        private HashSet<Character> _hittedCharacters = new HashSet<Character>();

        public void AllocateSharedResources(AreaEffectType areaEffectType)
        {
            Debug.Assert(areaEffectType == AreaEffectType.TentiSweep_Default || 
                         areaEffectType == AreaEffectType.TentiSweep_Hina ||
                         areaEffectType == AreaEffectType.TentiSweep_Bongjun
                         );
            base.AllocateSharedResourcesForBase(areaEffectType);

            var attackEffectBodyPath = areaEffectType switch
            {
                AreaEffectType.TentiSweep_Default => "Stages/Characters/TentiAttackEffects/TentiBasicAttack.prefab",
                AreaEffectType.TentiSweep_Hina => "Stages/Characters/TentiAttackEffects/HinaBasicAttack.prefab",
                AreaEffectType.TentiSweep_Bongjun=> "Stages/Characters/TentiAttackEffects/BongjunBasicAttack.prefab",
                _ => throw new NotSupportedException($"AreaEffectType {areaEffectType}이 텐티 스윕이 아닙니다."),
            };
            _attackEffectBody = ResourcePool.Instance.InstantiateFromResource<SkeletonAnimation>(attackEffectBodyPath);
            _attackEffectBody.transform.SetParent(this.gameObject.transform);
            _attackEffectBody.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            _attackEffectBody.transform.localScale = Vector3.one;
            _attackEffectBody.skeleton.SetToSetupPose();
            _attackEffectBody.gameObject.SetActive(false);

            var hitEventData = _attackEffectBody.Skeleton.Data.FindEvent("hit");
            var animation = _attackEffectBody.Skeleton.Data.FindAnimation("animation");
            var hitEvent = CharacterAnimationController.FindEventInAnimationTimeline(animation, hitEventData);
            _hitTimeOnAnimation = hitEvent.Time;
            _animationDuration = animation.Duration;

        }

        public void Initialize(
           AllianceType alliance,
           PlayerCharacter owner,
           Vector2 attackDirection,
           float damage,
           float areaRatio,
           float knockBackPower,
           float normalMonsterStunDuration,
           string hitSoundPrefabPath)
        {
            base.InitializeAreaObject(alliance);

            _owner = owner;
            _attackDirection = attackDirection;
            _attackSize = new Vector2(BaseAttackAreaWidth * areaRatio, BaseAttackAreaHeight * areaRatio);
            _damage = damage;
            _areaRatio = areaRatio;
            _knockBackPower = knockBackPower;
            _normalMonsterStunDuration = normalMonsterStunDuration;

            _createdAt = Time.time;

            this.gameObject.transform.SetParent(owner.gameObject.transform);
            this.gameObject.transform.localPosition = Vector3.zero;

            _attackEffectBody.gameObject.SetActive(true);
            _attackEffectBody.AnimationState.SetAnimation(0, "animation", loop: false);
            _attackEffectBody.gameObject.transform.localScale = new Vector3(_areaRatio, _areaRatio, _areaRatio);

            _isHitFired = false;

            _hitSoundPrefabPath = hitSoundPrefabPath;

            this.RotateBodyImageToMoveDirection();
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;

            if (_isHitFired)
            {
                return;
            }

            if (now > _createdAt + _hitTimeOnAnimation)
            {
                this.AttackToTargetArea(stage);
                _isHitFired = true;
            }
        }

        private void AttackToTargetArea(Stage stage)
        {
            var center = _owner.CenterPos + _attackDirection * (0.5f * _attackSize.x);
            float angle = Mathf.Atan2(_attackDirection.y, _attackDirection.x) * Mathf.Rad2Deg;
            SquareTargetArea targetArea = new SquareTargetArea(center, _attackSize, angle);
            CombatSystem.HitOnTargetArea(stage, targetArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, _owner.Pos, _knockBackPower, _hittedCharacters, null, _hitSoundPrefabPath);

            if (_normalMonsterStunDuration > 0)
            {
                foreach (Character target in _hittedCharacters)
                {
                    if (!target.IsBoss && !target.IsElite)
                    {
                        target.StatusEffects.AddOrUpdateStatusEffect(stage, target, StatusEffectType.Stun, duration: _normalMonsterStunDuration, Time.time, 0f);
                    }
                }
            }
        }

        private void RotateBodyImageToMoveDirection()
        {
            Vector2 direction = _attackDirection;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _attackEffectBody.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            _hittedCharacters.Clear();
        }
    }
}

