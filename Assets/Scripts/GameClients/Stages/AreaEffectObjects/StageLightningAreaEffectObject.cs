using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;
using SamMul.UnityHelpers;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class StageLightningAreaEffectObject : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + _lifeTime;

        private Stage _stage;
        private Character _owner;
        private float _damage;

        private float _createdAt;
        private float _lifeTime;

        private GameObject _body;
        private readonly string _zeusLightningPath = "Stages/AreaEffects/ZeusLightning/Zeus_Lightning.prefab";
        private SpriteAnimationHandler _animationHandler;

        AllianceType _allianceType;
        private float _attackRadius;
        private float _indicatorDuration;
        private float _stunDuration;

        private float _animationPlayAt;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.StageLightningAreaEffectObject);
            _body = ResourcePool.Instance.InstantiateFromResource(_zeusLightningPath);
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;
            _body.transform.localScale = Vector2.one;
            _animationHandler = _body.GetComponent<SpriteAnimationHandler>();
            _animationHandler.InitializeOnly();
        }

        public void Initialize(
            Stage stage,
            AllianceType allianceType,
            float indicatorDuration,
            Vector3 attackPosition,
            float attackRadius,
            float damage,
            float stunDuration
            )
        {
            base.InitializeAreaObject(allianceType);

            _stage = stage;
            _allianceType = allianceType;
            this.transform.position = attackPosition;
            _attackRadius = attackRadius;
            _damage = damage;
            _indicatorDuration = indicatorDuration;
            _stunDuration = stunDuration;

            float now = Time.time;
            _createdAt = now;
            _lifeTime = _animationHandler.AnimationDuration + _indicatorDuration;

            _body.gameObject.SetActive(false);
            _body.transform.localScale = Vector3.one * (_attackRadius / 1.5f);
            _stage.AreaIndicators.CreateBlinkCircularAttackRangeIndicator(this.transform.position, _attackRadius, _indicatorDuration);
            _animationPlayAt = now + _indicatorDuration;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;
            if(now > _animationPlayAt)
            {
                _body.gameObject.SetActive(true);
                _animationHandler.InitializeAndPlay(Hit);
                _animationPlayAt = float.MaxValue;
            }
        }

        public override void PuttingBackToPool()
        {
            _body.transform.localScale = Vector3.one;
            _body.gameObject.SetActive(false);
            base.PuttingBackToPool();
        }

        private void Hit()
        {
            CircularTargetArea targetArea = new CircularTargetArea(this.transform.position, _attackRadius);
            List<Character> findedCharacters = new List<Character>();
            _stage.FindAliveCharactersInArea(_allianceType.ToEnemyAlliance(), targetArea, findedCharacters);

            foreach (Character character in findedCharacters)
            {
                character.Hitted(_stage, null, _damage, Vector2.zero, targetArea.Center, null);
                character.StatusEffects.AddOrUpdateStatusEffect(_stage, character, Characters.StatusEffects.StatusEffectType.Stun, _stunDuration, Time.time, 0f);
            }

        }
    }
}
