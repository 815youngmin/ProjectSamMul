using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.UnityHelpers;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class ExplosionAreaEffect : AreaEffectObjectBase
    {
        public override bool IsAlive => !_isExplosion;

        private Character _owner;
        private float _damage;

        private float _explosionAt;
        private float _attackRadius;
        private Vector2 _position;
        private string _explosionParticlePath;
        private float _explosionParticleScale;

        private bool _isExplosion;

        private float _delay;
        private float _indicatorAt;
        private float _indicatorDuration;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.ExplosionAreaEffect);
        }

        public void Initialize(
            Character owner,
            float delay,
            float indicatorDuration,
            float damage,
            float attackRadius,
            Vector2 position,
            string explosionParticlePath,
            float explosionParticleScale
            )
        {
            base.InitializeAreaObject(owner.Alliance);
            float now = Time.time;
            _owner = owner;
            _delay = delay;
            _indicatorDuration = indicatorDuration;

            _damage = damage;
            _attackRadius = attackRadius;
            _position = position;
            _explosionParticlePath = explosionParticlePath;
            _explosionParticleScale = explosionParticleScale;

            _explosionAt = now + indicatorDuration + _delay;
            _indicatorAt = now + _delay;
            _isExplosion = false;

        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;

            if(_indicatorAt <= now)
            {
                stage.AreaIndicators.CreateBlinkCircularAttackRangeIndicator(_position, _attackRadius, _indicatorDuration);
                _indicatorAt = float.MaxValue;
            }

            if(_explosionAt <= now)
            {
                _isExplosion = true;
                CombatSystem.HitOnTargetArea(stage, new CircularTargetArea(_position, _attackRadius), _owner, _damage, CombatSystem.KnockBackType.Pivot, Vector2.zero, 0f, null, null, null);
                UnityGlobal.SpriteAnimations.CreateAndPlaySpriteAnimation(_explosionParticlePath, _position, Vector2.one * _explosionParticleScale, null);
            }
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
        }

    }

}
