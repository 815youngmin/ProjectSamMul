using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.Characters.PCs;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class DeployYoyoAreaEffectObject : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + _duration;

        private PlayerCharacter _owner;
        private float _damage;

        private GameObject _body;
        private SpriteRenderer _bodySpriteRenderer;

        private float _createdAt;
        private float _duration;
        private float _attackRadius;
        private float _knockbackPower;

        private static readonly float HITTED_CHARACTER_CLEAR_PERIOD = 0.5f;
        private float _hittedCharacterClearAt;

        private HashSet<Character> _hittedCharacters;
        private Sequence _fadeoutSequence;
        private Color _saveBodyColor;

        public void AllocateSharedResources(AreaEffectType areaEffectType)
        {
            Debug.Assert(areaEffectType == AreaEffectType.DeployBattleYoYo_Default ||
                         areaEffectType == AreaEffectType.DeployBattleYoYo_EggKim);

            base.AllocateSharedResourcesForBase(areaEffectType);

            var bodyPrefabPath = areaEffectType switch
            {
                AreaEffectType.DeployBattleYoYo_Default => "Stages/AreaEffects/Yoyo_w_0.prefab",
                AreaEffectType.DeployBattleYoYo_EggKim => "Stages/AreaEffects/Yoyo_EggKim.prefab",
                _ => throw new NotSupportedException($"AreaEffectType {areaEffectType}이 배틀 요요가 아닙니다."),
            };
            _body = ResourcePool.Instance.InstantiateFromResource(bodyPrefabPath);
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;
            _body.transform.localScale = Vector2.one * 0.5f;
            _bodySpriteRenderer = _body.GetComponent<SpriteRenderer>();


            _hittedCharacters = new HashSet<Character>();
        }

        public void Initialize(
            PlayerCharacter owner,
            Vector2 position,
            Vector2 direction,
            float duration,
            float attackRadius,
            float knockbackPower,
            float damage)
        {
            base.InitializeAreaObject(owner.Alliance);
            _damage = damage;
            _owner = owner;
            this.transform.position = position;
            _duration = duration;
            _knockbackPower = knockbackPower;
            _hittedCharacters.Clear();

            _createdAt = Time.time;

            if (direction.x > 0)
            {
                _body.transform.localScale = new Vector2(1, 1) * 0.5f * (attackRadius / 0.8f); //반지름 0.8f가 기본 사이즈 그보다 크면 이미지 크기를 키워준다

            }
            else
            {
                _body.transform.localScale = new Vector2(-1, 1) * 0.5f * (attackRadius / 0.8f); //반지름 0.8f가 기본 사이즈 그보다 크면 이미지 크기를 키워준다
            }

            if (_bodySpriteRenderer != null)
            {
                _saveBodyColor = _bodySpriteRenderer.color;
                _fadeoutSequence = DOTween.Sequence();
                _fadeoutSequence.Insert(duration - 0.2f, _bodySpriteRenderer.DOFade(0f, 0.2f));
            }
            _attackRadius = attackRadius;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            if (_hittedCharacterClearAt < Time.time)
            {
                _hittedCharacters.Clear();
                _hittedCharacterClearAt = Time.time + HITTED_CHARACTER_CLEAR_PERIOD;
            }


            CircularTargetArea area = new CircularTargetArea(this.transform.position, _attackRadius);
            CombatSystem.HitOnTargetArea(
                stage, area, _owner, _damage, CombatSystem.KnockBackType.Pivot,
                _owner.CenterPos, _knockbackPower, _hittedCharacters, _hittedCharacters,
                null);
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _hittedCharacters.Clear();

            if(_bodySpriteRenderer != null)
            {
                _bodySpriteRenderer.color = _saveBodyColor;
            }

            if (_fadeoutSequence != null)
            {
                _fadeoutSequence.Kill();
            }
        }
    }
}
