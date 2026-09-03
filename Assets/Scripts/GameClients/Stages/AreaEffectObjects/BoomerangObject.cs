using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class BoomerangObject : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time < _endTimeAt;

        private Character _owner;
        private GameObject _bodyImage;
        private float _damage;
        private Vector2 _startPosition;
        private Vector2 _endPosition;
        private float _duration;
        private float _startTimeAt;
        private float _tuneTimeAt;
        private float _endTimeAt;
        private Vector2 _startToEndDirect;
        private float _attackRadius;
        private float _rotatingSpeed;

        private HashSet<Character> _hittedCharacter = new HashSet<Character>();
        private float _hittedClearAt;

        public void AllocateSharedResources(AreaEffectType areaEffectType, string bodyPrefabPath)
        {
            base.AllocateSharedResourcesForBase(areaEffectType);
            _bodyImage = ResourcePool.Instance.InstantiateFromResource(bodyPrefabPath);
            _bodyImage.transform.SetParent(this.gameObject.transform);
            _bodyImage.transform.localPosition = Vector3.zero;
            _bodyImage.transform.localRotation = Quaternion.identity;
        }

        public void Initialize(
                Character owenr,
                float duration,
                Vector2 startPosition,
                Vector2 endPosition,
                float damage,
                float attackRadius,
                float rotatingSpeed)
        {
            _owner = owenr;
            _damage = damage;

            this.transform.position = startPosition;
            _startPosition = startPosition;
            _endPosition = endPosition;
            _attackRadius = attackRadius;
            _rotatingSpeed = rotatingSpeed;

            _duration = duration;
            _startTimeAt = Time.time;
            _tuneTimeAt = _startTimeAt + (duration * 0.6f);
            _hittedClearAt = _tuneTimeAt;
            _endTimeAt = _startTimeAt + duration;

            _startToEndDirect = (_endPosition - _startPosition).normalized;

            _hittedCharacter.Clear();
        }
        

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;

            Vector2 newPosition;
            float t = 0;
            if (_tuneTimeAt < now)
            {
                t = Mathf.Clamp((now - _tuneTimeAt) / (_duration * 0.4f), 0.0f, 1.0f);
                newPosition = Vector2.Lerp(_endPosition, _startPosition, easeInCubic(t));
                if (_rotatingSpeed == 0)
                {
                    _bodyImage.transform.right = -_startToEndDirect;
                }
            }
            else
            {
                t = Mathf.Clamp((now - _startTimeAt) / (_duration * 0.6f), 0.0f, 1.0f);
                newPosition = Vector2.Lerp(_startPosition, _endPosition, easeOutCubic(t));
                if(_rotatingSpeed == 0)
                {
                    _bodyImage.transform.right = _startToEndDirect;
                }
            }

            if(_rotatingSpeed != 0)
            {
                this.Rotate(deltaTime);
            }

            if(_hittedClearAt < now)
            {
                _hittedCharacter.Clear();
                _hittedClearAt = float.MaxValue;
            }

            CircularTargetArea attackArea = new CircularTargetArea(this.transform.position, _attackRadius);
            CombatSystem.HitOnTargetArea(stage, attackArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, attackArea.Center, 0.0f, _hittedCharacter, _hittedCharacter, string.Empty);

            this.transform.position = newPosition;
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _hittedCharacter.Clear();
        }

        private float easeOutCubic(float x)
        {
            return 1.0f - Mathf.Pow(1.0f - x, 3);
        }

        private float easeInCubic(float x)
        {
            return x * x * x;
        }

        private void Rotate(float deltaTime)
        {
            _bodyImage.transform.Rotate(0.0f, 0.0f, deltaTime * _rotatingSpeed);
        }
    }
}
