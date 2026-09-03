using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.Characters.Monsters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class EraMixGenieReflectionFollowUpObject : AreaEffectObjectBase
    {
        public override bool IsAlive => _lifeTime > 0 && _leftHitChances > 0;

        private Monster _owner;
        private float _objectRadius;
        private Vector2 _movingDirection;
        private float _movingSpeed;
        private float _rotatingSpeed;
        private float _damage;
        private float _lifeTime;
        private int _leftHitChances;

        private Rect _moveRect;

        private GameObject _bodyImage;
        private string _bodyPath;
        private Collider2D[] _overlappedColliders;
        private RaycastHit2D[] _raycastHits;

        private float _tickDamagePeriod = 0.25f;
        private float _tickDamageClearNextAt;
        private HashSet<Character> _hittedCharacters;

        private float _areaEffectCreateNextAt;

        private static readonly float ReflectionObjectDamageRatio = 1.0f;
        private static readonly float ReflectionObjectSpeed = 5;
        private static readonly float ReflectionObjectLifeTime = 12f;
        private static readonly float ReflectionObjectRadius = 2.0f;
        private static readonly float ReflectionObjectRotateSpeed = 720f;
        private static readonly int ReflectionObjectHitChances = 1;
        private static readonly string ReflectionObjectBodyPath = "Stages/Projectiles/Stone_Radius2.prefab";


        private static readonly float AreaEffectCreateInterval = 0.5f;
        private static readonly float AreaEffectDamageRatio = 1.0f;
        private static readonly float AreaEffectRadius = 2f;
        private static readonly float LastAreaEffectRadius = 3f;
        private static readonly float IndicatorDuration = 0.5f;
        private static readonly float AreaEffectAttackPeriod = 0.25f;
        private static readonly float AreaEffectLifeTime = 10f;
        private static readonly float Delay = 0;

        //Initialize 단계에서 이미지 경로 받아와서 처리해야되는 경우가 생겨 아래 함수 추가 
        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.EraMixGenieReflectionFollowUpObject);

            _bodyPath = ReflectionObjectBodyPath;
            _bodyImage = ResourcePool.Instance.InstantiateFromResource(_bodyPath);
            _bodyImage.transform.SetParent(this.gameObject.transform);
            _bodyImage.transform.localPosition = Vector3.zero;

            _overlappedColliders = new Collider2D[16];
            _raycastHits = new RaycastHit2D[16];
            _hittedCharacters = new HashSet<Character>();
        }

        public void Initialize(
            Monster owner,
            Vector2 startPosition,
            Vector2 movingDirection,
            Rect moveRect)
        {
            base.InitializeAreaObject(owner.Alliance);
            _owner = owner;
            _movingDirection = movingDirection.normalized;
            _moveRect = moveRect;
            this.transform.position = startPosition;
            _tickDamageClearNextAt = Time.time;
            _areaEffectCreateNextAt = Time.time;

            float angle = Mathf.Rad2Deg * Mathf.Atan2(_movingDirection.y, _movingDirection.x);
            _bodyImage.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            _hittedCharacters.Clear();

            _objectRadius = ReflectionObjectRadius;
            _movingSpeed = ReflectionObjectSpeed;
            _rotatingSpeed = ReflectionObjectRotateSpeed;
            _damage = _owner.SpecialAttackPower * ReflectionObjectDamageRatio;
            _lifeTime = ReflectionObjectLifeTime;
            _leftHitChances = ReflectionObjectHitChances;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;
            _lifeTime -= deltaTime;

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
                    if (!_hittedCharacters.Contains(character))
                    {
                        _hittedCharacters.Add(character);
                        character.Hitted(stage, _owner, _damage, _movingDirection, transform.position, hitSoundPrefabPath: string.Empty);
                        _leftHitChances--;
                    }
                });

            if (!IsAlive)
            {
                this.CreateLastSandAreaEffect(stage, this.transform.position);
                return;
            }

            this.Move(deltaTime);
            this.CheckAndReflect();
            this.Rotate(deltaTime);

            if (_areaEffectCreateNextAt <= now)
            {
                _areaEffectCreateNextAt = now + AreaEffectCreateInterval;
                this.CreateSandAreaEffect(stage, this.transform.position);
            }

        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
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

        private void CreateSandAreaEffect(Stage stage, Vector2 createPos)
        {
            stage.CreateSandAreaEffectObject(_owner, Delay, IndicatorDuration, _owner.SpecialAttackPower * AreaEffectDamageRatio,
                                             AreaEffectAttackPeriod, AreaEffectLifeTime, AreaEffectRadius, createPos);
        }

        private void CreateLastSandAreaEffect(Stage stage, Vector2 createPos)
        {
            stage.CreateSandAreaEffectObject(_owner, Delay, IndicatorDuration, _owner.SpecialAttackPower * AreaEffectDamageRatio,
                                             AreaEffectAttackPeriod, AreaEffectLifeTime, LastAreaEffectRadius, createPos);
        }

    }

}
