using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.GameClients.Stages.ItemObjects;
using SamMul.ObjectPools;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.ProjectileObjects
{
    public enum ProjectileType
    {
        BasicProjectile,
        CurveProjectile,
        WaveProjectile,
    }

    // Delegate
    public delegate bool OnHitCharacterHandler(Stage stage, Character target, Vector2 hitPosition, ProjectileObject attackerProjectile);
    public delegate bool OnHitItemObjectHandler(Stage stage, Collider2D collidedCharacter, ProjectileObject attackerProjectile);
    public delegate void OnFinishedHandler(Stage stage, ProjectileObject finishedProjectile, bool isHit);

    /// <summary>
    /// 발사체를 구현하는 기본 클래스
    /// 
    /// 장판과 다른 체계를 가져간다.
    /// </summary>
    public class ProjectileObject : MonoBehaviour, IPoolible<string>
    {
        private Character _owner;

        public Character Owner => _owner;

        /// <summary>
        /// 발사체 오브젝트가 유효한지 여부. <c>false</c>가 되면, 스테이지에서 제거된다.
        /// </summary>
        public bool IsAlive => (_leftAliveDistance > 0f) && (_leftHitChances > 0);

        public bool IsReserveToRemove => _isReserveToRemove;
        private bool _isReserveToRemove;

        // 몸체가 될 이미지. 프리팹으로 구워서 쓴다.
        private ProjectileBodyBase _projectileBody;
        public ProjectileBodyBase ProjectileBody => _projectileBody;

        private AllianceType _alliance;
        // 프로젝타일이 입힐 기본 대미지
        private float _baseDamage;
        private float _knockBackPower;
        protected Vector2 _direction;
        public  Vector2 Direction => _direction;
        protected float _speed;
        public float Speed => _speed;

        public float _acceleration;

        // NOTE: collidingRadius로 충돌처리도 해야 함.
        public float CollidingRadius => _collidingRadius;
        private float _collidingRadius;

        // 발사체가 앞으로 더 이동 가능한 거리. 0 이하면 곧 삭제됨
        private float _leftAliveDistance;
        private float _aliveDistance;
        // 발사체가 앞으로 피격 가능한 횟수. 0 이하면 곧 삭제 됨
        // 경우에 따라 피격은 하지만 <see cref="_leftHitChance"/>를 차감하지 않을 때도 있다. 관통 옵션 등에 의해.
        private int _leftHitChances;
        // 타격했을 때, 현재 프로젝타일과 속성이 동일한 새로운 프로젝타일을 생성합니다. 새롭게 생성된 프로젝타일은 splitCount가 0이 됩니다.(연쇄 분쇄 되지 않음)
        // 기본값은 0. (타격시 없어짐)
        private int _splitCount;
        private float FadeDuration => 0.2f;

        public AllianceType Alliance => _alliance;

        // 이 프로젝타일에 hit처리된 캐릭터들. 중복해서 hit되지 않게 처리하기 위함.
        private HashSet<Character> _hittedCharacters;

        //이 프로젝타일에 hit처리된 오브젝트들, 중복해서 hit되지 않게 처리하기 위함.
        private HashSet<Collider2D> _hittedColliders;

        private string _bodyProjectileResourcePath;

        private OnHitCharacterHandler _onHitCharacterHandler;
        private OnHitItemObjectHandler _onHitStageObjectHandler;
        private OnFinishedHandler _onFinishedHandler;

        public bool IsRemovableBySpinBladeObject => _isRemovableBySpinBladeObject;
        private bool _isRemovableBySpinBladeObject;

        // Curve
        private Vector3 _nextDirction;
        private Vector2 _targetPosition;
        private float _rotateSpeed;
        private float _rotateAcceleration;
        private ProjectileType _projectileType;

        // Wave
        private string _hitSoundPrefabPath;
        private float _waveFrequency;
        private float _waveAmplitude;

        private struct CurvePhase
        {
            public readonly float Speed;
            public readonly float Acceleration;
            public readonly float ConditionLessAngle;
            public CurvePhase(float conditionLessAngle, float speed, float acceleration)
            {
                ConditionLessAngle = conditionLessAngle;
                Speed = speed;
                Acceleration = acceleration;
            }
        }
        private Queue<CurvePhase> _curvePhases;

        public struct ProjectileRecurringActionParameter
        {
            public Stage stage;
            public ProjectileObject projectile;

            public ProjectileRecurringActionParameter(Stage stage, ProjectileObject projectile)
            {
                this.stage = stage;
                this.projectile = projectile;
            }
        }
        private RecurringActionManager<ProjectileRecurringActionParameter> _recurringActionManager;

        public void AllocateSharedResources(string bodyProjectileResourcePath)
        {
            _bodyProjectileResourcePath = bodyProjectileResourcePath;

            this.gameObject.layer = LayerMask.NameToLayer("Projectile");

            GameObject body = ResourcePool.Instance.InstantiateFromResource(bodyProjectileResourcePath);
            body.transform.SetParent(this.gameObject.transform);
            _projectileBody = ProjectileBodyBase.AllocateProjectileBody(body);

            _hittedCharacters = new HashSet<Character>();
            _hittedColliders = new HashSet<Collider2D>();
            _curvePhases = new Queue<CurvePhase>();
            _recurringActionManager = new RecurringActionManager<ProjectileRecurringActionParameter>();
        }

        public void InitializeProjectile(
            AllianceType alliance,
            Character owner,
            float baseDamage,
            float knockBackPower,
            Vector2 direction,
            float speed,
            float acceleration,
            float collidingRadius,
            float aliveDistance,
            int hitChances,
            int splitCount,
            string hitSoundPrefabPath
            )
        {
            InitializeProjectile(alliance, owner, baseDamage, knockBackPower, direction, speed, acceleration, collidingRadius, aliveDistance, hitChances, splitCount,
                true, this.TryHitCharacter, this.TryHitObject, null, hitSoundPrefabPath);
        }

        public void InitializeProjectile(
            AllianceType alliance, 
            Character owner,
            float baseDamage,
            float knockBackPower,
            Vector2 direction,
            float speed,
            float acceleration,
            float collidingRadius,
            float aliveDistance,
            int hitChances,
            int splitCount,
            bool isRemovableBySpinBladeObject,
            string hitSoundPrefabPath
            )
        {
            InitializeProjectile(alliance, owner, baseDamage, knockBackPower, direction, speed, acceleration, collidingRadius,aliveDistance, hitChances, splitCount,
                isRemovableBySpinBladeObject, this.TryHitCharacter, this.TryHitObject, null, hitSoundPrefabPath);
        }

        /// <param name="onFinishedHandler">프로젝타일이 소멸될 때 1회 호출된다</param>
        public void InitializeProjectile(
            AllianceType alliance,
            Character owner,
            float baseDamage,
            float knockBackPower,
            Vector2 direction,
            float speed,
            float acceleration,
            float collidingRadius,
            float aliveDistance,
            int hitChances,
            int splitCount,
            bool isRemovableBySpinBladeObject,
            OnHitCharacterHandler onHitCharacterHandler,
            OnHitItemObjectHandler onHitStageObjectHandler,
            OnFinishedHandler onFinishedHandler,
            string hitSoundPrefabPath
            )
        {
            _owner = owner;
            _alliance = alliance;
            _baseDamage = baseDamage;
            _knockBackPower = knockBackPower;
            _direction = direction.normalized;
            _speed = speed;
            _acceleration = acceleration;
            _collidingRadius = collidingRadius;
            _leftAliveDistance = aliveDistance;
            _aliveDistance = aliveDistance;
            _leftHitChances = hitChances;
            _splitCount = splitCount;
            _projectileBody.Initialize();
            _projectileBody.SetImageRight(_direction);

            _isRemovableBySpinBladeObject = isRemovableBySpinBladeObject;
            _projectileType = ProjectileType.BasicProjectile;
            _onHitCharacterHandler = onHitCharacterHandler;
            _onHitStageObjectHandler = onHitStageObjectHandler;
            _onFinishedHandler = onFinishedHandler;
            _hitSoundPrefabPath = hitSoundPrefabPath;

            _isReserveToRemove = false;
        }

        public void InitializeCurveProjectile(
            AllianceType alliance,
            Character owner,
            float baseDamage,
            float knockBackPower,
            Vector2 startDirection,
            Vector2 targetPosition,
            float moveSpeed,
            float rotateSpeed,
            float acceleration,
            float rotateAcceleration,
            float collidingRadius,
            float aliveDistance,
            int hitChances,
            int splitCount,
            bool isRemovableBySpinBladeObject,
            OnHitCharacterHandler hitCharacterHandler,
            OnHitItemObjectHandler hitItemHandler,
            OnFinishedHandler onFinishedHandler,
            string hitSoundPrefabPath
            )
        {
            this.InitializeProjectile(
                alliance, owner, baseDamage, knockBackPower, 
                startDirection, moveSpeed, acceleration, collidingRadius, aliveDistance, hitChances, splitCount,
                isRemovableBySpinBladeObject, hitCharacterHandler, hitItemHandler, onFinishedHandler, hitSoundPrefabPath);

            _targetPosition = targetPosition;
            _rotateSpeed = rotateSpeed;
            _nextDirction = startDirection;
            _projectileType = ProjectileType.CurveProjectile;
            _rotateAcceleration = rotateAcceleration;
            _curvePhases.Clear();
        }

        /// <param name="onFinishedHandler">프로젝타일이 소멸될 때 1회 호출된다</param>
        public void InitializeWaveProjectile(
            AllianceType alliance,
            Character owner,
            float baseDamage,
            float knockBackPower,
            Vector2 direction,
            float speed,
            float acceleration,
            float collidingRadius,
            float aliveDistance,
            int hitChances,
            int splitCount,
            float waveFrequency,
            float waveAmplitude,
            bool isRemovableBySpinBladeObject,
            OnHitCharacterHandler onHitCharacterHandler,
            OnHitItemObjectHandler onHitStageObjectHandler,
            OnFinishedHandler onFinishedHandler,
            string hitSoundPrefabPath
            )
        {
            _owner = owner;
            _alliance = alliance;
            _baseDamage = baseDamage;
            _knockBackPower = knockBackPower;
            _direction = direction.normalized;
            _speed = speed;
            _acceleration = acceleration;
            _collidingRadius = collidingRadius;
            _leftAliveDistance = aliveDistance;
            _aliveDistance = aliveDistance;
            _leftHitChances = hitChances;
            _splitCount = splitCount;
            _waveFrequency = waveFrequency;
            _waveAmplitude = waveAmplitude;

            _projectileBody.Initialize();
            _projectileBody.SetImageRight(_direction);

            _isRemovableBySpinBladeObject = isRemovableBySpinBladeObject;

            _onHitCharacterHandler = onHitCharacterHandler;
            _onHitStageObjectHandler = onHitStageObjectHandler;
            _onFinishedHandler = onFinishedHandler;
            _hitSoundPrefabPath = hitSoundPrefabPath;

            _projectileType = ProjectileType.WaveProjectile;
            
            _isReserveToRemove = false;
        }

        public void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;
            _projectileBody.UpdateLogic(deltaTime);
            _recurringActionManager.Update(now, new ProjectileRecurringActionParameter(stage, this));
            var (moveVector, moveDistance) = this.CalculateMoveVectorWithoutCollision(deltaTime);

            var isCollided = this.HandleCollisions(stage, moveVector, moveDistance);

            // NOTE: 가속도 문제 때문에 매번 체크합니다..
            float remainTime = _leftAliveDistance / _speed;
            if (remainTime <= FadeDuration)
            {
                UpdateFadeOut(remainTime);
            }

            if (!isCollided)
            {
                this.Move(stage, moveVector, moveDistance);
                this.Rotate(moveVector);
                return;
            }
        }

        private void UpdateFadeOut(float remainTime)
        {
            float ratio = (FadeDuration - remainTime)/ FadeDuration;
            float alpha = Mathf.Lerp(1.0f, 0.0f, ratio);
            _projectileBody.Fade(alpha);
        }

        public bool TryHitCharacter(Stage stage, Character target, Vector2 hitPosition, ProjectileObject attackerProjectile)
        {
            var hitVector = _direction * _knockBackPower;
            target.Hitted(stage, attacker: _owner, _baseDamage, hitVector, hitPosition, _hitSoundPrefabPath);
            return true;
        }

        public void ForceRemoveAllHitChances(Stage stage)
        {
            _leftHitChances = 0;
        }

        private (Vector2 moveVector, float moveDistance) CalculateMoveVectorWithoutCollision(float deltaTime)
        {
            return _projectileType switch
            {
                ProjectileType.CurveProjectile => CalculateCurveProjectileMoveVectorWithoutCollision(deltaTime),
                ProjectileType.WaveProjectile => CalculateWaveProjectileMoveVectorWithoutCollision(deltaTime),
                _ => CalculateBaseProjectileMoveVectorWithoutCollision(deltaTime)
            };
        }

        private (Vector2 moveVector, float moveDistance) CalculateBaseProjectileMoveVectorWithoutCollision(float deltaTime)
        {
            _speed += (_acceleration * deltaTime);
            float moveDistance = deltaTime * _speed;
            var moveVector = _direction * moveDistance;
            // 이후 로직에서 계산을 줄이기 위해, 이동 거리도 같이 반환해준다. Vector 길이 계산 다시 하지 않도록.
            return (moveVector, moveDistance);
        }

        private (Vector2 moveVector, float moveDistance) CalculateWaveProjectileMoveVectorWithoutCollision(float deltaTime)
        {
            _speed += (_acceleration * deltaTime);
            float moveDistance = deltaTime * _speed;

            //기본 직진 이동 벡터
            var moveVector = _direction * moveDistance;

            //좌우 흔들림 적용
            float frequency = 360f / _waveFrequency;  // 진동 간격(_waveFrequency 값이 클수록 진동 간격이 느려짐)
            float amplitude = _waveAmplitude;  // 흔들림 크기 (값이 클수록 더 크게 흔들림)

            // 방향 벡터의 수직 벡터 구하기 (왼쪽 또는 오른쪽 방향)
            Vector2 perpendicular = new Vector2(-_direction.y, _direction.x);

            // 현재 프레임의 흔들림 위치
            float previousWaveOffset = Mathf.Sin((_aliveDistance - _leftAliveDistance ) * frequency * Mathf.Deg2Rad) * amplitude;
            Vector2 previousWaveVector = perpendicular * previousWaveOffset;

            // 다음 프레임의 흔들림 위치
            float currentWaveOffset = Mathf.Sin((_aliveDistance - _leftAliveDistance + moveDistance) * frequency * Mathf.Deg2Rad) * amplitude;
            Vector2 currentWaveVector = perpendicular * currentWaveOffset;

            // 이전과 현재 위치의 차이 벡터 적용 (변화량)
            Vector2 waveVector = currentWaveVector - previousWaveVector;

            //최종 이동 벡터 = 직진 이동 + 좌우 흔들림
            //이동 거리는 직진 거리에 대한 이동 거리만 전달한다.
            return (moveVector + waveVector, moveDistance);
        }

        private (Vector2 moveVector, float moveDistance) CalculateCurveProjectileMoveVectorWithoutCollision(float deltaTime)
        {
            _speed += (_acceleration * deltaTime);
            float moveDistance = deltaTime * _speed;
            _direction = new Vector2(_nextDirction.x, _nextDirction.y);
            Vector2 moveVector = _direction * moveDistance;

            _rotateSpeed += (_rotateAcceleration * deltaTime);
            Vector3 targetPosition = _targetPosition;
            Vector3 targetDir = targetPosition - transform.position;
            targetDir.Normalize();

            Vector3 cross = Vector3.Cross(targetDir, _nextDirction);
            if (cross.sqrMagnitude == 0.0f)
            {
                cross = Vector3.back;
            }

            float diffAngle = Vector3.Angle(targetDir, _nextDirction);
            if (_curvePhases.Count != 0)
            {
                CurvePhase phase = _curvePhases.Peek();
                if (diffAngle < phase.ConditionLessAngle)
                {
                    _speed = phase.Speed;
                    _acceleration = phase.Acceleration;
                    _curvePhases.Dequeue();
                }
            }

            if (_rotateSpeed != 0.0f)
            {
                float nextAngle = _rotateSpeed * Time.deltaTime;
                if (diffAngle < nextAngle)
                {
                    nextAngle = diffAngle;
                    _rotateSpeed = 0.0f;
                    _rotateAcceleration = 0.0f;
                }
                _nextDirction = Quaternion.AngleAxis(-nextAngle, cross) * _nextDirction;
            }
            return (moveVector, moveDistance);
        }

        private bool HandleCollisions(Stage stage, Vector2 moveVector, float moveDistance)
        {
            // 총 두가지 충돌을 검사한다.
            // 1. 현재 위치에서 collidingRadius 기준으로 범위 충돌을 검사
            // 2. 진행방향으로 이동하면서 직선으로의 raycast 충돌을 검사
            // 이동하는 경로를 Capsule 형태로 모두 검사하지는 않는다. 
            // NOTE: Capsule 형태로 Physics2D 쿼리해서 처리하기.

            bool isCollided = false;

            var projectileLayer = LayerMask.NameToLayer("Projectile");
            int collidibleLayers = Physics2D.GetLayerCollisionMask(projectileLayer);

            var radiusBasedCollisionInfos = Physics2D.OverlapCircleAll(this.transform.position, _collidingRadius, collidibleLayers);

            var directionBasedCollisionInfos = Physics2D.RaycastAll(
                this.gameObject.transform.position,
                _direction,
                moveDistance,
                collidibleLayers);

            foreach (var collider in radiusBasedCollisionInfos)
            {
                if (!this.IsAlive)
                {
                    break;
                }

                if (collider.isTrigger)
                {
                    continue;
                }

                var otherCharacter = collider.GetComponent<Character>();
                if(collider.gameObject.layer == LayerMask.NameToLayer("CharacterHitBox"))
                {
                    otherCharacter = collider.transform.parent.GetComponent<Character>();
                }

                if (otherCharacter == null)
                {
                    if(_hittedColliders.Contains(collider))
                    {
                        continue;
                    }
                    if (this._onHitStageObjectHandler != null && this._onHitStageObjectHandler(stage, collider, this))
                    {
                        isCollided = true;
                        --_leftHitChances;
                        _hittedColliders.Add(collider);
                    }
                    continue;
                }

                if (otherCharacter.Alliance == _alliance)
                {
                    continue;
                }

                if (_hittedCharacters.Contains(otherCharacter))
                {
                    continue;
                }

                Vector2 thisPosition = this.transform.position;
                Vector2 hitPosition = thisPosition + (0.75f * (otherCharacter.CenterPos - thisPosition));
                if (this._onHitCharacterHandler != null && _onHitCharacterHandler(stage, otherCharacter, hitPosition, this))
                {
                    isCollided = true;
                    _hittedCharacters.Add(otherCharacter);

                    if (_splitCount > 0)
                    {
                        var bodyProjectilePath = ((IPoolible<string>)this).PoolKey;

                        var crossDirection = 0.75f * (Vector2)Vector3.Cross(new Vector3(_direction.x, _direction.y, 0f), new Vector3(0f, 0f, 1f));

                        for (int i = 0; i < _splitCount; ++i)
                        {
                            var directionOffset = (i == 0) ? 
                                new Vector2(0f, 0f) :
                                crossDirection * ((i + 1)/ 2);
                            
                            if (i % 2 == 0)
                            {
                                directionOffset *= -1;
                            }

                            stage.CreateProjectile(
                                bodyProjectilePath,
                                _alliance,
                                _owner,
                                _baseDamage,
                                _knockBackPower,
                                hitPosition,
                                _direction + directionOffset,
                                _speed,
                                _acceleration,
                                _collidingRadius,
                                _leftAliveDistance + 5f,
                                _leftHitChances,
                                splitCount: 0,
                                _hitSoundPrefabPath);
                        }
                        // 탄환이 분쇄되고나면, 바로 없어진다.
                        _leftHitChances = 0;
                        _leftAliveDistance = 0f;
                        return isCollided;
                    }

                    --_leftHitChances;
                    continue;
                }
            }

            foreach (var collisionInfo in directionBasedCollisionInfos.OrderBy(x=>x.distance))
            {
                if (!this.IsAlive)
                {
                    break;
                }

                var collider = collisionInfo.collider;
                if (collider.isTrigger)
                {
                    continue;
                }

                var otherCharacter = collider.GetComponent<Character>();
                if (collider.gameObject.layer == LayerMask.NameToLayer("CharacterHitBox"))
                {
                    otherCharacter = collider.transform.parent.GetComponent<Character>();
                }

                if (otherCharacter == null)
                {
                    if(_hittedColliders.Contains(collider))
                    {
                        continue;
                    }
                    if (this._onHitStageObjectHandler != null && this._onHitStageObjectHandler(stage, collider, this))
                    {
                        isCollided = true;
                        --_leftHitChances;
                        _hittedColliders.Add(collider);
                    }
                    continue;
                }
                
                if (otherCharacter.Alliance == _alliance)
                {
                    continue;
                }

                if (_hittedCharacters.Contains(otherCharacter))
                {
                    continue;
                }

                if(this._onHitCharacterHandler != null && _onHitCharacterHandler(stage, otherCharacter, collisionInfo.point, this))
                {
                    isCollided = true;
                    _hittedCharacters.Add(otherCharacter);
                    --_leftHitChances;
                    continue;
                }
            }

            return isCollided;
        }

        public bool TryHitObject(Stage stage, Collider2D collider, ProjectileObject attackerProjectile)
        {
            var breakableItemObject = collider.GetComponent<BreakableItemObject>();
            if (breakableItemObject != null)
            {
                if (_alliance == AllianceType.Players)
                {
                    // 플레이어의 프로젝타일은 부술수있는 아이템을 부술 수 있다.
                    // 몬스터의 프로젝타일은 못부숨
                    breakableItemObject.OnBroken(_owner, _baseDamage, stage);
                    return true;
                }
                return false;
            }

            // TODO : 또 다른 충돌가능한 오브젝트 대응
            return false;
        }

        private void Move(Stage stage, Vector2 moveVector, float distance)
        {
            this.transform.position += (Vector3)moveVector;

            _leftAliveDistance -= distance;
        }

        private void Rotate(Vector2 moveVector)
        {
            if(_projectileType != ProjectileType.WaveProjectile)
            {
                return;
            }

            _projectileBody.SetImageRight(moveVector);
        }

        /// <summary>
        /// 이미지의 방향을 설정하기 위한 함수.
        /// _bodyImage.transform.right 를 설정한다.
        /// </summary>
        /// <param name="right"></param>
        public void SetImageRight(Vector3 right)
        {
            _projectileBody.SetImageRight(right);
        }

        public void CallFinishedHander(Stage stage)
        {
            // NOTE: 길이가 0보다 크면 hit가 안되서 사라지는것으로 처리한다.
            bool isHit = 0 < _leftAliveDistance;

            if (_onFinishedHandler != null)
            {
                // FinishedHandler는 단 한번만 호출되도록 한다. 오류가 있어도 반복호출되지 않도록
                var finishedHandler = _onFinishedHandler;
                _onFinishedHandler = null;
                finishedHandler.Invoke(stage, this, isHit);
            }
        }

        public float AddOneOffAction(float beginAt, Action<ProjectileRecurringActionParameter> recurringAction)
            => _recurringActionManager.AddAction(beginAt, 0.0f, 0.0f, recurringAction);

        public float AddDurationalAction(float beginAt, float duration, Action<ProjectileRecurringActionParameter> recurringAction)
            => _recurringActionManager.AddAction(beginAt, duration, 0.0f, recurringAction);

        public float AddIntervalDurationalAction(float beginAt, float duration, float interval, Action<ProjectileRecurringActionParameter> recurringAction)
            => _recurringActionManager.AddAction(beginAt, duration, interval, recurringAction);

        public void ReserveToRemove()
        {
            _isReserveToRemove = true;
        }

        #region IPoolible interface
        Scene IPoolible<string>.RelatedScene => this.gameObject.scene;
        string IPoolible<string>.PoolKey => _bodyProjectileResourcePath;

        void IPoolible<string>.PuttingBackToPool()
        {
            _owner = null;
            _hittedCharacters.Clear();
            _hittedColliders.Clear();
            _projectileBody.PuttingBackToPool();
            _recurringActionManager.ClearAction();
            this.gameObject.SetActive(false);

            _isReserveToRemove = false;

            _onHitCharacterHandler = null;
            _onHitStageObjectHandler = null;
            _onFinishedHandler = null;
        }
        #endregion

    }
}
