using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class BouncingClawAreaEffectObject : AreaEffectObjectBase
    {
        public enum ChainProjectileState
        {
            MoveDirection,  //시작시 유저가 던진 투사체 상태
            HitTarget,      //데미지 전달
            FindTarget,     //다음 타겟 탐색
            MoveToTarget,   //다음 타겟으로 이동
            End,
        }

        public override bool IsAlive => _currentState != ChainProjectileState.End;

        private Character _owner;
        private Character _target;

        private Vector2 _movingDirection;
        private float _moveSpeed;
        private float _chainSpeed;
        private float _chainRadius;
        private float _firstDamage;
        private float _chainDamage;
        private float _scale;

        private ChainProjectileState _currentState;
        private float _chainAmount;
        private float _chainCount;

        private float _slowEffectDuration;
        private float _slowEffectSpeedChangeRate;


        private GameObject _normalSkillObject;
        private SpriteAnimationHandler _normalSkillObjectSpriteAnimationHandler;
        private SpriteRenderer _normalSpriteRenderer;

        private GameObject _transcendSkillObject;
        private SpriteAnimationHandler _transcendSkillObjectSpriteAnimationHandler;
        private SpriteRenderer _transcendSpriteRenderer;
        private TrailRenderer _transcendSkillObjectTrailRenderer;

        private bool _isTranscend;

        private static readonly float OBJECT_RADIUS = 0.8f;
        private static readonly string NORMAL_HIT_PARTICLE_PATH = "Stages/SkillEffects/BouncingClaw/FX_BouncingClaw_Bomb_N/FX_BouncingClaw_N-Begin.prefab";
        private static readonly string TRANSCENDENT_HIT_PARTICLE_PATH = "Stages/SkillEffects/BouncingClaw/FX_BouncingClaw_Bomb_S/FX_BouncingClaw_S-Begin.prefab";

        private static readonly string NORMAL_HIT_SFX_PATH = "Sounds/SoundEffects/PCs/BouncingClawNormalHit_SFX.prefab";
        private static readonly string TRANSCENDENT_HIT_SFX_PATH = "Sounds/SoundEffects/PCs/BouncingClawTranscendentHit_SFX.prefab";

        private HashSet<Character> _debuffedCharacters = new HashSet<Character>();

        private string _normalHitParticlePath;
        private string _transcendHitParticlePath;


        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.BouncingClaw);

            //해당 오브젝트의 하이어라키가 다른 오브젝트와 달라 SpriteRenderer를 SpriteAnimationHandler에서 불러올 수 없다.
            //SpriteRenderer는 따로 저장해둔다음 사용한다.

            string normalPrefabPath = "Stages/AreaEffects/BouncingClaw_N_AreaEffectObject.prefab";
            _normalSkillObject = ResourcePool.Instance.InstantiateFromResource(normalPrefabPath);
            _normalSkillObjectSpriteAnimationHandler = _normalSkillObject.GetComponentInChildren<SpriteAnimationHandler>();
            _normalSpriteRenderer = _normalSkillObject.GetComponentInChildren<SpriteRenderer>();
            _normalSkillObject.transform.SetParent(transform);
            _normalSkillObject.transform.localPosition = Vector3.zero;
            _normalSkillObject.transform.localScale = Vector3.one;

            string transcendPrefabPath = "Stages/AreaEffects/BouncingClaw_S_AreaEffectObject.prefab";
            _transcendSkillObject = ResourcePool.Instance.InstantiateFromResource(transcendPrefabPath);
            _transcendSkillObjectSpriteAnimationHandler = _transcendSkillObject.GetComponentInChildren<SpriteAnimationHandler>();
            _transcendSpriteRenderer = _transcendSkillObject.GetComponentInChildren<SpriteRenderer>();
            _transcendSkillObjectTrailRenderer = _transcendSkillObject.GetComponentInChildren<TrailRenderer>();
            _transcendSkillObject.transform.SetParent(transform);
            _transcendSkillObject.transform.localPosition = Vector3.zero;
            _transcendSkillObject.transform.localScale = Vector3.one;

            _normalHitParticlePath = NORMAL_HIT_PARTICLE_PATH;
            _transcendHitParticlePath = TRANSCENDENT_HIT_PARTICLE_PATH;

        }

        public void Initialize(
            Character owner,
            Vector2 movingDirection,
            float firstDamage,
            float chainDamage,
            int chainAmount,
            float chainRadius,
            float chainSpeed,
            float moveSpeed,
            float slowEffectDuration,
            float slowEffectSpeedChangeRate,
            float scale,
            bool isTranscend)
        {
            base.InitializeAreaObject(owner.Alliance);
            _owner = owner; 

            _firstDamage = firstDamage;
            _chainDamage = chainDamage;
            _chainAmount = chainAmount;
            _chainRadius = chainRadius;
            _chainSpeed = chainSpeed;
            _moveSpeed = moveSpeed;
            _slowEffectDuration = slowEffectDuration;
            _slowEffectSpeedChangeRate = slowEffectSpeedChangeRate;

            _scale = scale;

            _movingDirection = movingDirection;
            _currentState = ChainProjectileState.MoveDirection;
            _chainCount = 0;

            this.transform.localScale = Vector3.one * scale;
            this.transform.position = _owner.CenterPos;

            _isTranscend = isTranscend;
            if(isTranscend)
            {
                _normalSkillObject.gameObject.SetActive(false);
                _transcendSkillObject.gameObject.SetActive(true);
                _transcendSkillObjectTrailRenderer.Clear();
                _transcendSkillObjectSpriteAnimationHandler.InitializeAndPlay();
            }
            else
            {
                _normalSkillObject.gameObject.SetActive(true);
                _transcendSkillObject.gameObject.SetActive(false);
                _normalSkillObjectSpriteAnimationHandler.InitializeAndPlay();
            }

        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;
            switch (_currentState)
            {
                case ChainProjectileState.MoveDirection:
                    {
                        this.Move(deltaTime, _moveSpeed);
                        this.RotateBodyImageToMoveDirection();
                        _target = this.FindHitTarget(stage, radius: OBJECT_RADIUS * _scale);//this.FindClosetTarget(stage, radius: OBJECT_RADIUS * _scale);
                        if (_target != null)
                        {
                            _currentState = ChainProjectileState.HitTarget;
                        }
                    }
                    break;
                case ChainProjectileState.HitTarget:
                    {
                        //첫번째 타격은 별도의 파라미터를 사용한다.
                        if(_chainCount == 0)
                        {
                            _target.Hitted(stage, _owner, _firstDamage, Vector2.zero, _target.CenterPos, null);
                            UnityGlobal.Sounds.PlayBySoundPrefab(_isTranscend ? TRANSCENDENT_HIT_SFX_PATH : NORMAL_HIT_SFX_PATH, transform.position);
                        }
                        else
                        {
                            _target.Hitted(stage, _owner, _chainDamage, Vector2.zero, _target.CenterPos, null);
                        }

                        //공격 한번에 한번의 상태 이상만 부여
                        if(!_debuffedCharacters.Contains(_target) && 
                            !_target.IsBoss)
                        {
                            _debuffedCharacters.Add(_target);
                            if(_isTranscend)
                            {
                                if (!_target.Action.IsSkilling)
                                {
                                    _target.StatusEffects.AddOrUpdateStatusEffect(stage, _target, Characters.StatusEffects.StatusEffectType.Stun, _slowEffectDuration, now, 0f);
                                }
                            }
                            else
                            {
                                _target.StatusEffects.AddOrUpdateStatusEffect(stage, _target, Characters.StatusEffects.StatusEffectType.SlowMove, _slowEffectDuration, now, _slowEffectSpeedChangeRate);
                            }
                        }

                        if(_isTranscend)
                        {
                            UnityGlobal.SpriteAnimations.CreateAndPlaySpriteAnimation(_transcendHitParticlePath, _target.CenterPos, Vector2.one *2 * _scale, null);
                        }
                        else
                        {
                            UnityGlobal.SpriteAnimations.CreateAndPlaySpriteAnimation(_normalHitParticlePath, _target.CenterPos, Vector2.one * 2 * _scale, null);
                        }

                        _chainCount++;
                        if(_chainCount >= _chainAmount)
                        {
                            _currentState = ChainProjectileState.End;
                            return;
                        }
                        else
                        {
                            _currentState = ChainProjectileState.FindTarget;
                        }
                    }
                    break;
                case ChainProjectileState.FindTarget:
                    {
                        _target = this.FindClosetTarget(stage, _chainRadius);

                        if(_target  != null)
                        {
                            _currentState = ChainProjectileState.MoveToTarget;
                        }
                        else
                        {
                            _currentState = ChainProjectileState.End;
                            return;
                        }
                    }
                    break;
                case ChainProjectileState.MoveToTarget:
                    {
                        if(_target == null || _target.Action.IsDead)
                        {
                            _currentState = ChainProjectileState.End;
                            return;
                        }

                        if(Vector2.Distance(this.transform.position, _target.CenterPos) <= _chainSpeed * deltaTime)
                        {
                            this.AttatchToTarget();
                            _currentState = ChainProjectileState.HitTarget;
                        }
                        else
                        {
                            Vector2 currentPosition = this.transform.position;
                            _movingDirection = (_target.CenterPos - currentPosition).normalized;
                            this.Move(deltaTime, _chainSpeed);
                            this.RotateBodyImageToMoveDirection();
                        }
                    }
                    break;
                case ChainProjectileState.End:
                    {
                        return;
                    }
                    break;
            }
        }

        private void Move(float deltaTime, float speed)
        {
            Vector2 currentPosition = this.transform.position;
            Vector2 nextPosition = currentPosition + _movingDirection * speed * deltaTime;
            this.transform.position = nextPosition;
        }

        private void RotateBodyImageToMoveDirection()
        {
            Vector2 direction = _movingDirection * _moveSpeed;
            transform.right = Quaternion.AngleAxis(-90.0f, Vector3.forward) * direction;
        }

        // 범위내에 가장 가까운 캐릭터를 탐색해서 전달한다.
        private Character FindClosetTarget(Stage stage, float radius)
        {
            return stage.FindClosestCharacter(_owner.Alliance.ToEnemyAlliance(),
                                            this.transform.position,
                                            limitDistance: radius,
                                            condition: character => !character.Action.IsDead && !character.IsImmuneToHit && character != _target);
        }

        private Character FindHitTarget(Stage stage, float radius)
        {
            var targets = new List<Character>();
            stage.FindAliveCharactersInArea(_owner.Alliance.ToEnemyAlliance(), this.transform.position, radius, radius + 4f, targets);

            foreach (var target in targets)
            {
                if(!target.Action.IsDead && !target.IsImmuneToHit &&  target != _target && IsTargetHit(target, radius))
                {
                    return target;
                }
            }

            return null;

        }

        private void AttatchToTarget()
        {
            if(_target != null)
            {
                this.transform.position = _target.transform.position;
            }
        }


        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _owner = null;
            _target = null;
            _transcendSkillObjectTrailRenderer.Clear();
            _debuffedCharacters.Clear();
        }

        public bool IsTargetHit(Character target, float radius)
        {
            Vector2 hitBoxCenter = target.Pos + target.GetHitBoxOffset();
            Vector2 hitBoxSize = target.GetHitBoxSize();

            Vector2 bouncingClawPos = this.transform.position;

            Vector2 distance = bouncingClawPos - hitBoxCenter;

            float closestX = Mathf.Clamp(distance.x, -hitBoxSize.x / 2, hitBoxSize.x / 2);
            float closestY = Mathf.Clamp(distance.y, -hitBoxSize.y / 2, hitBoxSize.y / 2);

            Vector2 closestPoint = new Vector2(closestX, closestY);
            Vector2 closestDistance = distance - closestPoint;

            return closestDistance.sqrMagnitude <= radius * radius;
        }

    }

}

