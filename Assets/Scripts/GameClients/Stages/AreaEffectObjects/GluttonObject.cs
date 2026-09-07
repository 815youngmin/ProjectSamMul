using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    /// <summary>
    /// 글루튼(블레이드 드론). 전용 스파인 리소스 없이 공용 공격 비주얼을 판정 지름에 맞춰 사용한다.
    /// </summary>
    public class GluttonObject : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + _lifeTime;

        // 원본 회전(turn) 애니메이션 길이. 속도가 0이 되는 시점과 맞추기 위한 타이밍 값으로만 쓴다.
        private const float TURN_DURATION = 0.5f;

        private GameObject _visual;
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

        private bool _isPlayTurn;
        private bool _isPlayTurnAfterIdle;
        private float _turnAnimationPlayedAt;
        public bool IsTurning => Time.time < _turnAnimationPlayedAt + TURN_DURATION;

        private bool _isTranscend;

        private string _hitSoundPrefabPath;

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

            _visual = PlayerAttackVisual.Attach(transform, 1f);

            var shadowObject = new GameObject("Shadow");
            shadowObject.transform.SetParent(this.gameObject.transform, worldPositionStays: false);
            var shadow = shadowObject.AddComponent<SpriteRenderer>();
            shadow.sprite = ResourcePool.Instance.LoadResource<Sprite>("Stage/Common/ItemShadow.png");
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

            PlayerAttackVisual.SetDiameter(_visual, _objectRadius * 2f);

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
            _visual.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        }

        private void AccelerationMovingSpeed(float deltaTime)
        {
            _movingSpeed -= _oppositeDirectionAcceleration * deltaTime;
        }


        //드론이 방향을 돌리는 시점을 판정하고, 돌아온 뒤 공격 가능한 캐릭터 리스트를 초기화 한다.
        private void CheckingDroneHittedChatersAndChangeAnimation()
        {
            //블레이드 드론의 회전 애니메이션의 0.4 지점이 속도가 0이 되는 지점과 맞아야 된다.
            if (_movingSpeed - _oppositeDirectionAcceleration * (TURN_DURATION * 0.4f) <= 0 && !_isPlayTurn)
            {
                _isPlayTurn = true;
                _turnAnimationPlayedAt = Time.time;
            }
            //블레이드 드론의 회전이 끝나면 되돌아오는 길에 다시 공격할 수 있게 한다.
            else if (_isPlayTurn && Time.time > _turnAnimationPlayedAt + TURN_DURATION && !_isPlayTurnAfterIdle)
            {
                _isPlayTurnAfterIdle = true;
                _hittedCharacters.Clear();
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
