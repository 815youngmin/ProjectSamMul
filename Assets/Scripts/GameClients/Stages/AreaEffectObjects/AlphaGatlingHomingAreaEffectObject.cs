using System;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;
using SamMul.UnityHelpers;
using Random = UnityEngine.Random;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class AlphaGatlingHomingAreaEffectObject : AreaEffectObjectBase
    {
        private readonly string DEFAULT_MISSILE_BODY_PREFAB_PATH = "Stages/AreaEffects/Alpha_Gatling/Alpha_Projectile_S.prefab";
        private readonly string DEFAULT_MISSILE_BOOM_PREFAB_PATH = "Stages/AreaEffects/Alpha_Gatling/Alpha_Boom.prefab";

        private readonly string MAGENTA_MISSILE_BODY_PREFAB_PATH = "Stages/AreaEffects/Alpha_Gatling/Magenta_Projectile_S.prefab";
        private readonly string MAGENTA_MISSILE_BOOM_PREFAB_PATH = "Stages/AreaEffects/Alpha_Gatling/Magenta_Boom.prefab";

        public override bool IsAlive => !_isHit && Time.time <= _createdAt + _aliveTime;

        private GameObject _body;
        private SpriteRenderer _bodySpriteRenderer;
        private TrailRenderer[] _trailRenderers;
        private Character _owner;
        private float _findTargetRange;
        private float _damage;
        private float _areaEffectRadius;
        private float _knockBackPower;
        private float _aliveTime;
        private float _homingPowerRate;
        private float _movingSpeed;

        private Character _target;
        private Vector2 _movingDirection;
        private float _createdAt;
        private bool _isHit;
        private bool _isAcceleration;
        private float _acceleration;

        private List<Character> _characterList;
        private string _hitSoundPrefabPath;
        private string _missileBoomPath;

        public void AllocateSharedResources(AreaEffectType areaEffectType)
        {
            Debug.Assert(areaEffectType == AreaEffectType.AlphaGatlingHoming_Default || areaEffectType == AreaEffectType.AlphaGatlingHoming_Magenta);
            base.AllocateSharedResourcesForBase(areaEffectType);

            var bodyPrefabPath = areaEffectType switch
            {
                AreaEffectType.AlphaGatlingHoming_Default => DEFAULT_MISSILE_BODY_PREFAB_PATH,
                AreaEffectType.AlphaGatlingHoming_Magenta => MAGENTA_MISSILE_BODY_PREFAB_PATH,
                _ => throw new NotSupportedException($"AreaEffectType {areaEffectType}이 알파 개틀링 초월이 아닙니다."),
            };
            _body = ResourcePool.Instance.InstantiateFromResource(bodyPrefabPath);
            _body.transform.SetParent(transform);
            _body.transform.localPosition = Vector2.zero;
            _body.transform.localScale = Vector2.one;

            _bodySpriteRenderer = _body.GetComponentInChildren<SpriteRenderer>();
            _trailRenderers = _body.GetComponentsInChildren<TrailRenderer>();

            _missileBoomPath = areaEffectType switch
            {
                AreaEffectType.AlphaGatlingHoming_Default => DEFAULT_MISSILE_BOOM_PREFAB_PATH,
                AreaEffectType.AlphaGatlingHoming_Magenta => MAGENTA_MISSILE_BOOM_PREFAB_PATH,
                _ => throw new NotSupportedException($"AreaEffectType {areaEffectType}이 알파 개틀링 초월이 아닙니다."),
            };
            
        }

        public void Initialize(
            Character owner,
            Stage stage,
            float findTargetRange,
            float areaEffectRadius,
            Vector2 createPosition,
            Vector2 startDirection,
            float movingSpeed,
            float damage,
            float knockBackPower,
            float aliveTime,
            string hitSoundPrefabPath)
        {
            base.InitializeAreaObject(owner.Alliance);
            _owner = owner;
            _findTargetRange = findTargetRange;
            _areaEffectRadius = areaEffectRadius;
            _damage = damage;
            _knockBackPower = knockBackPower;
            _aliveTime = aliveTime;

            this.transform.position = createPosition;
            _movingDirection = startDirection;
            _movingSpeed = movingSpeed;

            _isHit = false;
            _isAcceleration = false;
            _acceleration = movingSpeed * 0.5f;
            _target = this.FindClosestCharacter(stage);

            _movingSpeed = 9f;
            _homingPowerRate = 10f;

            _createdAt = Time.time;

            _characterList = new List<Character>();

            foreach (var trail in _trailRenderers)
            {
                trail.Clear();
            }
            _hitSoundPrefabPath = hitSoundPrefabPath;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            if (!_isAcceleration)
            {
                if (_target == null || _target.Action.IsDead)
                {
                    _target = FindClosestCharacter(stage);
                }
            }

            this.RotateMoveDirectionToHomingDirection(deltaTime);
            this.RotateBodyImageToMoveDirection();
            this.MoveToCurrentPosition(deltaTime);
            this.HitCheck(stage);
        }

        //미사일 기준 가까운 타겟 찾는 코드
        private Character FindClosestCharacter(Stage stage)
        {
            return stage.FindClosestCharacter(
                _owner.Alliance.ToEnemyAlliance(),
                this.transform.position,
                _findTargetRange,
                 condition: character => !character.Action.IsDead && !character.IsImmuneToHit
                );
        }

        //타겟 방향으로 이동 방향 돌리는 코드
        private void RotateMoveDirectionToHomingDirection(float deltaTime)
        {
            if (_target != null && !_isAcceleration)
            {
                Vector2 targetDirection = _target.transform.position - this.transform.position;
                targetDirection.Normalize();

                _movingDirection = Vector2.Lerp(_movingDirection, targetDirection, _homingPowerRate * deltaTime).normalized;

                float dotProduct = Vector2.Dot(_movingDirection.normalized, targetDirection.normalized);
                float angle = Mathf.Acos(dotProduct) * Mathf.Rad2Deg;

                if (angle <= 5f)
                {
                    // 두 벡터의 각도가 5도 이내에 있는 경우 가속 상태로 만든다.
                    _isAcceleration = true;
                    _movingSpeed = _movingSpeed * 3f;
                }
            }
        }

        //몸체 이미지 리소스 회전 코드
        private void RotateBodyImageToMoveDirection()
        {
            Vector2 direction = _movingDirection * _movingSpeed;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _body.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        //현재 포지션 적용 (이동) 코드
        private void MoveToCurrentPosition(float deltaTime)
        {
            if (_isAcceleration)
            {
                Vector2 currentPosition = this.transform.position;
                Vector2 nextPosition = currentPosition + _movingDirection * deltaTime * _movingSpeed;
                this.transform.position = nextPosition;

                _movingSpeed += _acceleration * deltaTime;

            }
            else
            {
                Vector2 currentPosition = this.transform.position;
                Vector2 nextPosition = currentPosition + _movingDirection * deltaTime * _movingSpeed;
                this.transform.position = nextPosition;
            }

        }

        //타격 처리 (타격 되었으면 제거됩니다)
        private void HitCheck(Stage stage)
        {
            //총알 범위
            CircularTargetArea missileArea = new CircularTargetArea(this.transform.position, 0.5f);
            stage.FindAliveCharactersInArea(_owner.Alliance.ToEnemyAlliance(), missileArea, _characterList);

            if (_characterList.Count > 0)
            {
                //폭발 범위
                CircularTargetArea targetArea = new CircularTargetArea(this.transform.position, _areaEffectRadius);
                CombatSystem.HitOnTargetArea(stage, targetArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, knockBackPivot: this.transform.position, _knockBackPower, null, null, _hitSoundPrefabPath);

                UnityGlobal.SpriteAnimations.CreateAndPlaySpriteAnimation(_missileBoomPath, this.transform.position, Random.insideUnitCircle, this.transform.localScale * 0.5f, null, null, null);
                _isHit = true;
            }
        }
    }
}
