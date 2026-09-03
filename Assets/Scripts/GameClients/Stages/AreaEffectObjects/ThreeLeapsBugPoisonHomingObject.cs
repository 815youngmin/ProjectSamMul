using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.Characters.Monsters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class ThreeLeapsBugPoisonHomingObject : AreaEffectObjectBase
    {
        public override bool IsAlive => _isAlive;

        private Monster _owner;
        private Character _target;
        private float _objectRadius;
        private Vector2 _movingDirection;
        private float _movingSpeed;
        private float _damage;
        private float _lifeTime;
        private float _createdAt;
        private bool _isAlive;

        private float _poisonousAreaDuration;
        private float _poisonousAreaRadius;
        private float _poisonousAreaDamage;

        private Rect _moveRect;

        private GameObject _bodyImage;

        private Sequence _scaleAnimation;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.ThreeLeapsBugPoisonHomingObject);
            _bodyImage = ResourcePool.Instance.InstantiateFromResource("Stages/AreaEffects/PoisonEffects/PoisonBallRadius0_8.prefab");
            _bodyImage.transform.SetParent(this.gameObject.transform);
            _bodyImage.transform.localPosition = Vector3.zero;

            _scaleAnimation = DOTween.Sequence();
            _scaleAnimation.Append(transform.DOPunchScale(Vector3.one * 0.2f, 0.5f, 1, 0).SetLoops(2));
            _scaleAnimation.AppendInterval(0.2f);
            _scaleAnimation.SetLoops(-1);
            _scaleAnimation.Pause();

        }

        public void Initialize(
            Monster owner,
            Character target,
            float objectRadius,
            Vector2 startPosition,
            Vector2 movingDirection,
            float movingSpeed,
            float damage,
            float lifeTime,
            float poisonousAreaDuration,
            float poisonousAreaRadius,
            float poisonousAreaDamage,
            Rect moveRect)
        {
            base.InitializeAreaObject(owner.Alliance);
            _owner = owner;
            _target = target;
            _objectRadius = objectRadius;
            _movingDirection = movingDirection.normalized;
            _movingSpeed = movingSpeed;
            _damage = damage;
            _lifeTime = lifeTime;
            _moveRect = moveRect;
            _createdAt = Time.time;
            _poisonousAreaDuration = poisonousAreaDuration;
            _poisonousAreaRadius = poisonousAreaRadius;
            _poisonousAreaDamage = poisonousAreaDamage;

            this.transform.position = startPosition;
            _bodyImage.transform.localScale = _objectRadius / 0.8f * Vector3.one;
            _isAlive = true;
            _scaleAnimation.Restart();

        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            if(!_isAlive)
            {
                return;
            }

            float now = Time.time;

            this.MoveToCurrentPosition(deltaTime);
            this.ReflectionToRect();
            this.RotateBodyImageToMoveDirection();

            HashSet<Character> hittedCharacters = new HashSet<Character>();
            CircularTargetArea area = new CircularTargetArea(this.transform.position, _objectRadius);
            CombatSystem.HitOnTargetArea(
                stage, area, _owner, _damage, CombatSystem.KnockBackType.Pivot,
                _owner.Pos, 0f, hittedCharacters, null,null);

            if (hittedCharacters.Count > 0 || _createdAt + _lifeTime < now)
            {
                stage.CreatePoisonousAreaEffect(
                    owner: _owner,
                    position: this.transform.position,
                    lifeTime: _poisonousAreaDuration,
                    tickPeriod: 0.25f,
                    damage: _poisonousAreaDamage,
                    radius: _poisonousAreaRadius);

                _isAlive = false;
            }
        }

        public override void PuttingBackToPool()
        {
            _scaleAnimation.Pause();
            base.PuttingBackToPool();
        }

        private void MoveToCurrentPosition(float deltaTime)
        {
            Vector2 currentPosition = this.transform.position;
            Vector2 nextPosition = currentPosition + _movingDirection * _movingSpeed * deltaTime;
            this.transform.position = nextPosition;
        }

        private void ReflectionToRect()
        {
            Vector2 movePosition = this.transform.position;
            if (movePosition.x < _moveRect.xMin)
            {
                movePosition.x = _moveRect.xMin;
                _movingDirection = _target.Pos - movePosition;
            }
            else if (movePosition.x > _moveRect.xMax)
            {
                movePosition.x = _moveRect.xMax;
                _movingDirection = _target.Pos - movePosition;
            }
            else if (movePosition.y > _moveRect.yMax)
            {
                movePosition.y = _moveRect.yMax;
                _movingDirection = _target.Pos - movePosition;
            }
            else if (movePosition.y < _moveRect.yMin)
            {
                movePosition.y = _moveRect.yMin;
                _movingDirection = _target.Pos - movePosition;
            }

            _movingDirection.Normalize();
            this.transform.position = movePosition;
        }

        private void RotateBodyImageToMoveDirection()
        {
            Vector2 direction = _movingDirection * _movingSpeed;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _bodyImage.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        }
    }
}
