using UnityEngine;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class LightningMachineHardHomingSplitSpinMoveObject : AreaEffectObjectBase
    {
        public override bool IsAlive => !_isSplited;

        private Monster _owner;
        private Transform _targetTransform;

        private GameObject _body;
        private Collider2D[] _overlappedColliders;
        private RaycastHit2D[] _raycastHits;

        private float _splitAt;
        private bool _isSplited;

        private static readonly string HomingobjectBodyPath = "Stages/Projectiles/LightningBallRadius1_5.prefab";
        private static readonly float HomingObjectRadius = 1.5f;
        private static readonly float HomingObjectMoveSpeed = 6f;
        private static readonly float HomingObjectDamageCoefficient = 1f;
        private static readonly float HomingObjectLifeTime = 12f;
        private static readonly float HomingPower = 1.0f;

        private static readonly int SplitSpinMoveObjectAmount = 8;
        private static readonly string SpinMoveObjectBodyPath = "Stages/Projectiles/LightningBallRadius0_5.prefab";
        private static readonly float SpinMoveObjectDamageCoefficient = 1.0f;
        private static readonly float AreaEffectCreateDistance = 1f;
        private static readonly float AttackRadius = 1f;
        private static readonly float AreaEffectLifeTime = 15f;
        private static readonly float RadiusUpSpeed = 3.0f;
        private static readonly float AngleUpSpeed = 45f;
        private static readonly float RadiusUpAcceleration = 1.0f;
        private static readonly float AngleUpAcceleration = 1.0f;
        private static readonly float BodyRotationSpeed = 0f;
        private static readonly float MaxRadius = 6f;
        private static readonly float MaxRadiusUpSpeed = 9999f;
        private static readonly float MaxAngleUpSpeed = 9999f;

        private static float _objectDamage;
        private Vector2 _movingDirection;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.LightningMachineHardHomingSplitSpinMoveObject);

            _body = ResourcePool.Instance.InstantiateFromResource(HomingobjectBodyPath);
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;

            _overlappedColliders = new Collider2D[16];
            _raycastHits = new RaycastHit2D[16];
        }

        public void Initialize(
            Monster owner,
            Transform targetTransform,
            Vector2 startPosition,
            Vector2 startDirection)
        {
            base.InitializeAreaObject(owner.Alliance);
            float now = Time.time;

            _owner = owner;
            _targetTransform = targetTransform;

            _objectDamage = _owner.SpecialAttackPower * HomingObjectDamageCoefficient;
            _splitAt = now + HomingObjectLifeTime;

            _isSplited = false;
            this.transform.position = startPosition;
            _movingDirection = startDirection;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            CombatSystem.HandleAreaEffectCollisionWithCharacter(
                areaEffectObject: this,
                position: transform.position,
                radius: HomingObjectRadius,
                direction: _movingDirection,
                speed: HomingObjectMoveSpeed,
                deltaTime: deltaTime,
                overlappedColliders: _overlappedColliders,
                raycastHits: _raycastHits,
                onHitCharacter: (character) =>
                {
                    character.Hitted(stage, _owner, _objectDamage, _movingDirection, this.transform.position, hitSoundPrefabPath: string.Empty);
                    this.CreateSplitProjectiles(stage);
                    _isSplited = true;
                });

            if (!IsAlive)
            {
                return;
            }

            this.RotateMoveDirectionToHomingDirection(deltaTime);
            this.RotateBodyImageToMoveDirection();
            this.MoveToCurrentPosition(deltaTime);


            if (_splitAt <= Time.time)
            {
                this.CreateSplitProjectiles(stage);
                _isSplited = true;
            }
        }

        private void RotateMoveDirectionToHomingDirection(float deltaTime)
        {
            Vector2 targetDirection = _targetTransform.position - this.transform.position;
            targetDirection.Normalize();
            _movingDirection = Vector2.Lerp(_movingDirection, targetDirection, HomingPower * deltaTime).normalized;

        }

        // 몸체 이미지 리소스 회전 코드
        private void RotateBodyImageToMoveDirection()
        {

            Vector2 direction = _movingDirection * HomingObjectMoveSpeed;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _body.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        // 현재 포지션 적용 (이동) 코드
        private void MoveToCurrentPosition(float deltaTime)
        {
            Vector2 currentPosition = this.transform.position;
            Vector2 nextPosition = currentPosition + _movingDirection * deltaTime * HomingObjectMoveSpeed;
            this.transform.position = nextPosition;
        }

        private void CreateSplitProjectiles(Stage stage)
        {
            for (int i = 0; i < SplitSpinMoveObjectAmount; i++)
            {
                float startAngle = 360f / SplitSpinMoveObjectAmount * i;

                this.CreateSpinMoveObject(stage, this.transform.position, startAngle, offsetPosition: Vector2.zero, isLeftRotate: false);
            }
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
        }

        private void CreateSpinMoveObject(Stage stage, Vector2 firePos, float startAngle, Vector2 offsetPosition, bool isLeftRotate)
        {
            float angleDirection = isLeftRotate ? 1 : -1;
            stage.CreateSpinMoveObject(
                _owner,
                _owner.SpecialAttackPower * SpinMoveObjectDamageCoefficient,
                firePos,
                SpinMoveObjectBodyPath,
                AreaEffectLifeTime,
                AreaEffectCreateDistance * angleDirection,
                startAngle * angleDirection,
                RadiusUpSpeed * angleDirection,
                AngleUpSpeed * angleDirection,
                RadiusUpAcceleration * angleDirection,
                AngleUpAcceleration * angleDirection,
                AttackRadius,
                BodyRotationSpeed,
                offsetPosition,
                MaxRadius,
                MaxRadiusUpSpeed,
                MaxAngleUpSpeed);
        }
    }
}