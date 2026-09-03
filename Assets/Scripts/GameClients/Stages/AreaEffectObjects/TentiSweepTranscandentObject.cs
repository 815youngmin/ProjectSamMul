using SamMul.Animations.Placeholder;
using System;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.PCs;
using SamMul.GameClients.Stages.Characters.StatusEffects;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class TentiSweepTranscandentObject : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + _animationDuration;

        private PlayerCharacter _owner;
        private SkeletonAnimation _attackEffectBody;

        private Vector2 _imageDirection;
        private Vector2 _attackSize;
        private float _damage;
        private float _areaRatio;
        private float _knockBackPower;
        private float _normalMonsterStunDuration;
        private float _createdAt;

        public static readonly float BaseAttackAreaWidth = 7f;
        public static readonly float BaseAttackAreaHeight = 1.4f;

        private float _hitTimeOnAnimation;
        private float _animationDuration;

        private bool _isHitFired;

        private List<(float, Vector2)> _directions;

        private string _hitSoundPrefabPath;

        public void AllocateSharedResources(AreaEffectType areaEffectType)
        {
            Debug.Assert(
                areaEffectType == AreaEffectType.TentiSweepTranscendent_Default ||
                areaEffectType == AreaEffectType.TentiSweepTranscendent_Hina ||
                areaEffectType == AreaEffectType.TentiSweepTranscendent_Bongjun);
            base.AllocateSharedResourcesForBase(areaEffectType);

            var attackEffectBodyPath = areaEffectType switch
            {
                AreaEffectType.TentiSweepTranscendent_Default => "Stages/Characters/TentiAttackEffects/TentiBasicAttack_S.prefab",
                AreaEffectType.TentiSweepTranscendent_Hina => "Stages/Characters/TentiAttackEffects/HinaBasicAttack_S.prefab",
                AreaEffectType.TentiSweepTranscendent_Bongjun => "Stages/Characters/TentiAttackEffects/BongjunBasicAttack_S.prefab",
                _ => throw new NotSupportedException($"AreaEffectType {areaEffectType}이 텐티 스윕이 아닙니다."),
            };
            _attackEffectBody = ResourcePool.Instance.InstantiateFromResource<SkeletonAnimation>(attackEffectBodyPath);
            _attackEffectBody.transform.SetParent(this.gameObject.transform);
            _attackEffectBody.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            _attackEffectBody.transform.localScale = Vector3.one;
            _attackEffectBody.skeleton.SetToSetupPose();
            _attackEffectBody.gameObject.SetActive(false);

            var hitEventData = _attackEffectBody.Skeleton.Data.FindEvent("hit");
            var animation = _attackEffectBody.Skeleton.Data.FindAnimation("attack");
            var hitEvent = CharacterAnimationController.FindEventInAnimationTimeline(animation, hitEventData);
            _hitTimeOnAnimation = hitEvent.Time;
            _animationDuration = animation.Duration;

        }


        public void Initialize(
           Stage stage,
           AllianceType alliance,
           PlayerCharacter owner,
           Vector2 imageDirection,
           float damage,
           float areaRatio,
           float knockBackPower,
           List<(float, Vector2)> directions,
           float normalMonsterStunDuration,
           string hitSoundPrefabPath)
        {
            base.InitializeAreaObject(alliance);

            _owner = owner;
            _imageDirection = imageDirection;
            _attackSize = new Vector2(BaseAttackAreaWidth * areaRatio, BaseAttackAreaHeight * areaRatio);
            _damage = damage;
            _areaRatio = areaRatio;
            _knockBackPower = knockBackPower;
            _directions = directions;
            _normalMonsterStunDuration = normalMonsterStunDuration;

            _createdAt = Time.time;
            _hitSoundPrefabPath = hitSoundPrefabPath;

            this.gameObject.transform.SetParent(owner.gameObject.transform);
            this.gameObject.transform.localPosition = Vector3.zero;

            _attackEffectBody.gameObject.SetActive(true);
            _attackEffectBody.AnimationState.SetAnimation(0, "attack", loop: false);
            _attackEffectBody.gameObject.transform.localScale = new Vector3(_areaRatio, _areaRatio, _areaRatio);

            _isHitFired = false;

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
            HashSet<Character> hittedCharacters = new HashSet<Character>();

            foreach ((float angle, Vector2 direction) in _directions)
            {
                var center = _owner.CenterPos + direction * (0.5f * _attackSize.x);
                SquareTargetArea targetArea = new SquareTargetArea(center, _attackSize, angle);
                CombatSystem.HitOnTargetArea(stage, targetArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, _owner.Pos, _knockBackPower, hittedCharacters, null, _hitSoundPrefabPath);

                if (_normalMonsterStunDuration > 0)
                {
                    foreach (Character target in hittedCharacters)
                    {
                        if (!target.IsBoss && !target.IsElite)
                        {
                            target.StatusEffects.AddOrUpdateStatusEffect(stage, target, StatusEffectType.Stun, duration: _normalMonsterStunDuration, Time.time, 0f);
                        }
                    }
                }
                hittedCharacters.Clear();
            }
        }

        private void RotateBodyImageToMoveDirection()
        {
            Vector2 direction = _imageDirection;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _attackEffectBody.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

}
