using System;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class AxeBoomerangAreaEffectObject : AreaEffectObjectBase
    {
        private static readonly string AXE_PREFAB_PATH = "Stages/AreaEffects/VikingAxeLeader/AxeBoomerang.prefab";

        private bool _isAlive;
        public override bool IsAlive => _isAlive;

        private HashSet<Character> _hittedCharactersInAttackPeriod = new HashSet<Character>();
        private float _lastClearedHittedCharactersAt;

        private GameObject _bodyImage;

        private Character _owner;
        private float _attackPeriod;

        private Vector2 _pivotPosition;
        private float _remainAngle;
        private float _moveDistance;
        private float _distanceFromPivot;
        private float _currentAngleDegree;
        private Action _returnCallBack;
        private float _damage;

        private const float _attackRadius = 2.8f * 0.5f;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.AxeBoomerang);
            _bodyImage = ResourcePool.Instance.InstantiateFromResource(AXE_PREFAB_PATH);
            _bodyImage.transform.SetParent(this.gameObject.transform);
            _bodyImage.transform.localPosition = Vector3.zero;
            _bodyImage.transform.localScale = Vector3.one;
            _bodyImage.transform.localRotation= Quaternion.identity;

            _hittedCharactersInAttackPeriod.Clear();
        }

        public void Initialize(
            Character owner,
            Vector2 startPosition,
            Vector2 targetPosition,
            float moveDistance,
            float damage,
            float attackPeriod,
            Action returnCallBack)
        {
            base.InitializeAreaObject(owner.Alliance);

            _owner = owner;
            _attackPeriod = attackPeriod;
            _moveDistance = moveDistance;
            _damage = damage;
            _pivotPosition = (startPosition + targetPosition) * 0.5f;

            Vector2 dirctionFromPivot = (startPosition - _pivotPosition);
            _distanceFromPivot = (dirctionFromPivot).magnitude;

            _remainAngle = 360.0f;
            _currentAngleDegree = Vector3.SignedAngle(Vector3.right, dirctionFromPivot.normalized, Vector3.forward);

            _returnCallBack = returnCallBack;
            _isAlive = true;
            _lastClearedHittedCharactersAt = 0.0f;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            if(_lastClearedHittedCharactersAt < Time.time)
            {
                _lastClearedHittedCharactersAt = Time.time + _attackPeriod;
                _hittedCharactersInAttackPeriod.Clear();
            }

            this.Attack(stage);

            if(!_isAlive)
            {
                return;
            }

            this.MoveToCurrentPosition(deltaTime);

            _bodyImage.transform.Rotate(0.0f, 0.0f, 360.0f * 5.0f * deltaTime);

            if (_remainAngle<= 0.0f)
            {
                _isAlive = false;
                _returnCallBack?.Invoke();
            }
        }

        private void MoveToCurrentPosition(float deltaTime)
        {
            float dtDistance = deltaTime * _moveDistance;
            float circumference = _distanceFromPivot * 2.0f * Mathf.PI;

            float deltaDegree = (dtDistance / circumference) * 360.0f;
            _remainAngle -= Mathf.Abs(deltaDegree);
            if(_remainAngle < 0.0f)
            {
                deltaDegree -= (deltaDegree < 0 ? _remainAngle : -_remainAngle);
            }

            _currentAngleDegree += deltaDegree;

            float radian = _currentAngleDegree * Mathf.Deg2Rad;
            var offset = new Vector2(_distanceFromPivot * Mathf.Cos(radian), _distanceFromPivot * Mathf.Sin(radian));
            var nextPosition = _pivotPosition + offset;

            this.transform.position = nextPosition;
        }

        private void Attack(Stage stage)
        {
            CircularTargetArea attackArea = new CircularTargetArea(this.transform.position, _attackRadius);
            CombatSystem.HitOnTargetArea(stage, attackArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, attackArea.Center, 0.0f, _hittedCharactersInAttackPeriod, _hittedCharactersInAttackPeriod, hitSoundPrefabPath: string.Empty);
        }
    }
}
