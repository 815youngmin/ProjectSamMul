using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;
using SamMul.UnityHelpers;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class DrHomingSmallMissileAreaEffectObject : AreaEffectObjectBase
    {
        private static readonly float ExplosionRadius = 1.25f;
        private static readonly float WarningSequenceDuration = 0.95f;
        private static readonly string ExplosionPath = "Stages/ETCEffects/missile_boom.prefab";

        public override bool IsAlive => !_isExplosion;

        private Character _owner;
        private Character _target;
        private float _damage;
        private float _projectileRadius;

        private GameObject _body;
        private SpriteRenderer _bodyRenderer;
        private List<Character> _characterList;
        private HashSet<Character> _hittedCharacters;

        private Vector2 _movingDirection;
        private float _movingSpeed;
        private float _homingPowerRate;

        private float _warningAt;
        private float _explosionAt;
        private bool _isExplosion;
        private bool _isHit;

        private Sequence _warnningSequence;


        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.DrHomingSmallMissileObject);
            _body = ResourcePool.Instance.InstantiateFromResource("Stages/Projectiles/MissileRadius0_25.prefab");
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector3.zero;
            _bodyRenderer = _body.GetComponent<SpriteRenderer>();
            _warnningSequence = DOTween.Sequence(this.transform);
            _warnningSequence.Append(_bodyRenderer.DOColor(new Color(1f, 0.5f, 0.5f), 0.15f));
            _warnningSequence.Append(_bodyRenderer.DOColor(new Color(1f, 0.8f, 0.8f), 0.25f));
            _warnningSequence.Append(_bodyRenderer.DOColor(new Color(1f, 0.2f, 0.2f), 0.15f));
            _warnningSequence.Append(_bodyRenderer.DOColor(new Color(1f, 0.8f, 0.8f), 0.25f));
            _warnningSequence.Append(_bodyRenderer.DOColor(new Color(1f, 0.2f, 0.2f), 0.15f));
            _warnningSequence.SetAutoKill(false);
            _warnningSequence.Pause();
        }

        public void Initialize(
            Monster owner,
            Character target,
            Vector2 firePos,
            Vector2 fireDirection,
            float damage,
            float projectileRadius,
            float moveSpeed,
            float homingPower,
            float lifeTime)
        {
            base.InitializeAreaObject(owner.Alliance);

            _owner = owner;
            _target = target;
            _damage = damage;
            _projectileRadius = projectileRadius;
            _movingSpeed = moveSpeed;
            _homingPowerRate = homingPower;

            _characterList = new List<Character>();
            _hittedCharacters = new HashSet<Character>();
            float now = Time.time;
            this.transform.position = firePos;
            _movingDirection = fireDirection;

            _warningAt = now + lifeTime - WarningSequenceDuration;
            _explosionAt = now + lifeTime;
            _isExplosion = false;
            _isHit = false;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            this.RotateMoveDirectionToHomingDirection(deltaTime);
            this.RotateBodyImageToMoveDirection();
            this.MoveToCurrentPosition(deltaTime);
            this.HitCheck(stage);

            float now = Time.time;

            if(_warningAt <= now)
            {
                _warningAt = float.MaxValue;
                _warnningSequence.Restart();
            }

            if(_explosionAt <= now || _isHit)
            {
                _explosionAt = float.MaxValue;
                _isExplosion = true;
                this.Explosion(stage);
            }
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
            // 총알 범위
            CircularTargetArea targetArea = new CircularTargetArea(this.transform.position, _projectileRadius);
            stage.FindAliveCharactersInArea(_owner.Alliance.ToEnemyAlliance(), targetArea, _characterList);
            if (_characterList.Count > 0)
            {
                CombatSystem.HitOnTargetArea(stage, targetArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, knockBackPivot: this.transform.position, 0f, _hittedCharacters, _hittedCharacters, null);
                _isHit = true;
            }
        }

        private void Explosion(Stage stage)
        {
            CircularTargetArea targetArea = new CircularTargetArea(this.transform.position, ExplosionRadius);
            CombatSystem.HitOnTargetArea(stage, targetArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, knockBackPivot: this.transform.position, 0f, _hittedCharacters, _hittedCharacters, null);
            UnityGlobal.SpriteAnimations.CreateAndPlaySpriteAnimation(ExplosionPath, this.transform.position, Vector2.one * ExplosionRadius, null);
        }


        public override void PuttingBackToPool()
        {
            _warnningSequence.Pause();
            _bodyRenderer.color = Color.white;
            _characterList.Clear();
            _hittedCharacters.Clear();
            base.PuttingBackToPool();
        }
    }
}
