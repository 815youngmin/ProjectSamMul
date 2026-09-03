using Z.Animations.Placeholder;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class GluttonObject : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + _lifeTime;

        //몸체가 될 이미지
        private GameObject _bodyImage;
        private GameObject _normalSkillImage;
        private GameObject _transcendSkillImage;

        private SpriteRenderer _droneShadow;

        private Character _owner;

        private float _objectRadius;
        private Vector2 _movingDirection;
        private float _movingSpeed;
        private float _oppositeDirectionAcceleration;
        private float _damage;
        private float _knockBackPower;
        private float _lifeTime;
        private float _createdAt;


        private HashSet<Character> _hittedCharacters;
        private SkeletonAnimation _currentImageSkeletonAnimation;
        private SkeletonAnimation _normalImageSkeletonAnimation;
        private SkeletonAnimation _transcendImageSkeletonAnimation;
        private ParticleSystem _transcendSkillTrailSystem;

        private float _turnDuration;
        private bool _isPlayTurn;
        private bool _isPlayTurnAfterIdle;
        private float _turnAnimationPlayedAt;
        public bool IsTurning => Time.time < _turnAnimationPlayedAt + _turnDuration;

        private bool _isTranscend;

        private string _hitSoundPrefabPath;

        //_bodyImage: 몸체 이미지
        //_owner: 생성한 캐릭터
        //_objectRadius: 공격 범위
        //_movingDirection: 이동 방향
        //_movingSpeed: 현재 이동 속도
        //_oppositeDirectionAcceleration: 이동 방향과 반대 방향으로 가속되는 힘
        //_baseDamage: 공격대미지
        //_lifetime: 살아있는 시간
        //_createdAt: 생성된 시간
        //해당 스킬은 갈때 한번, 올때 한번 최대 2회만 공격 가능하다.
        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.Glutton);
 
            _hittedCharacters = new HashSet<Character>();
            
            string normalImageSpinePath = "Stages/AreaEffects/Glutton/Blade_Drone_SkeletonData.asset";
            string transcendImageSpinePath = "Stages/AreaEffects/Glutton/Glutton_S.prefab";
            _bodyImage = new GameObject("BaldeDroneBodyImage");
            _bodyImage.transform.SetParent(this.gameObject.transform);

            _normalSkillImage = new GameObject("normalSkillImage");
            _normalSkillImage.transform.SetParent(_bodyImage.transform);
            _normalSkillImage.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            _normalImageSkeletonAnimation = SpineHelper.LoadSpine(_normalSkillImage, normalImageSpinePath, sortingLayerName: "HighParticle");
            _normalImageSkeletonAnimation.AnimationState.Data.SetMix("idle", "turn", 0);
            _normalImageSkeletonAnimation.AnimationState.Data.SetMix("turn", "idle", 0);

            _transcendSkillImage = ResourcePool.Instance.InstantiateFromResource(transcendImageSpinePath);
            _transcendSkillImage.transform.SetParent(_bodyImage.transform);
            _transcendSkillImage.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            _transcendImageSkeletonAnimation = _transcendSkillImage.GetComponent<SkeletonAnimation>();
            _transcendImageSkeletonAnimation.AnimationState.Data.SetMix("idle", "turn", 0);
            _transcendImageSkeletonAnimation.AnimationState.Data.SetMix("turn", "idle", 0);
            _transcendSkillTrailSystem = _transcendSkillImage.GetComponentInChildren<ParticleSystem>();
            Debug.Assert(null != _transcendSkillTrailSystem);

            var shadowObject = new GameObject("Shadow");
            shadowObject.transform.SetParent(this.gameObject.transform, worldPositionStays: false);
            var shadow = shadowObject.AddComponent<SpriteRenderer>();
            // TODO 플레이어용 그림자 쓸 것
            shadow.sprite = ResourcePool.Instance.LoadResource<Sprite>("Stages/Items/ItemShadow.png");
            shadow.color = new Color(shadow.color.r, shadow.color.g, shadow.color.b, 0.50f);
            shadow.sortingLayerID = SortingLayer.NameToID("LowShadow");
            shadow.drawMode = SpriteDrawMode.Simple;
            shadow.transform.localScale = new Vector3(1.5f, 1, 1);
            shadow.transform.localPosition = new Vector3(0, -1f, 1);
            _droneShadow = shadow;

        }

        public void Initialize(
            AllianceType alliance,
            Character owner,
            float objectRadius,
            Vector2 movingDirection,
            float movingSpeed,
            float oppositeDirectionAcceleration,
            float damage,
            float knockBackPower,
            float lifeTime,
            bool isTranscend,
            string hitSoundPrefabPath)
        {
            base.InitializeAreaObject(alliance);
            _owner = owner;
            _objectRadius = objectRadius;
            _movingDirection = movingDirection;
            _movingSpeed = movingSpeed;
            _oppositeDirectionAcceleration = oppositeDirectionAcceleration;
            _damage = damage;
            _knockBackPower = knockBackPower;
            _lifeTime = lifeTime;

            _createdAt = Time.time;

            this.transform.position = _owner.CenterPos;
            _isTranscend = isTranscend;

            if(_isTranscend)
            {
                _transcendSkillImage.SetActive(true);
                _normalSkillImage.SetActive(false);
                _currentImageSkeletonAnimation = _transcendImageSkeletonAnimation;
            }
            else
            {
                _transcendSkillImage.SetActive(false);
                _normalSkillImage.SetActive(true);
                _currentImageSkeletonAnimation = _normalImageSkeletonAnimation;
            }

            _currentImageSkeletonAnimation.AnimationState.SetAnimation(0, "idle", true);
             _turnDuration = _currentImageSkeletonAnimation.AnimationState.Data.SkeletonData.FindAnimation("turn").Duration;
            _isPlayTurn = false;
            _isPlayTurnAfterIdle = false;
            _hitSoundPrefabPath = hitSoundPrefabPath;
            _hittedCharacters.Clear();
        }

        private void MoveToCurrentPosition(float deltaTime)
        {
            Vector2 currentPosition = this.transform.position;
            Vector2 nextPosition = currentPosition + _movingDirection * deltaTime * _movingSpeed;
            this.transform.position = nextPosition;
        }

        private void RotateToMoveDirection()
        {
            Vector2 direction = _movingDirection * _movingSpeed;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _bodyImage.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        }

        private void AccelerationMovingSpeed(float deltaTime)
        {
            _movingSpeed -= _oppositeDirectionAcceleration * deltaTime;
        }


        //현재 드론의 상황에 맞는 애니메이션을 재생하고 공격 가능한 캐릭터 리스트를 초기화 한다.
        private void CheckingDroneHittedChatersAndChangeAnimation()
        {   
            //블레이드 드론의 회전 애니메이션의 0.4 지점이 속도가 0이 되는 지점과 맞아야 된다.
            if (_movingSpeed - _oppositeDirectionAcceleration * (_turnDuration * 0.4f) <= 0 && !_isPlayTurn)
            {
                _isPlayTurn = true;
                _turnAnimationPlayedAt = Time.time;
                _currentImageSkeletonAnimation.AnimationState.SetAnimation(0, "turn", false);
                if (_isTranscend)
                {
                    _transcendSkillTrailSystem.gameObject.SetActive(false);
                }
            }
            //블레이드 드론의 회전 애니메이션이 끝나고 다시 idle 애니메이션을 재생해줘야 한다.
            else if (_isPlayTurn && Time.time > _turnAnimationPlayedAt + _turnDuration && !_isPlayTurnAfterIdle)
            {
                _isPlayTurnAfterIdle = true;
                _currentImageSkeletonAnimation.AnimationState.SetAnimation(0, "idle", true);
                _hittedCharacters.Clear();
                if (_isTranscend)
                {
                    _transcendSkillTrailSystem.gameObject.SetActive(true);
                }
            }
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _owner = null;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            //회전중인 경우 이동 방향에 따른 이미지 회전을 잠시 멈춘다.
            if(!IsTurning)
            {
                this.RotateToMoveDirection();
            }
            this.MoveToCurrentPosition(deltaTime);
            this.AccelerationMovingSpeed(deltaTime);
            this.CheckingDroneHittedChatersAndChangeAnimation();

            var targetArea = new CircularTargetArea(this.transform.position, _objectRadius);
            CombatSystem.HitOnTargetArea(stage, targetArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, targetArea.Center, _knockBackPower, _hittedCharacters, _hittedCharacters, _hitSoundPrefabPath); 
        }
    }
}
