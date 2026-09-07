using Shared.StaticDatas;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Stats;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.GameClients.Stages.ItemObjects;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.Characters.PCs.Skills
{
    public class DeathTouchSkill : SkillBase
    {
        private float _attackPowerRate;
        private float _missileFireDuration; //미사일 발사 지속시간
        private float _missileFireCoolTime; //미사일 발사 끝나고 쿨타임
        private int _missileFireAmount;     //데이터 테이블의 발사 개수
        private int _missileFireTotalAmount;//최종 미사일 발사 개수

        private int _currentfireAmount;
        private float _knockBackPower;
        private float _searchingDistance; // 발사할 때 타겟을 탐색하는 거리

        // 드론 몸체. 전용 스파인 대신 스프라이트 프리팹을 쓴다. 기본 방향은 오른쪽을 본다.
        private const string DRONE_BODY_PREFAB_PATH = "Skill/DeathTouch/DeathTouchBody.prefab";
        private GameObject _droneBodyImage;
        private SpriteRenderer _droneBodyRenderer;

        private SpriteRenderer _droneShadow;
        private Character _target;
        private BreakableItemObject _targetItem;    //_target이 null인 경우 다음 공격 대상으로 처리된다.
        private static readonly float SearchTargetInterval = 0.1f;
        private float _searchTargetAt;

        //플레이어기준으로 드론의 위치 
        private Vector2 _rightOffset;
        private Vector2 _leftOffset;

        //미사일 개당 발사 간격
        private float _missileDroneObjectCreatePeriod;

        //미사일 발사상태를 체크하는 변수
        private bool _isFireActivated;
        private float _fireActivatingAt => _fireDeactivatedAt + (_missileFireCoolTime / _characterStats.SkillAttackSpeedValue);
        private float _fireDeactivatingAt;
        private float _fireDeactivatedAt;

        private float _playerAttackRangeDistanceRatio;

        private List<float> _missileFiringAts = new List<float>();

        private readonly IReadOnlyCharacterStatCalculators _characterStats;

        public DeathTouchSkill(SkillStaticData staticData, IReadOnlyCharacterStatCalculators characterStats) : base(staticData)
        {
            _attackPowerRate = staticData.Parameter1;
            _missileFireDuration = staticData.Parameter2;
            _missileFireCoolTime = staticData.Parameter3;
            _missileFireAmount = (int)staticData.Parameter4;
            _knockBackPower = staticData.Parameter5;
            _searchingDistance = staticData.Parameter6;

            _characterStats = characterStats;
        }
        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);
            this.LoadOriginalResourceAndInitialize(owner);

            this.ActivateFire(owner, stage, now);
        }

        private void LoadOriginalResourceAndInitialize(PlayerCharacter owner)
        {
            _rightOffset = new Vector2(1.5f, 1.5f);
            _leftOffset = new Vector2(-1.5f, 1.5f);

            _droneBodyImage = ResourcePool.Instance.InstantiateFromResource(DRONE_BODY_PREFAB_PATH);
            _droneBodyImage.name = "MissileDroneBodyImage";
            _droneBodyRenderer = _droneBodyImage.GetComponent<SpriteRenderer>();
            _droneBodyImage.SetActive(true);
    
            if (owner.AnimationController.Body.skeleton.ScaleX < 0)
            {
                _droneBodyImage.transform.position = (Vector2)owner.transform.position + _leftOffset ;
            }
            else
            {
                _droneBodyImage.transform.position = (Vector2)owner.transform.position + _rightOffset;
            }

            var shadowObject = new GameObject("Shadow");
            shadowObject.transform.SetParent(_droneBodyImage.transform, worldPositionStays: false);

            var shadow = shadowObject.AddComponent<SpriteRenderer>();
            shadow.sprite = ResourcePool.Instance.LoadResource<Sprite>("Stage/Common/CharacterShadow.png");
            shadow.color = new Color(shadow.color.r, shadow.color.g, shadow.color.b, 0.9f);
            shadow.sortingLayerID = SortingLayer.NameToID("LowShadow");
            shadow.drawMode = SpriteDrawMode.Simple;
            shadow.transform.localScale = new Vector3(1.5f, 1, 1);
            _droneShadow = shadow;

        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);
            _isFireActivated = false;

            _currentfireAmount = 999999;
            _missileFireTotalAmount = 0;
            GameObject.Destroy(_droneBodyImage);
        }

        private void MoveToCurrentDronePosition(PlayerCharacter owner)
        {
            float maxDistance = 10f;
            float currentDistance;
            Vector2 nextPosition;
            if (owner.AnimationController.Body.skeleton.ScaleX < 0)
            {
                currentDistance =  Vector2.Distance((Vector2)owner.transform.position + _leftOffset, _droneBodyImage.transform.position);
                nextPosition = (Vector2)owner.transform.position + _leftOffset;
            }
            else
            {
                currentDistance = Vector2.Distance((Vector2)owner.transform.position + _rightOffset, _droneBodyImage.transform.position);
                nextPosition = (Vector2)owner.transform.position + _rightOffset;
            }

            float distanceRatio = Mathf.Clamp01(currentDistance / maxDistance);
            float moveSpeedValue = Mathf.Lerp(0.02f, 0.06f, distanceRatio);
            
            _droneBodyImage.transform.position = Vector2.Lerp(_droneBodyImage.transform.position, nextPosition, moveSpeedValue);
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            this.MoveToCurrentDronePosition(owner);

            if (_isFireActivated)
            {
                if (_fireDeactivatingAt <= now)
                {
                    this.DeactivateFire(owner, stage, now);
                }
                else
                {
                    this.UpdateTarget(owner, stage, now);
                    this.UpdateMissileFire(owner, stage, now);
                }
            }
            else
            {
                if (_fireActivatingAt <= now)
                {
                    this.ActivateFire(owner, stage, now);
                }
            }
            this.UpdateMissileDroneBodyImageDirection(owner, _target);
        }
        private void ActivateFire(Character owner, Stage stage, float now)
        {
            float missileFiringDuration = (_missileFireDuration * owner.Stats.DurationIncreaseRate.Value) / _characterStats.SkillAttackSpeedValue;
            _fireDeactivatingAt = now + missileFiringDuration;

            // NOTE: 지속시간이 증가한 만큼 발사체 갯수도 늘려준다. 갯수 이기 때문에 소수는 버린다.
            _missileFireTotalAmount = Mathf.FloorToInt(_missileFireAmount * owner.Stats.DurationIncreaseRate.Value);

            _isFireActivated = true;

            // NOTE: 드론의 데이터 테이블상 지속시간은 99999이다 발사 시작시 들어오는 함수에 비율 계산을 진행한다.
            _playerAttackRangeDistanceRatio = ((PlayerCharacter)owner).Stats.AttackRangeDistanceRatio.Value;
            //발사체 발사 간격
            _missileDroneObjectCreatePeriod = missileFiringDuration / _missileFireTotalAmount;
           
            _missileFiringAts.Clear();
            for (int i = 0; i < _missileFireTotalAmount; i ++)
            {
                _missileFiringAts.Add(now + (_missileDroneObjectCreatePeriod * i));
            }

            _searchTargetAt = now;

            _currentfireAmount = 0;
            
            // 발사 시작할 때 한번 재생시키고 끝낸다. 타격이 일어나면 타격사운드가 나올 것임
            PlaySkillSoundEffect(owner.Pos);
        }

        private void DeactivateFire(PlayerCharacter owner, Stage stage, float now)
        {
            _fireDeactivatedAt = now;
            _isFireActivated = false;

            //혹시나 남아있는 미사일 오브젝트를 생성해준다
            while (_missileFireTotalAmount > _currentfireAmount)
            {
                this.CreateDeathTouchEffectObjectAndInitialize(owner, stage);
                _currentfireAmount++;
            }
            _target = null;
            _targetItem = null;
        }

        private void UpdateMissileFire(PlayerCharacter owner, Stage stage, float now)
        {
            if (_currentfireAmount >= _missileFireTotalAmount)
            {
                return;
            }

            //미사일 발사시간들을 확인하고 발사한다 (한 프레임에 동시에 여러발 나갈 수도 있다)
            while (_currentfireAmount < _missileFireTotalAmount && _missileFiringAts[_currentfireAmount] < now)
            {
                this.CreateDeathTouchEffectObjectAndInitialize(owner, stage);
                _currentfireAmount++;
            }
        }

        private Character SearchClosestTarget(Character owner, Stage stage)
        {
            float searchDistance = _searchingDistance * _playerAttackRangeDistanceRatio;

            var target = stage.FindClosestCharacter(
                owner.Alliance.ToEnemyAlliance(),
                owner.CenterPos,
                limitDistance: searchDistance + 2f,
                condition: character => !character.Action.IsDead && !character.IsImmuneToHit);

            if (target != null)
            {
                return target;               
            }
            else
            {
                return null;
            }
        }

        private BreakableItemObject SearchClosetBrekableItemObject(PlayerCharacter owner, Stage stage)
        {
            float searchDistance = _searchingDistance * _playerAttackRangeDistanceRatio;
            var item = owner.FindClosestBreakableItemObjectExceptFence(stage, searchDistance + 2f);
            if (item != null)
            {
                return item;
            }
            else
            {
                return null;
            }
        }

        private void UpdateMissileDroneBodyImageDirection(Character owner, Character target)
        {
            bool isDroneDirectionToLeft;
            if (target != null)
            {
                Vector2 dir = target.transform.position - _droneBodyImage.transform.position;
                isDroneDirectionToLeft = dir.x <= 0;
            }
            else
            {
                Vector2 dir = owner.transform.position - _droneBodyImage.transform.position;
                isDroneDirectionToLeft = dir.x <= 0;
            }

            _droneBodyRenderer.flipX = isDroneDirectionToLeft;
        }

        private void CreateDeathTouchEffectObjectAndInitialize(PlayerCharacter owner, Stage stage)
        {
            Vector2 destination;
            if (_target != null)
            {
                float circleSize = 0.8f * _playerAttackRangeDistanceRatio;
                destination = _target.CenterPos + Random.insideUnitCircle * circleSize;
            }
            else
            {
                if(_targetItem != null)
                {
                    float circleSize = 0.8f * _playerAttackRangeDistanceRatio;
                    destination = (Vector2)_targetItem.transform.position + Random.insideUnitCircle * circleSize;
                }
                else
                {
                    float circleSize = 6f * _playerAttackRangeDistanceRatio;
                    destination = owner.CenterPos + Random.insideUnitCircle * circleSize;
                }

            }

            float damage = CombatSystem.CalculateSkillAttackDamage(owner.Stats, _attackPowerRate);
            float knockbackPower = CombatSystem.CalculateSkillAttackKnockBackPower(owner.Stats, _knockBackPower);
            float baseMovingTime = 0.3f - (0.3f *  (_characterStats.ProjectileMoveSpeedIncreaseRateValue - 1)); // 미사일드론 오브젝트 이동처리는 시간으로 계산한다
            float areaEffectRadius = 1f * _playerAttackRangeDistanceRatio;
            Vector3 missileDroneObjectScale = Vector3.one * _playerAttackRangeDistanceRatio;
            Vector2 createPosition = _droneBodyImage.transform.position;
            
            var missileDroneObject = stage.CreateDeathTouchEffectObject(
                owner.Alliance,
                owner,
                areaEffectRadius,
                createPosition,
                destination,
                baseMovingTime,
                damage,
                knockbackPower,
                IsTranscendent,
                StaticData.SkillHitSFXPath
            );
            missileDroneObject.transform.localScale = missileDroneObjectScale;
        }

        private void UpdateTarget(PlayerCharacter owner, Stage stage, float now)
        {
            if(now < _searchTargetAt)
            {
                return;
            }
            _target = this.SearchClosestTarget(owner, stage);
            _targetItem = _target != null ? null : this.SearchClosetBrekableItemObject(owner, stage);

            _searchTargetAt = now + SearchTargetInterval;
        }

        public override void ReApplyStat(PlayerCharacterStatCalculators ownerStats)
        {
            //미사일드론은 미사일 발사 간격이 짧아 _playerAttackRangeDistanceRatio값을 변경해 주기만 해도
            //자연스럽게 스탯이 적용된것처럼 보인다.
            _playerAttackRangeDistanceRatio = ownerStats.AttackRangeDistanceRatio.Value;
        }
    }
}
