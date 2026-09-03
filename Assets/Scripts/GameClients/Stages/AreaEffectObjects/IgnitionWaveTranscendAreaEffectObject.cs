using System;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.Characters.StatusEffects;
using Z.GameClients.Stages.CombatSystems;
using Z.GameClients.Stages.ItemObjects;
using Z.ResourcePools;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class IgnitionWaveTranscendAreaEffectObject : AreaEffectObjectBase
    {
        public override bool IsAlive => _isAlive;
        private bool _isAlive;

        private Character _owner;

        // 진행 속도 벡터 m / sec
        private Vector2 _moveVector;

        private Vector2 _firingDirection;
        // 얼만큼 더 날아갈 수 있는지, 남은 거리. 이만큼 사라지면 효과도 제거된다.
        private float _leftAliveDistance;
        private Vector2 _attackSize;
        private float _damage;
        private float _knockBackPower;
        private float _burnDamage;
        private float _burnDuration;
        private int _burnSpreadAmount;
        private float _stunDuration;

        private SpriteAnimationHandler _repeatAnimation;
        private SpriteAnimationHandler _endAnimation;
        private bool _isDisappearing;

        private HashSet<Character> _hittedCharacters;
        // hittedCharacter 마다 피격되었던 시각을 기록
        private Dictionary<Character, float> _hittedAtByCharacter;
        // hittedAt을 주기적으로 확인하고, 오래된 것 지워준다.
        private float _hittedAtCheckTimeSlice;
        // hittedAt에 의해 지워줄 캐릭터들
        private List<Character> _removeCandidates;

        private List<Character> _hitTargetsOnThisFrame;
        private const float HITTED_AT_CHECK_PERIOD = 0.03f;

        private string _hitSoundPrefabPath;


        public void AllocateSharedResources(AreaEffectType areaEffectType)
        {
            Debug.Assert(
                areaEffectType == AreaEffectType.IgnitionWaveTranscend_Default || 
                areaEffectType == AreaEffectType.IgnitionWaveTranscend_Siyeon ||
                areaEffectType == AreaEffectType.IgnitionWaveTranscend_Bongjun);
            base.AllocateSharedResourcesForBase(areaEffectType);

            var repeatAnimationPath = areaEffectType switch
            {
                AreaEffectType.IgnitionWaveTranscend_Default => "Stages/AreaEffects/IgnitionWaves/IgnitionWaveS_Repeat.prefab",
                AreaEffectType.IgnitionWaveTranscend_Siyeon => "Stages/AreaEffects/IgnitionWaves/SiyeonTranscendentAttackRepeat.prefab",
                AreaEffectType.IgnitionWaveTranscend_Bongjun => "Stages/AreaEffects/IgnitionWaves/GentleBongjunSS_Repeat.prefab",
                _ => throw new NotSupportedException($"AreaEffectType {areaEffectType}이 이그니션 웨이브가 아닙니다."),
            };
            _repeatAnimation = ResourcePool.Instance.InstantiateFromResource<SpriteAnimationHandler>(repeatAnimationPath);

            var endAnimationPath = areaEffectType switch
            {
                AreaEffectType.IgnitionWaveTranscend_Default => "Stages/AreaEffects/IgnitionWaves/IgnitionWaveS_End.prefab",
                AreaEffectType.IgnitionWaveTranscend_Siyeon => "Stages/AreaEffects/IgnitionWaves/SiyeonTranscendentAttackEnd.prefab",
                AreaEffectType.IgnitionWaveTranscend_Bongjun => "Stages/AreaEffects/IgnitionWaves/GentleBongjunSS_End.prefab",
                _ => throw new NotSupportedException($"AreaEffectType {areaEffectType}이 이그니션 웨이브가 아닙니다."),
            };
            _endAnimation = ResourcePool.Instance.InstantiateFromResource<SpriteAnimationHandler>(endAnimationPath);

            _repeatAnimation.InitializeOnly();
            _endAnimation.InitializeOnly();

            _repeatAnimation.transform.SetParent(this.transform);
            _endAnimation.transform.SetParent(this.transform);

            _repeatAnimation.transform.localPosition = Vector2.zero;
            _endAnimation.transform.localPosition = Vector2.zero;

            _hittedCharacters = new HashSet<Character>();
            _hittedAtByCharacter = new Dictionary<Character, float>();
            _removeCandidates = new List<Character>();
            _hitTargetsOnThisFrame = new List<Character>();
        }

        public void Initialize(
            Character owner,
            Vector2 firingDirection,
            float moveSpeed,
            float attackWidth,
            float attackRange,
            float damage,
            float knockBackPower,
            float burnDamage,
            float burnDuration,
            int burnSpreadAmount,
            float stunDuration,
            string hitSoundPrefabPath)
        {
            base.InitializeAreaObject(owner.Alliance);

            _isAlive = true;

            _leftAliveDistance = attackRange;

            _damage = damage;
            _knockBackPower = knockBackPower;
            _owner = owner;
            _firingDirection = firingDirection;
            _firingDirection.Normalize();

            // 진행방향으로 10 unit per seconds;
            _moveVector = _firingDirection * moveSpeed;
            _repeatAnimation.transform.right = _endAnimation.transform.right = _firingDirection;
            float bodyScale = attackWidth * (1 / 2.8f);
            _repeatAnimation.transform.localScale = _endAnimation.transform.localScale = new Vector2(bodyScale, bodyScale);

            _repeatAnimation.gameObject.SetActive(true);
            _endAnimation.gameObject.SetActive(false);
            _repeatAnimation.InitializeAndPlay();
            _isDisappearing = false;

            _burnDamage = burnDamage;
            _burnDuration = burnDuration;
            _burnSpreadAmount = burnSpreadAmount;
            _stunDuration = stunDuration;

            this.transform.position = _owner.transform.position;

            _attackSize = new Vector2(attackWidth, 3.5f);

            this.gameObject.SetActive(true);

            _hittedCharacters.Clear();
            _hitTargetsOnThisFrame.Clear();
            _hittedAtByCharacter.Clear();
            _hittedAtCheckTimeSlice = HITTED_AT_CHECK_PERIOD;
            _removeCandidates.Clear();

            _hitSoundPrefabPath = hitSoundPrefabPath;
        }

        private SquareTargetArea CurrentTargetArea()
        {
            Vector2 pos = this.transform.position;
            var areaRect = new SquareTargetArea(pos, _attackSize, Vector2.SignedAngle(Vector2.up, _firingDirection));
            return areaRect;
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _owner = null;

            _repeatAnimation.gameObject.SetActive(false);
            _endAnimation.gameObject.SetActive(false);

            this.gameObject.SetActive(false);
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            if (!IsAlive)
            {
                return;
            }

            if (_leftAliveDistance > 0f)
            {
                var deltaPosition = _moveVector * deltaTime;
                var previousPosition = this.transform.position;
                this.transform.position = new Vector3(
                    previousPosition.x + deltaPosition.x,
                    previousPosition.y + deltaPosition.y,
                    previousPosition.z);

                var deltaDistance = deltaPosition.magnitude;
                _leftAliveDistance -= deltaDistance;
                if (_leftAliveDistance < 0f)
                {
                    _leftAliveDistance = 0f;
                }
            }
            else
            {
                if (!_isDisappearing)
                {
                    _isDisappearing = true;
                    _repeatAnimation.gameObject.SetActive(false);
                    _endAnimation.gameObject.SetActive(true);
                    _endAnimation.InitializeAndPlay(null, endEventHandler: () =>
                    {
                        _isAlive = false;
                    });
                }
            }

            float now = Time.time;

            _hittedAtCheckTimeSlice -= deltaTime;
            if (_hittedAtCheckTimeSlice <= 0)
            {
                _hittedAtCheckTimeSlice = HITTED_AT_CHECK_PERIOD;

                foreach (var kvp in _hittedAtByCharacter)
                {
                    var character = kvp.Key;
                    float hittedAt = kvp.Value;
                    if (hittedAt + 0.22f < now)
                    {
                        _removeCandidates.Add(character);
                    }
                }

                foreach (var character in _removeCandidates)
                {
                    _hittedCharacters.Remove(character);
                    _hittedAtByCharacter.Remove(character);
                }
                _removeCandidates.Clear();
            }
            var areaRect = CurrentTargetArea();
            _hitTargetsOnThisFrame.Clear();

            stage.FindCharactersInArea(
                _owner.Alliance.ToEnemyAlliance(),
                areaRect,
                condition: (candidate) => !candidate.Action.IsDead && !_hittedCharacters.Contains(candidate),
                result: in _hitTargetsOnThisFrame);

            foreach (var target in _hitTargetsOnThisFrame)
            {
                var hitVector = (target.Pos - _owner.Pos).normalized * _knockBackPower;

                target.Hitted(stage, attacker: _owner, _damage, hitVector, (Vector2)this.transform.position, _hitSoundPrefabPath);
                if(_burnSpreadAmount > 0)
                {
                    target.StatusEffects.AddOrUpdateStatusEffect(stage, target, StatusEffectType.SpreadBurn, updateThresholdTime: 0.66f, duration: _burnDuration, Time.time, _burnDamage, _burnSpreadAmount);
                }
                else
                {
                    target.StatusEffects.AddOrUpdateStatusEffect(stage, target, StatusEffectType.Burn, updateThresholdTime: 0.66f, duration: _burnDuration, Time.time, _burnDamage);
                }

                if (_stunDuration > 0.0f && !target.IsBoss)
                {
                    target.StatusEffects.AddOrUpdateStatusEffect(stage, target, StatusEffectType.Stun, _stunDuration, Time.time, 0f);
                }
                _hittedCharacters.Add(target);
                _hittedAtByCharacter.Add(target, now);
            }
            _hitTargetsOnThisFrame.Clear();

            IReadOnlyList<BreakableItemObject> breakableItemObjects = stage.BreakableItemObjects;
            foreach (var breakableItemObject in breakableItemObjects)
            {
                if (areaRect.Contains(breakableItemObject.transform.position, BreakableItemObject.ITEM_COLLIDER_RADIUS))
                {
                    breakableItemObject.OnBroken(_owner, _damage, stage);
                }
            }
        }
    }
}
