using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class EraMixKojiroSplitToReflectionObject : AreaEffectObjectBase
    {
        public override bool IsAlive => !_isSplited;
        private bool _isSplited;

        private Monster _owner;
        private Vector2 _objectMovingDirection;
        private float _objectDamage;
        private float _splitAt;

        private Rect _moveRect;

        private GameObject _bodyImage;
        private Collider2D[] _overlappedColliders;
        private RaycastHit2D[] _raycastHits;

        private static readonly float SPLITOBJECT_DAMAGE_COEFFICIENT = 1.0f;

        private static readonly string SplitProjectilePath = "Stages/Projectiles/ShurikenBig2_0.prefab";
        private static readonly float SplitProjectileRadius = 1.0f;
        private static readonly float SplitProjectileSpeed = 10;
        private static readonly float SplitProjectileLifeTime = 20f;
        private static readonly float SplitProjectileRotateSpeed = 720f;

        private Vector2 _splitDirection = Vector2.zero;

        private float _tickDamagePeriod = 0.25f;
        private float _tickDamageClearNextAt;
        private HashSet<Character> _hittedCharacters;


        //초기화 단계에서 리소스 경로 입력받아서 처리하기 위해 처리
        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.EraMixKojiroSplitToReflectionObject);

            _overlappedColliders = new Collider2D[16];
            _raycastHits = new RaycastHit2D[16];
            _hittedCharacters = new HashSet<Character>();

            _bodyImage = ResourcePool.Instance.InstantiateFromResource(SplitProjectilePath);
            _bodyImage.transform.SetParent(this.transform);
            _bodyImage.transform.localPosition = Vector2.zero;
        }

        public void Initialize(
            Stage stage,
            Monster owner,
            Vector2 startPosition,
            Vector2 startDirection
            )
        {
            base.InitializeAreaObject(owner.Alliance);

            _owner = owner;
            _objectMovingDirection = startDirection.normalized;
            _objectDamage = _owner.SpecialAttackPower * SPLITOBJECT_DAMAGE_COEFFICIENT;
            _moveRect = stage.FenceRect != null ? stage.FenceRect.Value : stage.StaticData.GetWalkableArea();

            _splitAt = Time.time + SplitProjectileLifeTime;

            _isSplited = false;
            this.transform.position = startPosition;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;
            if (_tickDamageClearNextAt <= now)
            {
                _hittedCharacters.Clear();
                _tickDamageClearNextAt = now + _tickDamagePeriod;
            }

            CombatSystem.HandleAreaEffectCollisionWithCharacter(
                areaEffectObject: this,
                position: transform.position,
                radius: SplitProjectileRadius,
                direction: _objectMovingDirection,
                speed: SplitProjectileSpeed,
                deltaTime: deltaTime,
                overlappedColliders: _overlappedColliders,
                raycastHits: _raycastHits,
                onHitCharacter: (character) =>
                {

                    if (!_hittedCharacters.Contains(character))
                    {
                        _hittedCharacters.Add(character);
                        character.Hitted(stage, _owner, _objectDamage, _objectMovingDirection, transform.position, hitSoundPrefabPath: string.Empty);
                    }
                });

            if (!IsAlive)
            {
                return;
            }

            this.MoveToCurrentPosition(deltaTime);
            this.Rotate(deltaTime);
            this.SplitToRect();

            if (_splitAt <= Time.time)
            {
                for(int i = 0; i < ReflectionObjectAmount; i++)
                {
                    Vector2 dir = Quaternion.Euler(0, 0, Random.Range(-45f, 45f)) * _splitDirection;
                    FireReflectionObject(stage, this.transform.position, dir);
                }
                _isSplited = true;
            }
        }

        public override void PuttingBackToPool()
        {
            _hittedCharacters.Clear();
            base.PuttingBackToPool();
        }

        private void MoveToCurrentPosition(float deltaTime)
        {
            Vector2 currentPosition = this.transform.position;
            Vector2 nextPosition = currentPosition + _objectMovingDirection * SplitProjectileSpeed * deltaTime;
            this.transform.position = nextPosition;
        }

        private void Rotate(float deltaTime)
        {
            _bodyImage.transform.Rotate(0.0f, 0.0f, deltaTime * SplitProjectileRotateSpeed);
        }

        /// <summary>
        /// 벽에 부딪히는 경우 분열 투사체를 생성해준다.
        /// </summary>
        private void SplitToRect()
        {
            Vector2 movePosition = this.transform.position;
            bool split = false;
            if (movePosition.x < _moveRect.xMin)
            {
                movePosition.x = _moveRect.xMin;
                _splitDirection = Vector2.Reflect(_objectMovingDirection, Vector2.right);
                split = true;

            }
            else if (movePosition.x > _moveRect.xMax)
            {
                movePosition.x = _moveRect.xMax;
                _splitDirection = Vector2.Reflect(_objectMovingDirection, Vector2.left);
                split = true;
            }
            else if (movePosition.y > _moveRect.yMax)
            {
                movePosition.y = _moveRect.yMax;
                _splitDirection = Vector2.Reflect(_objectMovingDirection, Vector2.down);
                split = true;
            }
            else if (movePosition.y < _moveRect.yMin)
            {
                movePosition.y = _moveRect.yMin;
                _splitDirection = Vector2.Reflect(_objectMovingDirection, Vector2.up);
                split = true;
            }
            this.transform.position = movePosition;

            if (split)
            {
                _splitAt = Time.time;
            }
        }


        private static readonly int ReflectionObjectAmount = 2;
        private static readonly float ReflectionObjectDamageRatio = 1.0f;
        private static readonly float ReflectionObjectSpeed = 8;
        private static readonly float ReflectionObjectLifeTime = 8f;
        private static readonly float ReflectionObjectRadius = 1.0f;
        private static readonly float ReflectionObjectKnobackPower = 0.1f;
        private static readonly float ReflectionObjectRotateSpeed = 720f;
        private static readonly string ReflectionObjectBodyPath = "Stages/AreaEffects/SengokuNinjaAttack/Shuriken1_0.prefab";

        private void FireReflectionObject(Stage stage, Vector2 firePos, Vector2 fireDirection)
        {
            var rect = stage.FenceRect != null ? stage.FenceRect.Value : stage.StaticData.GetWalkableArea();
            stage.CreateReflectionAreaEffectObject(
                _owner,
                ReflectionObjectRadius,
                firePos,
                fireDirection,
                ReflectionObjectSpeed,
                ReflectionObjectRotateSpeed,
                ReflectionObjectDamageRatio * _owner.SpecialAttackPower,
                ReflectionObjectKnobackPower,
                ReflectionObjectLifeTime,
                rect,
                ReflectionObjectBodyPath
                );
        }
    }
}
