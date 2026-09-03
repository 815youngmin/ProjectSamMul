using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.UnityHelpers;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class SpaceShipIceGroupAreaEffect : AreaEffectObjectBase
	{
		public override bool IsAlive =>  Time.time <= _createdAt + _lifeTime;

        private Character _owner;
        private float _damage;

        private float _createdAt;
        private float _lifeTime;

        private int _dropCount;
        private float _dropDuration;
        private Vector2 _dropPositionCenter;
        private float _dropRadius;
        private float _attackRadius;
        private float _slowAreaEffectDuration;
        private float _slowDuration;
        private float _slowRatio;

        private List<float> _attackAts;
        private int _currentAttackCount;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.SpaceShipIceGroup);
        }

        public void Initialize(
            Character owner,
            int dropCount,
            float dropDuration,
            Vector2 dropPositionCenter,
            float dropRadius,
            float attackRadius,
            float slowAreaEffectDuration,
            float slowDuration,
            float slowRatio,
            float damage 
            )
        {
            base.InitializeAreaObject(owner.Alliance);
            float now = Time.time;
            _createdAt = now;
            _owner = owner;
            _lifeTime = dropDuration;

            _dropCount = dropCount;
            _dropDuration = dropDuration;
            _dropPositionCenter = dropPositionCenter;
            _dropRadius = dropRadius;
            _attackRadius = attackRadius;
            _slowAreaEffectDuration = slowAreaEffectDuration;
            _slowDuration = slowDuration;
            _slowRatio = slowRatio;
            _damage = damage;

            _attackAts = new List<float>();
            float attackPeriod = dropDuration / dropCount;
            for (int i = 0; i < dropCount; i++)
			{
				float attackAt = now + attackPeriod * i;
				_attackAts.Add(attackAt);
			}
			_currentAttackCount = 0;

			UnityGlobal.Sounds.PlayBySoundPrefab("Sounds/SoundEffects/PCs/SpaceShipAttack_SFX.prefab", dropPositionCenter);
		}

		public override void UpdateLogic(Stage stage, float deltaTime)
		{
			float now = Time.time;
			for (int i = _currentAttackCount; i < _attackAts.Count; i++)
            {
                if (_attackAts[i] <= now)
                {
                    _currentAttackCount++;
                    Vector2 attackPos = _dropPositionCenter + Random.insideUnitCircle.normalized * Random.Range(0f, _dropRadius);
                    stage.CreateSpaceShipIceObject(_owner, attackPos, _attackRadius, _damage, _slowAreaEffectDuration, _slowDuration, _slowRatio);
                }
            }
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
        }
    }
}
