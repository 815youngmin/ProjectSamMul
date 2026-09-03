using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class LaserAreaEffectObject : AreaEffectObjectBase
    {
        private bool _isAlive;
        public override bool IsAlive => _isAlive;

        private GameObject _body;

        private Character _owner;
        private Vector2 _startDirection;
        private float _angle;
        private float _attackDuration;
        private float _distance;
        private float _thickness;
        private float _damage;
        private float _attackTickInterval;

        private float _accumulatedTime;
        private float _nextHittedClearAt;

        private HashSet<Character> _hittedCharactes = new HashSet<Character>();

        private AreaEffectType _areaEffectType;

        public void AllocateSharedResources(AreaEffectType areaEffectType, string prefabPath)
        {
            base.AllocateSharedResourcesForBase(areaEffectType);
            _body = ResourcePool.Instance.InstantiateFromResource(prefabPath);
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector3.zero;
            _body.transform.localScale = Vector3.one;

            _areaEffectType = areaEffectType;
        }

        public void Initialize(
                    Character owner,  
                    float damage,
                    float attackTickInterval,
                    float distance,
                    float thickness,
                    Vector2 startDirection,
                    float angle,
                    float attackDuration,
                    bool isRightRotate,
                    bool isBodyEnable
                    )
        {
            _owner = owner;
            _isAlive = true;
            _attackDuration = attackDuration;
            _damage = damage;
            _distance = distance;
            _thickness = thickness;
            _attackTickInterval = attackTickInterval;
            _accumulatedTime = 0.0f;
            _nextHittedClearAt = 0.0f;

            this.transform.right = startDirection;
            this._startDirection= startDirection;
            this._angle = angle * (isRightRotate ? -1.0f : 1.0f);

            if (_areaEffectType == AreaEffectType.XxperManLaser)
            {
                //이미지 리소스에 여백이 있어서 조금더 크게 사이즈 잡아줘야됨
                SpriteRenderer spriteRenderer = _body.GetComponentInChildren<SpriteRenderer>();
                spriteRenderer.size = new Vector2(_distance * 1.13f, thickness);
                this.transform.localScale = Vector3.one;
            }
            else if(_areaEffectType == AreaEffectType.LaserAreaEffect)
            {
                this.transform.localScale = new Vector3(_distance, thickness, 1.0f);
            }


            _body.SetActive(isBodyEnable);
        }

        public void Initialize(
            Character owner,
            float damage,
            float attackTickInterval,
            float distance,
            float thickness,
            Vector2 startDirection,
            float angle,
            float attackDuration,
            bool isRightRotate
            )
        {
            this.Initialize(owner, damage, attackTickInterval, distance, thickness, startDirection, angle, attackDuration, isRightRotate, true);
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;
            if(_nextHittedClearAt < now)
            {
                _hittedCharactes.Clear();
                _nextHittedClearAt = now + _attackTickInterval;
            }

            float t = _accumulatedTime / _attackDuration;

            float angle = Mathf.Lerp(0.0f, _angle, t);
            this.transform.right = Quaternion.AngleAxis(angle,Vector3.forward) * _startDirection;

            Vector3 currentDirection = this.transform.right;

            Vector3 squareCenter = (currentDirection * _distance * 0.5f) + this.transform.position;
            SquareTargetArea attackArea = new SquareTargetArea(squareCenter, new Vector2(_distance, _thickness*0.5f), Vector3.SignedAngle(Vector3.right, currentDirection, Vector3.forward));
            CombatSystem.HitOnTargetArea(stage, attackArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, attackArea.Center, 1.0f, _hittedCharactes, _hittedCharactes, hitSoundPrefabPath: string.Empty);

            if (1.0f < t)
            {
                _isAlive = false;
                return;
            }

            _accumulatedTime += deltaTime;
        }
    }
}
