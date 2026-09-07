using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    /// <summary>
    /// 쇼크 봄. 전용 스파인 리소스 없이 몸체는 공용 공격 비주얼로, 공격 범위는 판정 시 AttackAreaFlashManager 로 표시한다.
    /// </summary>
    public class ShockBombAreaEffectObject : AreaEffectObjectBase
    {
        private static readonly Vector2 SHADOW_OFFSET = new Vector2(-0.0361f, -0.5f);
        private static readonly Vector3 SHADOW_MAX_SCALE = 1.2f * Vector3.one;
        private static readonly Vector3 SHADOW_MIN_SCALE = 0.8f * Vector3.one;

        // 원본 도착(변신)·종료 애니메이션 길이. 타이밍 값으로만 쓴다.
        private const float ARRIVE_DURATION = 0.5f;
        private const float END_DURATION = 0.5f;
        private const float BODY_DIAMETER = 0.8f;

        public override bool IsAlive => !_isHit && Time.time <= _createdAt + _lifeTime;

        private Character _owner;
        private float _damage;

        private float _attackRadius;
        private float _knockbackPower;
        private float _duration;
        private float _hitPeriod; // Param 타격하는 주기
        private bool _isTranscend;
        private string _hitSoundPrefabPath;

        private float _createdAt;
        private float _lifeTime;
        private bool _isHit;

        private GameObject _visual;
        private GameObject _shadow;

        private float _arriveAt;
        private float _hitAt;
        private float _hitEndAt;

        private float _tickDamageNextAt;
        private HashSet<Character> _hittedCharacters;


        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.ShockBomb);

            _visual = PlayerAttackVisual.Attach(transform, BODY_DIAMETER);

            _shadow = new GameObject("Shadow");
            _shadow.transform.localPosition = Vector2.zero;
            _shadow.transform.localScale = SHADOW_MAX_SCALE;
            var shadow = _shadow.AddComponent<SpriteRenderer>();
            shadow.sprite = ResourcePool.Instance.LoadResource<Sprite>("Stage/Common/CharacterShadow.png");
            shadow.color = new Color(shadow.color.r, shadow.color.g, shadow.color.b, 1.00f);
            shadow.sortingLayerID = SortingLayer.NameToID("LowShadow");
            shadow.drawMode = SpriteDrawMode.Simple;

            _hittedCharacters = new HashSet<Character>();
        }


        public void Initialize(
            Character owner,
            Vector2 arrivalPosition,
            float arrivalTime,
            float damage,
            float radius,
            float knockbackPower,
            float duration,
            float hitPeriod,
            bool isTranscend,
            string hitSoundPrefabPath
            )
        {
            base.InitializeAreaObject(owner.Alliance);

            _owner = owner;
            _damage = damage;
            _attackRadius = radius;
            _knockbackPower = knockbackPower;
            _duration = duration;
            _hitPeriod = hitPeriod;
            _isTranscend = isTranscend;
            _hitSoundPrefabPath = hitSoundPrefabPath;

            float now = Time.time;
            _createdAt = now;
            _lifeTime = arrivalTime + ARRIVE_DURATION + duration + END_DURATION;

            this.transform.position = _owner.CenterPos;
            this.transform.DOJump(arrivalPosition, jumpPower: 5f, numJumps: 1, duration: arrivalTime).SetEase(Ease.Linear);

            _shadow.SetActive(true);
            _shadow.transform.position = _owner.CenterPos + SHADOW_OFFSET;
            _shadow.transform.DOMove(arrivalPosition + SHADOW_OFFSET, duration: arrivalTime).SetEase(Ease.Linear);
            DOTween.Sequence(_shadow)
                .Append(_shadow.transform.DOScale(SHADOW_MIN_SCALE, arrivalTime / 2.0f))
                .Append(_shadow.transform.DOScale(SHADOW_MAX_SCALE, arrivalTime / 2.0f));

            _visual.SetActive(true);

            _arriveAt = now + arrivalTime;
            _hitAt = now + arrivalTime + ARRIVE_DURATION;
            _hitEndAt = _hitAt + _duration;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;

            //ShockBomb이 목적지에 도착한 시점
            if (_arriveAt <= now)
            {
                _arriveAt = float.MaxValue;
                _tickDamageNextAt = now;
                _shadow.SetActive(false);
            }

            if (_hitAt <= now)
            {
                this.UpdateAttacked(stage, now);
            }


            if (_hitEndAt <= now)
            {
                // 공격이 끝나면 몸체를 숨기고 END_DURATION 뒤에 수명이 끝난다.
                _visual.SetActive(false);

                _hitAt = float.MaxValue;
                _hitEndAt = float.MaxValue;
            }
        }

        private void UpdateAttacked(Stage stage, float now)
        {
            if (_tickDamageNextAt <= now)
            {
                _hittedCharacters.Clear();
                _tickDamageNextAt = Time.time + _hitPeriod;
            }

            CircularTargetArea circularTargetArea = new CircularTargetArea(this.transform.position, _attackRadius);
            CombatSystem.HitOnTargetArea(stage, circularTargetArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, this.transform.position, _knockbackPower, _hittedCharacters, _hittedCharacters, _hitSoundPrefabPath);
        }

        public override void PuttingBackToPool()
        {
            _hittedCharacters.Clear();
            base.PuttingBackToPool();
        }
    }

}
