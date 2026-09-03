using System;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.Characters.StatusEffects;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;
using SamMul.UnityHelpers;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class IgnitionWaveAreaEffectObject : AreaEffectObjectBase
    {
        private Vector2 _firingDirection;
        private Character _owner;

        private bool _isAlive;
        public override bool IsAlive => _isAlive;

        private float _damage;
        private float _attackDistance;
        private float _attackAngle;
        private float _burnDamage;
        private float _burnDuration;
        private int _burnSpreadAmount;
        private float _knockbackPower;

        // 파동 부채꼴 꼭지점(시작점)이 PC보다 살짝 앞에 그려지게 하는 오프셋
        private Vector2 _areaOffsetFromOwner;
        private const float OFFSET_DISTANCE = 1.0f;

        private SpriteAnimationHandler _noraml60degreeAnimation;
        private SpriteAnimationHandler _noraml90degreeAnimation;
        private SpriteAnimationHandler _selectedAniamtion;

        private HashSet<Character> _hittedCharacters;

        private bool _isFired;

        private string _hitSoundPrefabPath;

        public void AllocateSharedResources(AreaEffectType areaEffectType)
        {
            Debug.Assert(
                areaEffectType == AreaEffectType.IgnitionWave_Default || 
                areaEffectType == AreaEffectType.IgnitionWave_Siyeon ||
                areaEffectType == AreaEffectType.IgnitionWave_Bongjun);
            base.AllocateSharedResourcesForBase(areaEffectType);

            var normal60degreeAnimationPath = areaEffectType switch
            {
                AreaEffectType.IgnitionWave_Default => "Stages/AreaEffects/IgnitionWaves/IgnitionWave.prefab",
                AreaEffectType.IgnitionWave_Siyeon => "Stages/AreaEffects/IgnitionWaves/SiyeonBasicAttack.prefab",
                AreaEffectType.IgnitionWave_Bongjun => "Stages/AreaEffects/IgnitionWaves/GentleBongjun_basic.prefab",
                _ => throw new NotSupportedException($"AreaEffectType {areaEffectType}이 이그니션 웨이브가 아닙니다."),
            };
            _noraml60degreeAnimation = ResourcePool.Instance.InstantiateFromResource<SpriteAnimationHandler>(normal60degreeAnimationPath);
            _noraml60degreeAnimation.InitializeAndPlay(Hit, () =>
            {
                _isAlive = false;
            });
            _noraml60degreeAnimation.transform.SetParent(this.transform);
            _noraml60degreeAnimation.gameObject.SetActive(false);

            var normal90degreeAnimationPath = areaEffectType switch
            {
                AreaEffectType.IgnitionWave_Default => "Stages/AreaEffects/IgnitionWaves/IgnitionWave90.prefab",
                AreaEffectType.IgnitionWave_Siyeon => "Stages/AreaEffects/IgnitionWaves/SiyeonBasicAttackWide.prefab",
                AreaEffectType.IgnitionWave_Bongjun => "Stages/AreaEffects/IgnitionWaves/GentleBongjun_basic90.prefab",
                _ => throw new NotSupportedException($"AreaEffectType {areaEffectType}이 이그니션 웨이브가 아닙니다."),
            };
            _noraml90degreeAnimation = ResourcePool.Instance.InstantiateFromResource<SpriteAnimationHandler>(normal90degreeAnimationPath);
            _noraml90degreeAnimation.InitializeAndPlay(Hit, () =>
            {
                _isAlive = false;
            });

            _noraml90degreeAnimation.transform.SetParent(this.transform);
            _noraml90degreeAnimation.gameObject.SetActive(false);

            _hittedCharacters = new HashSet<Character>();

            _isAlive = false;

        }

        public void Initialize(
            Character owner, 
            Vector2 firingDirection, 
            float damage, 
            float attackDistance, 
            float attackAngle, 
            float knockbackPower, 
            float burnDamage, 
            float burnDuration, 
            int burnSpreadAmount,
            string hitSoundPrefabPath)
        {
            _firingDirection = firingDirection;
            _firingDirection.Normalize();
            _owner = owner;
            _damage = damage;
            _attackDistance = attackDistance;
            _attackAngle = attackAngle;
            _burnDamage = burnDamage;
            _burnDuration = burnDuration;
            _burnSpreadAmount = burnSpreadAmount;
            _knockbackPower = knockbackPower;
            _hitSoundPrefabPath = hitSoundPrefabPath;

            this.gameObject.transform.SetParent(owner.gameObject.transform);

            // 0.5유닛만큼 앞에 서 이펙트를 시작한다.
            _areaOffsetFromOwner = _firingDirection * OFFSET_DISTANCE;
            this.transform.localPosition = new Vector2(0f, 0.4f); // 발보다 살짝 올려서 친다.

            _selectedAniamtion = 60.0f < attackAngle ? _noraml90degreeAnimation : _noraml60degreeAnimation;
            _selectedAniamtion.gameObject.SetActive(true);
            _selectedAniamtion.transform.localPosition = Vector2.zero + (_firingDirection * (attackDistance * 0.5f + 0.5f)) + _areaOffsetFromOwner;
            _selectedAniamtion.transform.right = firingDirection;

            float scale = 1f;
            if (_noraml60degreeAnimation == _selectedAniamtion)
            {
                scale = attackDistance * (1f / 3.8f);
            }
            else if (_noraml90degreeAnimation == _selectedAniamtion)
            {
                scale = attackDistance * (1f / 3.8f);
            }

            _selectedAniamtion.transform.localScale = new Vector2(scale, scale);

            _isAlive = true;
            _isFired = false;
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _noraml60degreeAnimation.gameObject.SetActive(false);
            _noraml90degreeAnimation.gameObject.SetActive(false);

            this.gameObject.transform.SetParent(null);
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
        }

        public void Hit()
        {
            if (_isFired)
            {
                return;
            }

            _isFired = true;
            this.gameObject.transform.SetParent(null, worldPositionStays: true);

            // TODO : 이렇게 전역접근하는 코드 남기면 안됨. 처음부터 구조 설계를 잘 할 것.
            // HitFrame타격을 구현하려면, 히트프레임 시간 계산해서 UpdateLogic 오버라이드해서 파라미터로 전달된 Stage를 사용한다.
            var stage = GameClient.Stage;

            //이펙트는 플레이어 보다 조금 앞에서 나오지만 타격영역은 중점부터 계산한다.
            //플레이어 주변으로 OFFSET_DISTANCE + 0.65f 사이즈의 원 영역만큼도 타격한다.

            _hittedCharacters.Clear();
            Vector2 center = this.transform.position;
            CircularSectorTargetArea attackArea = new CircularSectorTargetArea(
                center,
                _firingDirection,
                _attackDistance + OFFSET_DISTANCE + 0.7f,
                _attackAngle);

            CombatSystem.HitOnTargetArea(
                stage,
                attackArea,
                _owner,
                _damage,
                CombatSystem.KnockBackType.Pivot, center,
                _knockbackPower,
                _hittedCharacters,
                null,
                _hitSoundPrefabPath);


            var centerArea = new CircularTargetArea(center, OFFSET_DISTANCE + 0.65f);
            CombatSystem.HitOnTargetArea(
                stage,
                centerArea,
                _owner,
                _damage,
                CombatSystem.KnockBackType.Pivot,
                center,
                _knockbackPower,
                _hittedCharacters,
                _hittedCharacters,
                _hitSoundPrefabPath
                );

            foreach (Character character in _hittedCharacters)
            {
                if(_burnSpreadAmount > 0)
                {
                    character.StatusEffects.AddOrUpdateStatusEffect(stage, character, StatusEffectType.SpreadBurn, updateThresholdTime: 0.66f, _burnDuration, Time.time, _burnDamage, _burnSpreadAmount);
                }
                else
                {
                    character.StatusEffects.AddOrUpdateStatusEffect(stage, character, StatusEffectType.Burn, updateThresholdTime: 0.66f, _burnDuration, Time.time, _burnDamage);
                }
            }
        }
    }
}