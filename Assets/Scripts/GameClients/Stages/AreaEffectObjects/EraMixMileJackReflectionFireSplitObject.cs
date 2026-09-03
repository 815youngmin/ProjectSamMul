using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class EraMixMileJackReflectionFireSplitObject : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + ReflectionObjectLifeTime;

        private Monster _owner;
        private Vector2 _movingDirection;
        private float _createdAt;

        private Rect _moveRect;

        private GameObject _bodyImage;


        private static readonly float DAMAGE_COEFFICIENT = 1.0f;
        private static readonly float ReflectionObjectSpeed = 6;
        private static readonly float ReflectionObjectLifeTime = 12f;
        private static readonly float ReflectionObjectRadius = 0.8f;
        private static readonly float ReflectionObjectRotateSpeed = 0f;
        private static readonly string ReflectionObjectBodyPath = "Stages/Projectiles/SoundRedImpulseRadius0_9.prefab";
        private static readonly float DamagePeriod = 0.25f;
        private HashSet<Character> _hittedCharacters;
        private float _hittedCharacterClearAt;

        private static readonly float ProjectileFireDelay = 1;
        private static readonly float PROJECTILE_DAMAGE_COEFFICIENT = 1.0f;
        private static readonly string ProjectileBodyPath = "Stages/Projectiles/SoundRedImpulseSmallRadius0_45.prefab";
        private static readonly float ProjectileSpeed = 12f;
        private static readonly float ProjectileAcceleration = 1f;
        private static readonly float ProjectileRadius = 0.45f;
        private static readonly float ProjectileAliveDistance = 37;
        private static readonly float ProjectileKnobackPower = 0.1f;
        private static readonly bool IsSpinBladeCollide = true;
        private float _projectileFireAt;


        //Initialize 단계에서 이미지 경로 받아와서 처리해야되는 경우가 생겨 아래 함수 추가 
        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.EraMixMileJackReflectionFireSplitObject);

            _bodyImage = ResourcePool.Instance.InstantiateFromResource(ReflectionObjectBodyPath);
            _bodyImage.transform.SetParent(this.gameObject.transform);
            _bodyImage.transform.localPosition = Vector3.zero;

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
            _createdAt = Time.time;
            this.transform.position = startPosition;

            float angle = Mathf.Rad2Deg * Mathf.Atan2(_movingDirection.y, _movingDirection.x);
            _bodyImage.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            float now = Time.time;

            _hittedCharacters.Clear();
            _hittedCharacterClearAt = now;

            _projectileFireAt = now;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;

            if (_hittedCharacterClearAt + DamagePeriod <= now)
            {
                _hittedCharacterClearAt = now;
                _hittedCharacters.Clear();
            }

            var targetArea = new CircularTargetArea(this.transform.position, ReflectionObjectRadius);
            CombatSystem.HitOnTargetArea(stage, targetArea, _owner, _owner.SpecialAttackPower * DAMAGE_COEFFICIENT, CombatSystem.KnockBackType.Pivot, Vector2.zero, 0f, _hittedCharacters, _hittedCharacters, null);


            if(_projectileFireAt + ProjectileFireDelay <= now)
            {
                FireCircleShape(stage, this.transform.position);
                _projectileFireAt = now;
            }

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


            //외부에서transform 사이즈 연출을 Dotween으로 처리하고있어 해당 코드를 추가했다.
            DOTween.Kill(this.transform);
        }

        private void Move(float deltaTime)
        {
            this.transform.Translate(deltaTime * ReflectionObjectSpeed * _movingDirection);
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
                if (ReflectionObjectRotateSpeed == 0.0f)
                {
                    float angle = Mathf.Rad2Deg * Mathf.Atan2(_movingDirection.y, _movingDirection.x);
                    _bodyImage.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
                }
            }
        }

        private void Rotate(float deltaTime)
        {
            if (ReflectionObjectRotateSpeed == 0.0f)
            {
                return;
            }

            _bodyImage.transform.Rotate(0.0f, 0.0f, deltaTime * ReflectionObjectRotateSpeed);
        }




        private static readonly int FireAmount = 10;

        private void FireCircleShape(Stage stage, Vector2 firePosition)
        {
            for (int i = 0; i < FireAmount; i++)
            {
                Vector2 dir = Quaternion.Euler(0.0f, 0.0f, 360f / FireAmount * i) * Vector2.up;
                this.FireProjectile(stage, firePosition, dir);
            }
        }



        private void FireProjectile(Stage stage, Vector2 firePosition, Vector2 fireDirection)
        {
            stage.CreateProjectile(
                ProjectileBodyPath,
                _owner.Alliance,
                _owner,
                _owner.RangeAttackPower * PROJECTILE_DAMAGE_COEFFICIENT,
                ProjectileKnobackPower,
                firePosition,
                fireDirection,
                ProjectileSpeed,
                ProjectileAcceleration,
                ProjectileRadius,
                ProjectileAliveDistance,
                hitChances: 1,
                splitCount: 0,
                IsSpinBladeCollide,
                hitSoundPrefabPath: string.Empty
                );
        }




    }
}
