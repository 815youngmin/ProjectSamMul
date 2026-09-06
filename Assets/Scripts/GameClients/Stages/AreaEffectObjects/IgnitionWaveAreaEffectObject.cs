using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.Characters.StatusEffects;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    /// <summary>
    /// 이그니션 웨이브 기본 공격. 전용 이펙트 리소스 없이 생성된 프레임에 판정하고 사라진다.
    /// 판정 범위는 CombatSystem.HitOnTargetArea 를 거치며 AttackAreaFlashManager 가 표시한다.
    /// </summary>
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

        // 파동 부채꼴 꼭지점(시작점)이 PC보다 살짝 앞에서 시작하는 오프셋
        private const float OFFSET_DISTANCE = 1.0f;

        private HashSet<Character> _hittedCharacters;

        private string _hitSoundPrefabPath;

        public void AllocateSharedResources(AreaEffectType areaEffectType)
        {
            Debug.Assert(
                areaEffectType == AreaEffectType.IgnitionWave_Default ||
                areaEffectType == AreaEffectType.IgnitionWave_Siyeon ||
                areaEffectType == AreaEffectType.IgnitionWave_Bongjun);
            base.AllocateSharedResourcesForBase(areaEffectType);

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

            // 발보다 살짝 위를 판정 중심으로 잡는다.
            this.transform.position = owner.transform.position + new Vector3(0f, 0.4f, 0f);

            _isAlive = true;
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            if (!_isAlive)
            {
                return;
            }

            this.Hit(stage);
            _isAlive = false;
        }

        private void Hit(Stage stage)
        {
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
