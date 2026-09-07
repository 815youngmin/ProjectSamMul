using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.Characters.PCs;
using SamMul.GameClients.Stages.Characters.StatusEffects;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    /// <summary>
    /// 텐티 스윕 초월 공격. 전용 스파인 리소스 없이 판정 시점에 방향별 사각 범위를 AttackAreaFlashManager 로 표시한다.
    /// </summary>
    public class TentiSweepTranscandentObject : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + ANIMATION_DURATION;

        // 원본 공격 애니메이션 길이와 히트 프레임 시점. 타이밍 값으로만 쓴다.
        private const float ANIMATION_DURATION = 0.5f;
        private const float HIT_TIME_ON_ANIMATION = 0.25f;

        private PlayerCharacter _owner;

        private Vector2 _imageDirection;
        private Vector2 _attackSize;
        private float _damage;
        private float _areaRatio;
        private float _knockBackPower;
        private float _normalMonsterStunDuration;
        private float _createdAt;

        public static readonly float BaseAttackAreaWidth = 7f;
        public static readonly float BaseAttackAreaHeight = 1.4f;

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

            _isHitFired = false;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;

            if (_isHitFired)
            {
                return;
            }

            if (now > _createdAt + HIT_TIME_ON_ANIMATION)
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
    }

}
