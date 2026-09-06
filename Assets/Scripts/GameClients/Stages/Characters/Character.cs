#nullable enable
using Shared.GameDataTypes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using SamMul.GameClients.Stages.Characters.Actions;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.CharacterEvents;
using SamMul.GameClients.Stages.Characters.GroundEffects;
using SamMul.GameClients.Stages.Characters.Shields;
using SamMul.GameClients.Stages.Characters.Stats;
using SamMul.GameClients.Stages.Characters.StatusEffects;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.Loggers;
using SamMul.ObjectPools;
using SamMul.ResourcePools;
using SamMul.UIs.Stages.HUDs;
using SamMul.UnityHelpers;

namespace SamMul.GameClients.Stages.Characters
{
    //모든 캐릭터에 사용되는 부모 클래스
    public abstract class Character : MonoBehaviour, IPoolible<CharacterType>
    {
        public CharacterType CharacterType { get; private set; }
        CharacterType IPoolible<CharacterType>.PoolKey => MakePoolKey(this.CharacterType);

        protected AllianceType _alliance;
        public AllianceType Alliance => _alliance;

        public CharacterEventHandlerManager EventHandlers => _eventHandlers;
        protected CharacterEventHandlerManager _eventHandlers;

        public CharacterActionController Action => _action;
        protected CharacterActionController _action;

        public CharacterAnimationController AnimationController => _animationController;
        protected CharacterAnimationController _animationController = null;
        public CharacterStatCalculators Stats => _stats;
        protected CharacterStatCalculators _stats;

        public StatusEffectSet StatusEffects => _statusEffects;
        protected StatusEffectSet _statusEffects;

        [SerializeField]
        private GameObject _body = null;
        public GameObject Body => _body;
        // sorting order 설정을 위해 body(SkeletonAnimation)의 MeshRenderer 컴포넌트의 참조를 가져온다.
        [SerializeField]
        protected Renderer _bodyRenderer = null;
        [SerializeField]
        private SpriteRenderer _shadow = null;
        /// <summary>
        /// 그림자 오브젝트. 
        /// 캐릭터 게임오브젝트에 차일드 게임오브젝트에 붙은 스프라이트 렌더러
        /// 바디 스파인 오브젝트와 별도로 transform을 관리하기 위해 별도 게임오브젝트에 붙어있다.
        /// </summary>
        public SpriteRenderer Shadow => _shadow;

        protected float _colliderRadius;
        protected Collider2D _characterCollider; // 이동에 관련된 충돌체, 몬스터와 부딫여서 처리되는것도 해당 충돌체에서 처리된다.
        protected BoxCollider2D _hitBoxCollider;  // 이동 외 충돌에 관련된 처리를 하려는 충돌체 (현재는 Projectile만 처리중)
        protected Rigidbody2D _rigidBody;

        protected HPBar _hpBar;

        [SerializeField]
        private Transform _uiPositionOffset;
        public Vector2 UIPositionOffset => _uiPositionOffset.localPosition;
        public Vector2 UIPos => _uiPositionOffset.position;

        [SerializeField]
        private Transform _centerPositionOffset;
        public Vector2 CenterPos => _centerPositionOffset.position;

        /// <summary>
        /// 이 플래그가 true면, 피격시 넉백 안 당함
        /// </summary>
        public bool IsImmuneToKnockBack => _isImmuneToKnockBack;
        private bool _isImmuneToKnockBack;
        /// <summary>
        /// 피격 내성, 이 플래그가 true면 피격 안 당함. 타겟으로도 지정 안 됨.
        /// (타겟으로도 지정 안 되게 구현해야 함)
        /// </summary>
        public bool IsImmuneToHit => _isImmuneToHit;
        protected bool _isImmuneToHit;

        private float _hp;
        protected float _lastDamage;

        public bool IsLastHittedCritical => _isLastHittedCritical;
        private bool _isLastHittedCritical;

        protected float _lastRangeAttackedAt { get; set; } = 0.0f;

        public float RangeAttackDuration => _stats.CharacterAttackSpeed.Value <= 0f ? 10f : (1.0f / _stats.CharacterAttackSpeed.Value);
        public abstract float RangeAttackPower { get; }

        public float CurrentHP => _hp;
        public float MaxHP => _stats.MaxHP.Value;

        public virtual Vector2 Pos => this.transform.position;

        public Vector2 MoveDir { get; protected set; } = Vector2.up;

        // 피격당했을 때, hitVector에 곱해지는 상수. hitVector를 토대로 얼만큼 힘을 줄지 결정한다.
        public float KnockBackForceConversionRate => 10f;
        public float DeadForceConversionRate => 25f;

        /// <summary>
        /// 죽거나 사라질 때 애니메이션 재생을 마치고 스테이지에서 제거될 시각.
        /// </summary>
        public float WillBeRemovedFromStageAt { get; private set; }

        // 충돌에 의한 공격시, 충돌체 반경보다 offset만큼 추가 범위를 탐색하여 공격한다. 
        public readonly static float COLLISION_ATTACK_OFFSET = 0.1f;
        public float CollisionAttackRadius => _colliderRadius + COLLISION_ATTACK_OFFSET;
        public float ColliderRadius => _colliderRadius;

        public static CharacterType MakePoolKey(CharacterType characterType) => characterType;

        private GroundCircle _groundCircle;

        // 버프 효과 등을 위해 그려줄 바닥 이펙트. 중복해서 그리지는 않는다.
        private Dictionary<CharacterGroundEffectType, CharacterGroundEffect> _groundEffects;
        private Dictionary<CharacterGroundEffectType, CharacterGroundEffect> _ephemeralGroundEffects;

        //체인 타겟이 존재하면 타겟이 대신 데미지를 대신 입습니다.
        //Hitted 효과는 맞는쪽에서 처리합니다.
        //체인 타겟이 죽으면 같이 죽습니다.
        private Character _chainTarget;

        protected bool _isBoss;
        public bool IsBoss => _isBoss;

        protected bool _isElite;
        public bool IsElite => _isElite;

        //실드가 활성화 되어 있으면 데미지를 입지 않습니다.
        public bool IsShieldActive => _shield?.IsActive ?? false;
        public Shield Shield => _shield;
        protected Shield _shield;

        /// <summary>
        /// 캐릭터 풀에서 인스턴스가 생성되는 시점에 한번만 호출된다.
        /// <paramref name="characterType"/>끼리 공유되는 리소스들을 할당한다.
        /// </summary>
        public virtual void AllocateSharedResources(
            CharacterType characterType,
            string skeletonDataPath,
            float colliderRadius,
            Vector2 hitBoxOffset,
            Vector2 hitBoxSize,
            float shadowSize)
        {
            // CharacterType 별로 공유하는 리소스를 생성한다. (오브젝트 풀링할 때 공유할 리소스)
            // TODO : collider 를 지정된 크기에 맞춰 설정하고 (width, height)
            // collider크기에 맞춰 Shadow도 지정하자.
            this.CharacterType = characterType;
            this.AllocateBodyComponents(skeletonDataPath);

            this.AllocateColliderComponent(colliderRadius, hitBoxOffset, hitBoxSize);
            this.AllocateShadowComponent(shadowSize);
            this.AllocatePositionComponents(characterType, hitBoxOffset, hitBoxSize);

            _stats = this.CreateStatCalculators();
            _statusEffects = new StatusEffectSet();

            _action = this.CreateCharacterActionController();

            _eventHandlers = new CharacterEventHandlerManager();

            _isImmuneToKnockBack = false;
            _isImmuneToHit = false;

            _groundEffects = new Dictionary<CharacterGroundEffectType, CharacterGroundEffect>();
            _ephemeralGroundEffects = new Dictionary<CharacterGroundEffectType, CharacterGroundEffect>();
        }

        protected void InitializeCharacter(AllianceType alliance, BaseStats baseStats, float mass, float drag)
        {
            _chainTarget = null;
            _isBoss = false;
            _isElite = false;

            _alliance = alliance;
            this.InitializeBaseStats(baseStats);

            this.InitializeHp();

            _rigidBody.mass = mass;
            _rigidBody.drag = drag;
            _rigidBody.angularDrag = drag;
            _characterCollider.enabled = true;
            _hitBoxCollider.enabled = true;

            _isImmuneToKnockBack = false;
            _isImmuneToHit = false;

            _animationController.SetToInitialState();

            this.InitializeEffects();

            WillBeRemovedFromStageAt = 0.0f;

            this.ClearGroundEffects();

            this.ResetAttackDelay();
        }

        public virtual void InitializeHp()
        {
            _hp = _stats.MaxHP.Value;
        }

        public virtual void InitializeEffects()
        {
            this.ShowShadow(true);
        }

        private void AllocateColliderComponent(float colliderRadius, Vector2 hitBoxOffset, Vector2 hitBoxSize)
        {
            var collider = this.gameObject.AddComponent<CircleCollider2D>();
            collider.offset = new Vector2(0f, 0f);
            collider.radius = colliderRadius;
            _colliderRadius = colliderRadius;
            _characterCollider = collider;

            var rigidBody2D = this.gameObject.AddComponent<Rigidbody2D>();
            rigidBody2D.bodyType = RigidbodyType2D.Dynamic;
            rigidBody2D.mass = 1f;
            rigidBody2D.drag = 0.2f;
            rigidBody2D.angularDrag = 0.05f;
            rigidBody2D.gravityScale = 1f;
            rigidBody2D.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
            rigidBody2D.sleepMode = RigidbodySleepMode2D.StartAwake;
            rigidBody2D.interpolation = RigidbodyInterpolation2D.None;
            rigidBody2D.freezeRotation = true;
            _rigidBody = rigidBody2D;

            // Hit Box
            {
                GameObject hitBoxObject = new GameObject("HitBoxCollderObj");
                hitBoxObject.transform.SetParent(this.transform, false);
                hitBoxObject.layer = LayerMask.NameToLayer("CharacterHitBox");
                _hitBoxCollider = hitBoxObject.AddComponent<BoxCollider2D>();
                if (hitBoxSize.x == 0.0f && hitBoxSize.y == 0.0f)
                {
                    this.SetHitBoxSize(new Vector2(2 * colliderRadius, 2 * colliderRadius));
                }
                else
                {
                    this.SetHitBoxSize(hitBoxSize);
                }
                this.SetHitBoxOffset(hitBoxOffset);
                _hitBoxCollider.isTrigger = false;
            }
        }

        private void AllocateShadowComponent(float shadowSize)
        {
            var shadowObject = new GameObject("Shadow");
            shadowObject.transform.SetParent(this.gameObject.transform, worldPositionStays: false);

            var shadow = shadowObject.AddComponent<SpriteRenderer>();
            // TODO 플레이어용 그림자 쓸 것
            shadow.sprite = ResourcePool.Instance.LoadResource<Sprite>("Stage/Common/CharacterShadow.png");
            shadow.color = new Color(shadow.color.r, shadow.color.g, shadow.color.b, 1.00f);
            shadow.sortingLayerID = SortingLayer.NameToID("LowShadow");
            shadow.drawMode = SpriteDrawMode.Simple;

            float scale = shadowSize;
            shadow.transform.localScale = new Vector2(scale, scale);

            _shadow = shadow;
            this.ShowShadow(0.0f != shadowSize);
        }

        private void ClearGroundEffects()
        {
            foreach (var groundEffect in _groundEffects.Values)
            {
                groundEffect.PutResourceBackToPool();
            }

            _groundEffects.Clear();
        }

        // 제거 명령 전까지, 무한히 루프를 돌림
        public void AddInfiniteGroundEffect(CharacterGroundEffectType effectType)
        {
            // 영속 장판은, 앞에 같은게 있으면 뒤에걸 무시한다. (컨텐츠레벨에서 이런 경우가 나오도록 설계하면 안 된다)
            if (_groundEffects.ContainsKey(effectType))
            {
                Log.I.Warn($"{effectType}을 중복해서 추가하려 함. 무시합니다.");
                return;
            }

            var groundEffect = CharacterGroundEffect.CreateInfiniteLoop(effectType, this.transform);
            _groundEffects.Add(effectType, groundEffect);
        }

        public void RemoveInfiniteGroundEffect(CharacterGroundEffectType effectType)
        {
            if (!_groundEffects.Remove(effectType, out var effect))
            {
                Log.I.Warn($"{effectType}이 없는데 제거하려했습니다. 무시합니다.");
                return;
            }

            effect.PutResourceBackToPool();
        }

        private List<CharacterGroundEffect> v_ExpiredEphemeralGroundEffects = new List<CharacterGroundEffect>();
        private void RemoveExpiredEphemeralGroundEffect()
        {
            v_ExpiredEphemeralGroundEffects.Clear();
            foreach (var kvp in _ephemeralGroundEffects)
            {
                var groundEffect = kvp.Value;

                if (groundEffect.IsExpired)
                {
                    v_ExpiredEphemeralGroundEffects.Add(groundEffect);
                }
            }

            foreach (var expired in v_ExpiredEphemeralGroundEffects)
            {
                expired.PutResourceBackToPool();
                _ephemeralGroundEffects.Remove(expired.GroundEffectType);
            }
            v_ExpiredEphemeralGroundEffects.Clear();

        }

        public void ShowShadow(bool show)
        {
            _shadow.gameObject.SetActive(show);
        }

        private void AllocatePositionComponents(CharacterType characterType, Vector2 hitBoxOffset, Vector2 hitBoxSize)
        {
            var uiPositionObject = new GameObject("UIPosition");
            uiPositionObject.transform.SetParent(this.gameObject.transform);
            uiPositionObject.transform.localPosition = new Vector2(0f, hitBoxSize.y + hitBoxOffset.y);
            _uiPositionOffset = uiPositionObject.transform;

            switch (characterType)
            {
                case CharacterType.Viking_BossKraken:
                    {
                        var centerPositionObject = new GameObject("CenterPosition");
                        centerPositionObject.transform.SetParent(this.gameObject.transform);
                        centerPositionObject.transform.localPosition = new Vector2(0f, 0f);
                        _centerPositionOffset = centerPositionObject.transform;
                    }
                    break;
                case CharacterType.Crusades_EasternEmpireShip:
                    {
                        var centerPositionObject = new GameObject("CenterPosition");
                        centerPositionObject.transform.SetParent(this.gameObject.transform);
                        centerPositionObject.transform.localPosition = new Vector2(0f, 0f);
                        _centerPositionOffset = centerPositionObject.transform;
                    }
                    break;
                case CharacterType.Crusades_EasternEmpireShip_Collider:
                    {
                        var centerPositionObject = new GameObject("CenterPosition");
                        centerPositionObject.transform.SetParent(this.gameObject.transform);
                        centerPositionObject.transform.localPosition = new Vector2(0f, 0f);
                        _centerPositionOffset = centerPositionObject.transform;
                    }
                    break;
                default:
                    {
                        var centerPositionObject = new GameObject("CenterPosition");
                        centerPositionObject.transform.SetParent(this.gameObject.transform);
                        centerPositionObject.transform.localPosition = new Vector2(hitBoxOffset.x, hitBoxSize.y * 0.5f + hitBoxOffset.y);
                        _centerPositionOffset = centerPositionObject.transform;
                    }
                    break;
            }

        }

        private void AllocateBodyComponents(string bodyDataPath)
        {
            var bodyObject = new GameObject("Body");
            bodyObject.transform.SetParent(this.gameObject.transform);
            _animationController = this.CreateCharacterAnimationController(bodyObject, bodyDataPath);

            _body = bodyObject;
        }

        public virtual void OnEnterredIntoStage(Stage stage)
        {
            float appearingDuration = _animationController.AppearAnimationDuration;
            _action.ChangeTo(stage, new AppearingAction(appearingDuration, this, this._animationController));
        }

        public virtual void OnExitingFromStage(Stage stage)
        {
            this.StopMovement();
        }

        protected virtual void OnDestroy()
        {
            // 캐릭터를 풀링하고 있기 때문에, skeletonData를 풀링하는 것은 의미가 없다. 
            // Destroy하는 순간엔 이미 씬전환하는 순간으로 리소스풀에 돌아간 리소스도 결국 모두 제거될 것이다.
        }

        Scene IPoolible<CharacterType>.RelatedScene => this.gameObject.scene;
        public virtual void PuttingBackToPool()
        {
            this.HideHPBar();

            // 이러면 안되는데.... ㅈㅅㅈㅅ
            var ThisIsNotAGoodIdeaButStageReference = GameClient.Stage;
            _statusEffects?.Clear(this, ThisIsNotAGoodIdeaButStageReference);
            this.gameObject.SetActive(false);
            _animationController?.SetToInitialState();
            _eventHandlers.Clear();
        }

        protected virtual CharacterActionController CreateCharacterActionController()
        {
            return new CharacterActionController(_animationController);
        }

        protected virtual CharacterStatCalculators CreateStatCalculators()
        {
            return new CharacterStatCalculators();
        }

        public virtual void UpdateLogic(Stage stage)
        {
            var now = Time.time;
            this.UpdateDrawOrder();

            _action.Update(stage);
            _statusEffects.Update(stage, this, now);
            _animationController.Update();
            if (_groundCircle != null)
            {
                _groundCircle.RotateToMoveDirection(MoveDir);
            }
            if (IsHpBarActive)
            {
                _hpBar.UpdateLogic(CurrentHP, MaxHP, Pos);
            }

            this.RemoveExpiredEphemeralGroundEffect();
        }

        protected bool IsGroundCircleActive => _groundCircle != null;
        protected bool IsHpBarActive => _hpBar != null;

        public void SetImmuneToKnockBack()
        {
            _isImmuneToKnockBack = true;
        }

        public void UnsetImmuneToKnockBack()
        {
            _isImmuneToKnockBack = false;
        }

        public void SetImmuneToHit()
        {
            _isImmuneToHit = true;
        }

        public void UnsetImmuneToHit()
        {
            _isImmuneToHit = false;
        }

        protected abstract CharacterAnimationController CreateCharacterAnimationController(GameObject bodyObject, string bodyDataPath);

        /// <param name="moveVector">조이스틱이 움직여진 방향과 크기를 가진 벡터가 들어온다. 최대 크기는 1인 벡터.</param>
        public virtual void Move(Vector2 moveVector)
        {
            // 몬스터만 이 함수가 호출된다. virtual 함수인데 사실상 virtual 함수가 아니다. Player용은 따로 있다. 
            if (_action.IsStunned || _action.IsDead || _action.IsAppearing)
            {
                return;
            }

#if USE_SCOPED_PROFILER
            using (new ScopedProfiler("Character.Move.SetDir"))
#endif
            {
                this.MoveDir = moveVector.normalized;
            }

            if (this.MoveDir != Vector2.zero)
            {
                // 몬스터의 이동이 정확한 물리시뮬레이션으로 처리되는 것이 아니라,
                // 이속이 빠른 몬스터가 많을 경우, 밀고 지나갈 수 있게 처리해야해서, rigidBody의 힘을 사용하지 않고, 바로 transform을 사용하여 움직인다.
#if USE_SCOPED_PROFILER
                using (new ScopedProfiler("Character.Move.SetPosition"))
#endif
                {
                    Vector2 deltaMovement = Time.deltaTime * _stats.MoveSpeed.Value * this.MoveDir;
                    this.transform.position += (Vector3)deltaMovement;
                }
#if USE_SCOPED_PROFILER
                using (new ScopedProfiler("Character.Move.UpdateBodyDirectionByMoveDirection"))
#endif
                {
                    _animationController.UpdateBodyDirectionByMoveDirection(this.MoveDir);
                }
#if USE_SCOPED_PROFILER
                using (new ScopedProfiler("Character.Move.PlayMoveInfinitely"))
#endif
                {
                    _animationController.PlayMoveInfinitely(moveVector);
                }
            }
        }

        public virtual void StopMovement()
        {
            this.MoveDir = Vector2.zero;
            _rigidBody.velocity = Vector2.zero;

            if (_action.IsDead || _action.IsAppearing)
            {
                return;
            }
            _animationController.StopMovement();
        }

        // NOTE: hitVector에 무기의 특성, 공격된 힘의 차이를 반영하여 크기가 결정되도록 수정하자.
        // 지금은 hitVector가 단순히 캐릭터와 공격오브젝트의 거리차로 만들어지고 있다.
        /// <param name="hitVector">타격이 일어날 때, 공격자가 공격한 벡터. 방향과 크기를 모두 사용한다. defender vector - attacker vector</param>
        public virtual float Hitted(Stage stage, Character? attacker, float inputDamage, Vector2 hitVector, Vector2 hitPosition, string hitSoundPrefabPath)
        {
            if (_action.IsDead || _isImmuneToHit)
            {
                return 0f;
            }

            float attackerCriticalChance = attacker == null ? 0.0f : attacker.Stats.CriticalChance.Value;
            float attackerCriticalCoefficient = attacker == null ? 0.0f : attacker.Stats.CriticalCoefficient.Value;
            
            var calculateResult = CombatSystem.CalculateCharacterHittedDamage(_stats, inputDamage, attackerCriticalChance, attackerCriticalCoefficient);
            _isLastHittedCritical = calculateResult.isCritical;
            float finalDamage = calculateResult.finalDamage;

            //체인 타겟이 있으면 타겟에 데미지를 입히고 당사자는 데미지를 입지 않습니다.
            //당사자는 데미지 입는 연출만 합니다.
            if (_chainTarget != null)
            {
                //Crusades_EasternEmpireShip_Collider만 _chainTarget에게 Hitted처리를 넘기고 return합니다
                //바디 사이즈 뽀용뽀용 하는 연출이 Monster쪽에 있어서 여기서 전부 처리해주기 어려움
                if (CharacterType == CharacterType.Crusades_EasternEmpireShip_Collider)
                {
                    _chainTarget.Hitted(stage, attacker, inputDamage, hitVector, hitPosition, hitSoundPrefabPath);
                    return 0f;
                }
                else
                {
                    _chainTarget.HittedChainType(stage, finalDamage, hitVector);
                }
            }
            else
            {
                _hp -= finalDamage;
                _lastDamage = finalDamage;
            }

            this.CreateDamagePopup(stage, attacker, hitPosition, finalDamage, hitVector, true);
            if (!this.IsImmuneToKnockBack)
            {
                var knockBackResistance = _stats.KnockBackResistance.Value;
                // 넉백저항이 1보다 크면 넉백 무시 
                if (knockBackResistance < 1f &&
                    hitVector != Vector2.zero)
                {
                    var knockBackPower = (knockBackResistance < 0f) ?
                        hitVector :
                        hitVector * (1.0f - knockBackResistance);

                    this.KnockBack(knockBackPower);
                }
            }

            _animationController.PlayHitted(true);
            _eventHandlers.OnHitted(stage, this, attacker, finalDamage, hitPosition);

            UnityGlobal.Sounds.PlayBySoundPrefab(hitSoundPrefabPath, this.Pos);

            bool isDead = (_chainTarget != null) ?
                _chainTarget.Action.IsDead : // 체인타겟이 있다면 체인되어있는 캐릭터가 죽으면 당사자도 죽습니다.
                (_hp <= 0.0f); // 체인타겟 없으면 HP가 0이하면 죽습니다.

            //만약 죽었다면 그 전 스턴 상태였는지 기록하기 위해 미리 기록
            bool isStunned = _action.IsStunned;

            if (isDead)
            {
                this.Dead(stage, hitVector.normalized);
            }
            else
            {
                bool isBigCharacter = (IsBoss);// || IsElite);
                _animationController.BeginHittedBodyEffect(isBigCharacter);
            }

            // Attacker측 이벤트 핸들러 처리, Defender측 처리가 모두 완료된 뒤에 처리한다.
            if (attacker != null)
            {
                attacker.AttackedEnemy(stage, enemy: this, finalDamage);
                if (_action.IsDead)
                {
                    attacker.KilledEnemy(stage, enemy: this);
                }

                if(_action.IsDead && isStunned)
                {
                    attacker.KilledStunnedEnemy(stage, enemy: this);
                }

            }

            return finalDamage;
        }

        public virtual void AttackedEnemy(Stage stage, Character enemy, float damage)
        {
        }
        public virtual void KilledEnemy(Stage stage, Character enemy)
        {
        }

        public virtual void KilledStunnedEnemy(Stage stage, Character enemy)
        {
        }

        public void HittedChainType(Stage stage, float finalDamage, Vector2 hitVector)
        {
            _hp -= finalDamage;
            _lastDamage = finalDamage;

            if (_hp <= 0.0f)
            {
                this.Dead(stage, hitVector.normalized);
            }
        }

        public abstract void CreateDamagePopup(Stage stage, Character? attacker, Vector2 hitPosition, float finalDamage, Vector2 hitVector, bool isAttack);

        public virtual void RecoverHP(Stage stage, float increment)
        {
            if (_action.IsDead)
            {
                return;
            }

            float previousHp = _hp;

            _hp += increment;
            if (_hp > this.MaxHP)
            {
                _hp = this.MaxHP;
            }

            if (_hp - previousHp > 0)
            {
                //만약 소수점 단위의 체력이 올라갔다면 그냥 최소단위 1로 표시
                float healPopupValue = Mathf.Clamp(_hp - previousHp, 1.0f, this.MaxHP);
                stage.DamagePopups.CreateDamagePopup(null, this.CenterPos, healPopupValue, new Vector2(0.0f, 0.1f), false);
            }

            var particle = stage.Particles.CreateParticle("Stages/AreaEffects/fx_heal.prefab", this.transform);
            particle.transform.SetParent(this.transform, worldPositionStays: true);
        }

        public virtual void DrainHP(Stage stage, float increment)
        {
            if (_action.IsDead)
            {
                return;
            }

            _hp += increment;
            if (_hp > this.MaxHP)
            {
                _hp = this.MaxHP;
            }

            var particle = stage.Particles.CreateParticle("Stages/AreaEffects/fx_drainHP.prefab", this.transform);
            particle.transform.SetParent(this.transform, worldPositionStays: true);
        }

        protected void KnockBack(Vector2 hitVector)
        {
            _rigidBody.AddForce(hitVector * KnockBackForceConversionRate, ForceMode2D.Impulse);
        }

        public void KnockBack(Vector2 knockbackVector, float force)
        {
            _rigidBody.AddForce(knockbackVector * force, ForceMode2D.Impulse);
        }

        public void ForceKillSelf(Stage stage)
        {
            if (this.Action.IsDead)
            {
                return;
            }

            this.Dead(stage, Vector2.zero);
        }

        protected virtual void Dead(Stage stage, Vector2 hitVector)
        {
            _hp = 0.0f;

            if (_action.IsDead)
            {
                return;
            }

            this.StatusEffects.OwnerDead(this, stage);
            this.StatusEffects.Clear(this, stage);
            this.HideHPBar();
            this.HideGroundCircle();
            this.ClearGroundEffects();

            WillBeRemovedFromStageAt = Time.time + _animationController.DeadAnimationDuration;
            _action.ChangeTo(stage, new DeadAction(hitVector, this, _animationController));

            _eventHandlers.OnDead(stage, this, hitVector);
        }

        public virtual void DisappearFromStage(Stage stage)
        {
            if (_action.IsDead)
            {
                return;
            }

            this.StatusEffects.Clear(this, stage);
            this.HideHPBar();
            this.HideGroundCircle();
            this.ClearGroundEffects();

            WillBeRemovedFromStageAt = Time.time + _animationController.DisappearAnimationDuration;   // 사라짐은 죽음과 동등한 상태로 취급한다.
            _action.ChangeTo(stage, new DisappearingAction(this, this._animationController));
        }

        public void ResetAttackDelay()
        {
            _lastRangeAttackedAt = 0f;
        }

        public virtual bool IsAbleToRangeAttackNow(float now)
        {
            if (_action.IsStunned ||
                _action.IsDead)
            {
                return false;
            }

            float timeAfterLastAttack = now - _lastRangeAttackedAt;
            if (timeAfterLastAttack < this.RangeAttackDuration)
            {
                return false;
            }

            return true;
        }

        private void InitializeBaseStats(BaseStats baseStats)
        {
            _stats.ClearModifiers();
            _stats.SetBaseStats(baseStats);
        }

        public void ShowHPBar(Vector2 offsetFromPosition, Vector3 hpBarLocalScale, Color fillColor)
        {
            if (_hpBar == null)
            {
                _hpBar = ResourcePool.Instance.InstantiateFromResource<HPBar>("Stage/UIs/PlayerHPBar/HPBar.prefab");
                // NOTE: IsHeroType가 true면 PC라고 판단한다. 만약 PC외의 Character가 HeroType이면 수정해야합니다.
                _hpBar.AllocateSharedResources(this.CharacterType.IsHeroType());
            }
            _hpBar.Initialize(this.transform, offsetFromPosition, hpBarLocalScale, fillColor);
            _hpBar.gameObject.SetActive(true);
        }
        public void HideHPBar()
        {
            if (_hpBar != null)
            {
                _hpBar.gameObject.SetActive(false);
            }
        }

        public void HideGroundCircle()
        {
            if (_groundCircle != null)
            {
                _groundCircle.Clear();
                _groundCircle = null;
            }
        }

        public void SetHitBoxSize(Vector2 size)
        {
            _hitBoxCollider.size = size;
            _hitBoxCollider.gameObject.transform.localPosition = new Vector3(0.0f, size.y * 0.5f); // height의 절반만큼 위로 올린것을 기준으로 합니다.
        }

        public void SetHitBoxOffset(Vector2 offset)
        {
            _hitBoxCollider.offset = offset;
        }

        public Vector2 GetHitBoxSize()
        {
            return _hitBoxCollider.size;
        }

        public Vector2 GetHitBoxOffset()
        {
            return _hitBoxCollider.offset;
        }

        //체인 타겟을 지정 합니다.
        //체인 타겟이 있으면 타겟에 데미지를 입히고 당사자는 데미지를 입지 않습니다.
        //당사자는 데미지 입는 연출만 합니다.
        public void ChainTarget(Character target)
        {
            _chainTarget = target;
        }

        /// <summary>
        /// 방어막을 생성합니다. 피해를 받으면 방어막 개수를 1개 줄이고 피해를 무효화합니다.
        /// </summary>
        public abstract void CreateShield();

        /// <summary>
        /// 방어막을 파괴합니다.
        /// </summary>
        public void DestroyShield()
        {
            if (_shield == null)
            {
                Debug.LogWarning("방어막이 null인데 파괴하려고 했습니다.");
                return;
            }

            _shield.Destroy();
            _shield = null;
        }

        public void UpdateDrawOrder()
        {
            Debug.Assert(_bodyRenderer != null, "_bodyRenderer가 존재하지 않습니다. 현재 위치에 따른 DrawOrder를 계산 할 수 없습니다.");
            _bodyRenderer.sortingOrder = (int)(transform.position.y * -100.0f);
        }

    }
}
