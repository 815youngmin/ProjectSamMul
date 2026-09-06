using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.Characters.StatusEffects;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.GameClients.Stages.ItemObjects;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    /// <summary>
    /// 이그니션 웨이브 초월 공격. 전용 이펙트 리소스 없이, 진행 방향으로 이동하는 사각 판정 범위를 AttackAreaFlashManager 로 표시한다.
    /// </summary>
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
                // 사거리를 다 나아가면 바로 사라진다.
                _isAlive = false;
                return;
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
            stage.AttackAreaFlashes.Show(areaRect);
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
