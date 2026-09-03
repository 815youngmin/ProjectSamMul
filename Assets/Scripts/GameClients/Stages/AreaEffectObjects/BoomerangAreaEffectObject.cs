using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class BoomerangAreaEffectObject : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time < _endTimeAt;

        private Character _owner;
        private GameObject _bodyImage;
        private float _projectileDamage;
        private float _projectileRadius;
        private Vector2 _startPosition;
        private Vector2 _endPosition;
        private float _duration;
        private float _startTimeAt;
        private float _tuneTimeAt;
        private float _endTimeAt;
        private Vector2 _startToEndDirect;

        private HashSet<Character> _hittedCharacter = new HashSet<Character>();
        private float _hittedClearAt;

        public void AllocateSharedResources(AreaEffectType areaEffectType, string imagePath)
        {
            base.AllocateSharedResourcesForBase(areaEffectType);
            _bodyImage = ResourcePool.Instance.InstantiateFromResource(imagePath);
            _bodyImage.transform.SetParent(this.gameObject.transform);
            _bodyImage.transform.localPosition = Vector3.zero;
            _bodyImage.transform.localScale = Vector3.one;
            _bodyImage.transform.localRotation = Quaternion.identity;
        }

        public void Initialize(
                Character owner,
                float duration,
                Vector2 startPosition,
                Vector2 endPosition,
                float projectileRadius,
                float projectileDamage
                )
        {
            _owner = owner;
            _projectileDamage = projectileDamage;
            _projectileRadius = projectileRadius;

            this.transform.position = startPosition;
            _startPosition = startPosition;
            _endPosition = endPosition;

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
                _bodyImage.transform.right = -_startToEndDirect;
            }
            else
            {
                t = Mathf.Clamp((now - _startTimeAt) / (_duration * 0.6f), 0.0f, 1.0f);
                newPosition = Vector2.Lerp(_startPosition, _endPosition, easeOutCubic(t));
                _bodyImage.transform.right = _startToEndDirect;
            }

            if(_hittedClearAt < now)
            {
                _hittedCharacter.Clear();
                _hittedClearAt = float.MaxValue;
            }

            CircularTargetArea attackArea = new CircularTargetArea(this.transform.position, _projectileRadius);
            CombatSystem.HitOnTargetArea(stage, attackArea, _owner, _projectileDamage, CombatSystem.KnockBackType.Pivot, attackArea.Center, 0.0f, _hittedCharacter, _hittedCharacter, string.Empty);

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
    }
}
