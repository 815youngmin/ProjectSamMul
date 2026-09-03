using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class OdinGungnirAreaEffectObject : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _endAt;

        private Character _owner;
        private float _damage;

        private GameObject _body;

        private float _moveTargetDuration;
        private float _spinDuration;
        private float _moveBackDuration;

        private Vector2 _spawnPosition;
        private Vector2 _targetPosition;
        private Vector2 _spawnToTargetDirection;
        private Vector2 _targetToSpawnDirection;

        private float _startAt;
        private float _spinAt;
        private float _backAt;
        private float _endAt;

        private HashSet<Character> _hittedCharacters = new HashSet<Character>();
        private float _hittedClearAt;


        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.OdinGungnir);
            _body = ResourcePool.Instance.InstantiateFromResource("Stages/AreaEffects/OdinGungnirAreaEffectObject.prefab");
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;
            _body.transform.localScale = Vector2.one;
        }

        public void Initialize(
            Character owner,
            Vector2 spawnPosition,
            Vector2 targetPosition,
            float moveTargetDuration,
            float spinDuration,
            float moveBackDuration,
            float damage
            )
        {
            base.InitializeAreaObject(owner.Alliance);
            float now = Time.time;

            _owner = owner;
            _damage = damage;
            _moveTargetDuration = moveTargetDuration;
            _spinDuration = spinDuration;
            _moveBackDuration = moveBackDuration;

            _spawnPosition = spawnPosition;
            _targetPosition = targetPosition;

            _startAt = Time.time;
            _spinAt = _startAt + _moveTargetDuration;
            _backAt = _spinAt + _spinDuration;
            _endAt = _backAt + _moveBackDuration;

            _spawnToTargetDirection = _targetPosition - spawnPosition;
            _targetToSpawnDirection = _spawnPosition - _targetPosition;

            _hittedClearAt = Time.time + _moveTargetDuration + _spinDuration * 0.5f;
        }


        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;

            Vector2 newPosition = Vector2.zero;
            float t = 0;
            if (_startAt <= now && now < _spinAt)
            {
                t = Mathf.Clamp((now - _startAt) / _moveTargetDuration, 0.0f, 1.0f);
                newPosition = Vector2.Lerp(_spawnPosition, _targetPosition, easeOutCubic(t));
                _body.transform.right = _spawnToTargetDirection;
            }
            else if (_spinAt <= now && now < _backAt)
            {
                t = Mathf.Clamp((now - _spinAt) / _spinDuration, 0.0f, 1.0f);
                newPosition = _targetPosition;

                 float spinAngle = Mathf.Lerp(0, 360f  + 180f, easeOutCubic(t));
                _body.transform.right = Quaternion.Euler(0,0, spinAngle) * _spawnToTargetDirection;
            }
            else if (_backAt <= now && now < _endAt)
            {
                t = Mathf.Clamp((now - _backAt) / _moveBackDuration, 0.0f, 1.0f);
                newPosition = Vector2.Lerp(_targetPosition, _spawnPosition, easeInCubic(t));
                _body.transform.right = _targetToSpawnDirection;
            }

            if (_hittedClearAt < now)
            {
                _hittedCharacters.Clear();
                _hittedClearAt = float.MaxValue;
            }

            float angle = (Mathf.Atan2(this.transform.right.y, this.transform.right.x) * Mathf.Rad2Deg);
            SquareTargetArea spearTargetArea = new SquareTargetArea(this.transform.position, new Vector2(10f, 0.5f), angle);
            CombatSystem.HitOnTargetArea(stage, spearTargetArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, Vector2.zero, 0f, _hittedCharacters, _hittedCharacters, hitSoundPrefabPath: string.Empty);

            this.transform.position = newPosition;
        }

        public override void PuttingBackToPool()
        {
            _hittedCharacters.Clear();
            base.PuttingBackToPool();
        }
        private float easeOutCubic(float x)
        {
            return 1.0f - Mathf.Pow(1.0f - x, 3);
        }

        private float easeInCubic(float x)
        {
            return x * x * x;
        }
    }

}
