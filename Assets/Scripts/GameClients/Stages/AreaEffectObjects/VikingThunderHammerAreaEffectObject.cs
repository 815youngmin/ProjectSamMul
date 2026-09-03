using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class VikingThunderHammerAreaEffectObject : AreaEffectObjectBase
    {
        private const string _hammerPrefabPath = "Stages/AreaEffects/VikingThunderWarrior/VikingThunderHammer.prefab";

        public override bool IsAlive => Time.time < _endTimeAt;

        private Character _owner;
        private GameObject _bodyImage;
        private float _hammerDamage;
        private float _lightningDamage;
        private Vector2 _startPosition;
        private Vector2 _endPosition;
        private float _duration;
        private float _startTimeAt;
        private float _tuneTimeAt;
        private float _endTimeAt;
        private Vector2 _startToEndDirect;

        private int _lightningIndex;
        private List<float> _lightningTiming = new List<float>();
        private Vector2 _lightningDirect;
        private float _lightningDistance = 2.0f;
        private float _lightningIndicatorDuration = 1f;
        private float _lightningRadius = 3.0f;

        private int _projectileAmount;
        private float _projectileSpeed;
        private float _projectileAcceleration;
        private float _projectileAliveDistance;
        private float _projectileKnobackPower;

        private HashSet<Character> _hittedCharacter = new HashSet<Character>();
        private float _hittedClearAt;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.VikingThunderHammer);
            _bodyImage = ResourcePool.Instance.InstantiateFromResource(_hammerPrefabPath);
            _bodyImage.transform.SetParent(this.gameObject.transform);
            _bodyImage.transform.localPosition = Vector3.zero;
            _bodyImage.transform.localScale = Vector3.one;
            _bodyImage.transform.localRotation = Quaternion.identity;
        }

        public void Initialize(
                Character owenr,
                float duration,
                Vector2 startPosition,
                Vector2 endPosition,
                float hammerDamage,
                float lightningDamage,
                int lightningCount,
                int projectileAmount,
                float projectileSpeed,
                float projectileAcceleration,
                float projectileAliveDistance,
                float projectileKnobackPower
                )
        {
            _owner = owenr;
            _hammerDamage = hammerDamage;

            this.transform.position = startPosition;
            _startPosition = startPosition;
            _endPosition = endPosition;

            _duration = duration;
            _startTimeAt = Time.time;
            _tuneTimeAt = _startTimeAt + (duration * 0.6f);
            _hittedClearAt = _tuneTimeAt;
            _endTimeAt = _startTimeAt + duration;

            _startToEndDirect = (_endPosition - _startPosition).normalized;

            _lightningIndex = 0;
            _lightningDamage = lightningDamage;

            _projectileAmount = projectileAmount;
            _projectileSpeed = projectileSpeed;
            _projectileAcceleration = projectileAcceleration;
            _projectileAliveDistance = projectileAliveDistance;
            _projectileKnobackPower = projectileKnobackPower;

            _lightningTiming.Clear();
            float interval = 1.0f / (float)(lightningCount + 1);
            for (int i = 1; i < lightningCount + 1; ++i)
            {
                _lightningTiming.Add(i * interval);
            }

            int randomValue = Random.Range(0, 2);
            Vector2 crossVector = Vector3.Cross(_startToEndDirect, Vector3.forward);
            _lightningDirect = (randomValue == 1) ? crossVector : -crossVector;
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

                int tryCount = 0;
                int lightningCount = _lightningTiming.Count;
                while (_lightningIndex < lightningCount && _lightningTiming[_lightningIndex] < t && tryCount++ < 10)
                {
                    Vector2 tempPos = Vector2.Lerp(_startPosition, _endPosition, _lightningTiming[_lightningIndex]);
                    tempPos += (_lightningDirect * _lightningDistance);
                    _lightningDirect *= -1.0f;

                    stage.CreateVikingThunderWarriorLightningAreaEffectObject(_owner, tempPos, _lightningRadius, _lightningDamage, _lightningIndicatorDuration, _projectileAmount, _projectileSpeed, _projectileAcceleration, _projectileAliveDistance, _projectileKnobackPower);
                    ++_lightningIndex;
                }
            }

            if(_hittedClearAt < now)
            {
                _hittedCharacter.Clear();
                _hittedClearAt = float.MaxValue;
            }

            CircularTargetArea attackArea = new CircularTargetArea(this.transform.position, 2.77f * 0.5f);
            CombatSystem.HitOnTargetArea(stage, attackArea, _owner, _hammerDamage, CombatSystem.KnockBackType.Pivot, attackArea.Center, 0.0f, _hittedCharacter, _hittedCharacter, string.Empty);

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
