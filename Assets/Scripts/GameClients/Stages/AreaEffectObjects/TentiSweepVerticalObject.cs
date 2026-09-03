#nullable enable
using Z.Animations.Placeholder;
using System;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.PCs;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class TentiSweepVerticalObject : AreaEffectObjectBase
    {
        private readonly Vector2 DEFAULT_ATTACK_AREA = new Vector2(1.0f, 3.5f);

        public override bool IsAlive => _isAlive;

        private SkeletonAnimation _attackEffectBody = null!;
        private HashSet<Character> _hittedMonsters = null!;

        private PlayerCharacter _owner = null!;
        private string _hitSoundPrefabPath = null!;

        private Vector2 _attackPosition;
        private Vector2 _attackArea;
        private float _damage;
        private float _knockBackPower;
        private float _hitTimeOnAnimation;
        private float _animationDuration;
        private float _createdAt;
        private float _hitAt;
        private float _hpDrainPercent;

        private bool _isAlive;

        public void AllocateSharedResources(AreaEffectType areaEffectType)
        {
            Debug.Assert(
                areaEffectType == AreaEffectType.TentiSweepVertical_Default ||
                areaEffectType == AreaEffectType.TentiSweepVertical_Hina ||
                areaEffectType == AreaEffectType.TentiSweepVertical_Bongjun);
            base.AllocateSharedResourcesForBase(areaEffectType);

            var attackEffectBodyPath = areaEffectType switch
            {
                AreaEffectType.TentiSweepVertical_Default => "Stages/Characters/TentiAttackEffects/TentiVerticalAttack.prefab",
                AreaEffectType.TentiSweepVertical_Hina => "Stages/Characters/TentiAttackEffects/HinaVerticalAttack.prefab",
                AreaEffectType.TentiSweepVertical_Bongjun => "Stages/Characters/TentiAttackEffects/BongjunVerticalAttack.prefab",
                _ => throw new NotSupportedException($"AreaEffectType {areaEffectType}이 텐티 스윕이 아닙니다."),
            };
            _attackEffectBody = ResourcePool.Instance.InstantiateFromResource<SkeletonAnimation>(attackEffectBodyPath);
            _attackEffectBody.Skeleton.SetToSetupPose();
            _attackEffectBody.transform.SetParent(transform);
            _attackEffectBody.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            _attackEffectBody.transform.localScale = Vector3.one;
            _attackEffectBody.gameObject.SetActive(false);

            var animation = _attackEffectBody.Skeleton.Data.FindAnimation("animation");
            var hitEventData = _attackEffectBody.Skeleton.Data.FindEvent("hit");
            var hitEvent = CharacterAnimationController.FindEventInAnimationTimeline(animation, hitEventData);
            _hitTimeOnAnimation = hitEvent.Time;
            _animationDuration = animation.Duration;

            _hittedMonsters = new HashSet<Character>();
        }

        public void Initialize(
            PlayerCharacter owner,
            Vector2 attackPosition,
            float damage,
            float areaRatio,
            float knockBackPower,
            float hpDrainPercent,
            string hitSoundPrefabPath)
        {
            base.InitializeAreaObject(owner.Alliance);

            _owner = owner;
            _attackPosition = attackPosition;
            _attackArea = areaRatio * DEFAULT_ATTACK_AREA;
            _damage = damage;
            _knockBackPower = knockBackPower;
            _hpDrainPercent = hpDrainPercent;
            _hitSoundPrefabPath = hitSoundPrefabPath;

            float now = Time.time;
            _createdAt = now;
            _hitAt = now + _hitTimeOnAnimation;

            transform.position = attackPosition;

            _attackEffectBody.gameObject.SetActive(true);
            _attackEffectBody.transform.localScale = areaRatio * Vector3.one;
            _attackEffectBody.AnimationState.SetAnimation(0, "animation", loop: false);

            _isAlive = true;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;

            if (_hitAt <= now)
            {
                this.AttackToTargetArea(stage);
                _hitAt = float.MaxValue;
            }

            if (now >= _createdAt + _animationDuration)
            {
                _isAlive = false;
            }
        }

        private void AttackToTargetArea(Stage stage)
        {
            var center = _attackPosition + 0.5f * _attackArea.y * Vector2.up;
            var targetArea = new SquareTargetArea(center, _attackArea, 0.0f);

            _hittedMonsters.Clear();
            CombatSystem.HitOnTargetArea(
                stage, targetArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, _owner.Pos, _knockBackPower, _hittedMonsters, null, _hitSoundPrefabPath);
            if (_hpDrainPercent > 0.0f)
            {
                _owner.DrainHP(stage, _owner.MaxHP * _hpDrainPercent * _hittedMonsters.Count);
            }
        }
    }
}
