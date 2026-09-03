using DG.Tweening;
using SamMul.Animations.Placeholder;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class HomingProjectileAreaEffectObject : AreaEffectObjectBase
    {
        private static readonly float AttackPeriod = 0.3f;
        public override bool IsAlive => _hitChances > 0 && Time.time <= _createdAt + _lifeTime;

        private Character _owner;
        private Character _target;
        private float _damage;
        private float _projectileRadius;

        private float _createdAt;
        private float _lifeTime;
        bool _willRotateInMoveDirection;
        private int _hitChances;
        private float _lastAttackAt;

        private string _bodyResourcePath;
        private GameObject _body;
        private List<Character> _characterList;

        private Vector2 _movingDirection;
        private float _movingSpeed;
        private float _homingPowerRate;

        private Sequence _fadeoutSequence;
        private Color _saveBodyColor;
        private SpriteRenderer _spriteRenderer;
        private SkeletonAnimation _skeletonAnimation;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.HomingProjectile);
        }

        public void Initialize(
            string bodyResourcePath,
            Monster owner,
            Character target,
            Vector2 firePos,
            Vector2 fireDirection,
            float damage,
            float projectileRadius,
            float moveSpeed,
            float homingPower,
            float lifeTime,
            bool willRotateInMoveDirection,
            int hitChances)
        {
            base.InitializeAreaObject(owner.Alliance);

            _owner = owner;
            _target = target;
            _damage = damage;
            _projectileRadius = projectileRadius;
            _movingSpeed = moveSpeed;
            _homingPowerRate = homingPower;
            _willRotateInMoveDirection = willRotateInMoveDirection;
            _lifeTime = lifeTime;

            _characterList = new List<Character>();
            float now = Time.time;
            _createdAt = now;
            this.transform.position = firePos;
            _movingDirection = fireDirection;// (_target.transform.position - this.transform.position).normalized;
            _hitChances = hitChances;

            _bodyResourcePath = bodyResourcePath;
            if (_body != null)
            {
                Debug.LogWarning($"_body 이미 생성되어있는데 또 구현하려 합니다. 로직이 잘못되었습니다. 확인이 필요합니다.");
            }
            _body = ResourcePool.Instance.InstantiateFromResource(bodyResourcePath);
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector3.zero;

            if (_body.TryGetComponent<SpriteRenderer>(out _spriteRenderer))
            {
                _saveBodyColor = _spriteRenderer.color;
                _fadeoutSequence = DOTween.Sequence();
                _fadeoutSequence.Insert(lifeTime - 0.2f, _body.GetComponent<SpriteRenderer>().DOFade(0f, 0.2f));
            }
            else if (_body.TryGetComponent<SkeletonAnimation>(out _skeletonAnimation))
            {
                _saveBodyColor = _skeletonAnimation.skeleton.GetColor();
                _fadeoutSequence = DOTween.Sequence();
                _fadeoutSequence.Insert(lifeTime - 0.2f, DOVirtual.Float(0f, 1f, 0.2f, v => _skeletonAnimation.skeleton.A = v));
            }
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            this.RotateMoveDirectionToHomingDirection(deltaTime);
            this.RotateBodyImageToMoveDirection();
            this.MoveToCurrentPosition(deltaTime);
            this.HitCheck(stage);
        }

        private void RotateMoveDirectionToHomingDirection(float deltaTime)
        {
            Vector2 targetDirection = _target.transform.position - this.transform.position;
            targetDirection.Normalize();
            _movingDirection = Vector2.Lerp(_movingDirection, targetDirection, _homingPowerRate * deltaTime).normalized;

        }

        // 몸체 이미지 리소스 회전 코드
        private void RotateBodyImageToMoveDirection()
        {
            if (!_willRotateInMoveDirection)
            {
                return;
            }

            Vector2 direction = _movingDirection * _movingSpeed;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _body.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        // 현재 포지션 적용 (이동) 코드
        private void MoveToCurrentPosition(float deltaTime)
        {
            Vector2 currentPosition = this.transform.position;
            Vector2 nextPosition = currentPosition + _movingDirection * deltaTime * _movingSpeed;
            this.transform.position = nextPosition;
        }

        // 타격 처리
        private void HitCheck(Stage stage)
        {
            float now = Time.time;
            if (_lastAttackAt + AttackPeriod > now)
            {
                return;
            }

            // 총알 범위
            CircularTargetArea targetArea = new CircularTargetArea(this.transform.position, _projectileRadius);
            stage.FindAliveCharactersInArea(_owner.Alliance.ToEnemyAlliance(), targetArea, _characterList);
            if (_characterList.Count > 0)
            {
                CombatSystem.HitOnTargetArea(stage, targetArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, knockBackPivot: this.transform.position, 0f, null, null, null);
                --_hitChances;
                _lastAttackAt = now;
            }
        }

        public override void PuttingBackToPool()
        {
            this.transform.localRotation = Quaternion.identity;
            this.transform.localScale = Vector3.one;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _saveBodyColor;
                ResourcePool.Instance.PutBackInstance(_bodyResourcePath, _body);
                _body = null;
                _spriteRenderer = null;
                _bodyResourcePath = null;
            }
            else if (_skeletonAnimation != null)
            {
                _skeletonAnimation.skeleton.SetColor(_saveBodyColor);
                ResourcePool.Instance.PutBackInstance(_bodyResourcePath, _body);
                _body = null;
                _skeletonAnimation = null;
                _bodyResourcePath = null;
            }

            _characterList.Clear();
            if (_fadeoutSequence != null)
            {
                _fadeoutSequence.Kill();
            }

            base.PuttingBackToPool();
        }
    }
}
