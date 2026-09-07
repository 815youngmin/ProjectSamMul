#nullable enable
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.Characters.PCs;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    /// <summary>
    /// 텐티 스윕 수직 공격. 전용 스파인 리소스 없이 판정 시점에 사각 범위를 AttackAreaFlashManager 로 표시한다.
    /// </summary>
    public class TentiSweepVerticalObject : AreaEffectObjectBase
    {
        private readonly Vector2 DEFAULT_ATTACK_AREA = new Vector2(1.0f, 3.5f);

        // 원본 공격 애니메이션 길이와 히트 프레임 시점. 타이밍 값으로만 쓴다.
        private const float ANIMATION_DURATION = 0.5f;
        private const float HIT_TIME_ON_ANIMATION = 0.25f;

        public override bool IsAlive => _isAlive;

        private HashSet<Character> _hittedMonsters = null!;

        private PlayerCharacter _owner = null!;
        private string _hitSoundPrefabPath = null!;

        private Vector2 _attackPosition;
        private Vector2 _attackArea;
        private float _damage;
        private float _knockBackPower;
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
            _hitAt = now + HIT_TIME_ON_ANIMATION;

            transform.position = attackPosition;

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

            if (now >= _createdAt + ANIMATION_DURATION)
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
