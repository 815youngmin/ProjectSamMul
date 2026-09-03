using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.Characters.Monsters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class ReflectionAreaEffectMultiHitObject : SmartAreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + _lifeTime && _leftHitChances > 0;

        private Monster _owner;
        private float _objectRadius;
        private Vector2 _movingDirection;
        private float _movingSpeed;
        private float _rotatingSpeed;
        private float _damage;
        private float _lifeTime;
        private float _createdAt;
        private int _leftHitChances;

        private Rect _moveRect;

        private GameObject _bodyImage;
        private string _bodyPath;
        private Collider2D[] _overlappedColliders;
        private RaycastHit2D[] _raycastHits;

        private float _tickDamagePeriod = 0.25f;
        private float _tickDamageClearNextAt;
        private HashSet<Character> _hittedCharacters;

        //Initialize 단계에서 이미지 경로 받아와서 처리해야되는 경우가 생겨 아래 함수 추가 
        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForSmartBase(AreaEffectType.ReflectionMultiHitObject);

            _bodyImage = null;
            _overlappedColliders = new Collider2D[16];
            _raycastHits = new RaycastHit2D[16];
            _hittedCharacters = new HashSet<Character>();
        }

        public void Initialize(
            Monster owner,
            float objectRadius,
            Vector2 startPosition,
            Vector2 movingDirection,
            float movingSpeed,
            float rotatingSpeed,
            float damage,
            float lifeTime,
            int hitChances,
            Rect moveRect,
            string bodyImagePath)
        {
            base.InitializeAreaObject(owner.Alliance);
            _owner = owner;
            _objectRadius = objectRadius;
            _movingDirection = movingDirection.normalized;
            _movingSpeed = movingSpeed;
            _rotatingSpeed = rotatingSpeed;
            _damage = damage;
            _lifeTime = lifeTime;
            _leftHitChances = hitChances;
            _moveRect = moveRect;
            _createdAt = Time.time;
            this.transform.position = startPosition;
            _tickDamageClearNextAt = Time.time;

            if (_bodyImage != null)
            {
                Debug.LogWarning($"_bodyImage가 이미 생성되어있는데 또 구현하려 합니다. 로직이 잘못되었습니다. 확인이 필요합니다.");
            }
            _bodyPath = bodyImagePath;
            _bodyImage = ResourcePool.Instance.InstantiateFromResource(bodyImagePath);
            _bodyImage.transform.SetParent(this.gameObject.transform);
            _bodyImage.transform.localPosition = Vector3.zero;

            float angle = Mathf.Rad2Deg * Mathf.Atan2(_movingDirection.y, _movingDirection.x);
            _bodyImage.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            _hittedCharacters.Clear();
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            base.UpdateLogic(stage, deltaTime);

            float now = Time.time;
            if (_tickDamageClearNextAt <= now)
            {
                _hittedCharacters.Clear();
                _tickDamageClearNextAt = now + _tickDamagePeriod;
            }

            CombatSystem.HandleAreaEffectCollisionWithCharacter(
                areaEffectObject: this,
                position: transform.position,
                radius: _objectRadius,
                direction: _movingDirection,
                speed: _movingSpeed,
                deltaTime: deltaTime,
                overlappedColliders: _overlappedColliders,
                raycastHits: _raycastHits,
                onHitCharacter: (character) =>
                {
                    if(!_hittedCharacters.Contains(character))
                    {
                        _hittedCharacters.Add(character);
                        character.Hitted(stage, _owner, _damage, _movingDirection, transform.position, hitSoundPrefabPath: string.Empty);
                        _leftHitChances--;
                    }
                });

            if (!IsAlive)
            {
                return;
            }

            this.Move(deltaTime);
            this.CheckAndReflect();
            this.Rotate(deltaTime);
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            //해당 타입은 Initialilze단계에서 몸체 이미지를 생성하기 때문에 바디 이미지를 제거 해줘야된다.
            if (_bodyImage != null)
            {
                ResourcePool.Instance.PutBackInstance(_bodyPath, _bodyImage);
                _bodyImage = null;
            }
            _hittedCharacters.Clear();

            //외부에서transform 사이즈 연출을 Dotween으로 처리하고있어 해당 코드를 추가했다.
            DOTween.Kill(this.transform);
        }

        private void Move(float deltaTime)
        {
            this.transform.Translate(deltaTime * _movingSpeed * _movingDirection);
        }

        private void CheckAndReflect()
        {
            Vector2 position = this.transform.position;
            bool isReflecting = false;

            if (position.x < _moveRect.xMin)
            {
                position.x = _moveRect.xMin;
                _movingDirection = Vector2.Reflect(_movingDirection, Vector2.right).normalized;
                isReflecting = true;
            }
            else if (position.x > _moveRect.xMax)
            {
                position.x = _moveRect.xMax;
                _movingDirection = Vector2.Reflect(_movingDirection, Vector2.left).normalized;
                isReflecting = true;
            }
            else if (position.y > _moveRect.yMax)
            {
                position.y = _moveRect.yMax;
                _movingDirection = Vector2.Reflect(_movingDirection, Vector2.down).normalized;
                isReflecting = true;
            }
            else if (position.y < _moveRect.yMin)
            {
                position.y = _moveRect.yMin;
                _movingDirection = Vector2.Reflect(_movingDirection, Vector2.up).normalized;
                isReflecting = true;
            }

            if (isReflecting)
            {
                this.transform.position = position;
                if (_rotatingSpeed == 0.0f)
                {
                    float angle = Mathf.Rad2Deg * Mathf.Atan2(_movingDirection.y, _movingDirection.x);
                    _bodyImage.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
                }
            }
        }

        private void Rotate(float deltaTime)
        {
            if (_rotatingSpeed == 0.0f)
            {
                return;
            }

            _bodyImage.transform.Rotate(0.0f, 0.0f, deltaTime * _rotatingSpeed);
        }
    }
}

