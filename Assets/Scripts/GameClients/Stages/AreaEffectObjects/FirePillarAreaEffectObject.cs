using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.Characters.StatusEffects;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class FirePillarAreaEffectObject : SmartAreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + _lifeTime;

        private Character _owner;
        private float _damage;
        private float _burnDamage;
        private float _burnDuration;
        private float _radius;

        private float _createdAt;
        private float _indicatorAt;
        private float _hitAt;
        private float _lifeTime;
        private float _delay;
        private float _indicatorDuration;
        private bool _isAnimationPlay;

        private GameObject _body;
        private SpriteAnimationHandler _bodyAnimationHandler;

        private HashSet<Character> _hittedCharacters;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForSmartBase(AreaEffectType.FirePillar);
            _body = ResourcePool.Instance.InstantiateFromResource("Stages/AreaEffects/fire_attack.prefab");
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;
            _body.transform.localScale = Vector2.one;
            _bodyAnimationHandler = _body.GetComponent<SpriteAnimationHandler>();
            _bodyAnimationHandler.InitializeOnly();

            _hittedCharacters = new HashSet<Character>();
        }

        public void Initialize(
            Character owner,
            Vector2 position,
            float delay,
            float indicatorDuration,
            float radius,
            float damage,
            float burnDamage,
            float burnDuration
            )
        {
            base.InitializeAreaObject(owner.Alliance);
            float now = Time.time;
            _createdAt = now;
            _owner = owner;
            _delay = delay;
            _indicatorDuration = indicatorDuration;
            _radius = radius;
            _damage = damage;
            _burnDamage = burnDamage;
            _burnDuration = burnDuration;

            _body.gameObject.SetActive(false);
            _body.transform.localScale = Vector3.one * _radius / 1.5f;
            _lifeTime = _delay + _indicatorDuration + _bodyAnimationHandler.AnimationDuration;
            _hitAt = now + _indicatorDuration + _delay;
            _isAnimationPlay = false;
            _indicatorAt = now + delay;
            this.transform.position = position;

        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            base.UpdateLogic(stage, deltaTime);

            float now = Time.time;
            if(_indicatorAt <= now)
            {
                stage.AreaIndicators.CreateBlinkCircularAttackRangeIndicator(this.transform.position, _radius, _indicatorDuration);
                _indicatorAt = float.MaxValue;
            }

            if(_hitAt <= now)
            {
                if(!_isAnimationPlay)
                {
                    _body.gameObject.SetActive(true);
                    _bodyAnimationHandler.InitializeAndPlay();
                    _isAnimationPlay = true;
                }

                HashSet<Character> currentHitted = new HashSet<Character>();
                CircularTargetArea targetArea = new CircularTargetArea(this.transform.position, _radius);
                CombatSystem.HitOnTargetArea(stage, targetArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, Vector2.zero, knockBackPower: 0f, currentHitted, _hittedCharacters, null);

                foreach (Character character in currentHitted)
                {
                    character.StatusEffects.AddOrUpdateStatusEffect(stage, character, StatusEffectType.Burn, _burnDuration, Time.time, _burnDamage);
                    _hittedCharacters.Add(character);
                }
            }
        }

        public override void PuttingBackToPool()
        {
            _body.transform.localScale = Vector3.one;
            _body.gameObject.SetActive(false);
            _hittedCharacters.Clear();
            base.PuttingBackToPool();
        }
    }
}
