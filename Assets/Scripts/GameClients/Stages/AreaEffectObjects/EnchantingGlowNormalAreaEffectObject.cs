using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.Characters.StatusEffects;
using Z.GameClients.Stages.CombatSystems;
using Z.GameClients.Stages.ItemObjects;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class EnchantingGlowNormalAreaEffectObject : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + _attackDuration;

        private readonly float AttackAngle = 20f;

        private Character _owner;
        private float _createdAt;

        private CircularSectorTargetArea _targetArea;

        private float _damage;
        private int _attackAmount;
        private Vector2 _startPosition;
        private Vector2 _attackDirection;
        private float _knobackPower;
        private float _attackRadius;
        private float _attackPeriod;
        private float _attackDuration;
        private float _stunDuration;
        private float _stunPeriod;
        private bool _removePoisons;

        private HashSet<Character> _exceptedCharacters;
        private List<Character> _stunedCharacters;

        private float _clearHittedCharactersAt;
        private float _clearStunedCharactersAt;


        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.EnchantingGlowNormalAreaEffectObject);
        }

        public void Initialize(
            Stage stage,
            Character owner,
            int attackAmount,
            float damage,
            Vector2 startPosition,
            Vector2 attackDirection,
            float knobackPower,
            float attackRadius,
            float attackPeriod,
            float attackDuration,
            float stunDuration,
            float stunPeriod,
            bool removePoisons
            )
        {
            base.InitializeAreaObject(owner.Alliance);
            float now = Time.time;
            
            _owner = owner;
            _attackAmount = attackAmount;
            _damage = damage;
            _knobackPower = knobackPower;
            _startPosition = startPosition;
            _attackDirection = attackDirection;
            _attackRadius = attackRadius;
            _attackPeriod = attackPeriod;
            _attackDuration = attackDuration;
            _stunDuration = stunDuration;
            _stunPeriod = stunPeriod;
            _removePoisons = removePoisons;

            _createdAt = now;

            _exceptedCharacters = new HashSet<Character>();
            _stunedCharacters = new List<Character>();

            this.transform.position = _startPosition;

            //공격 이펙트에 딱 맞춰서 탐색하니 어색하게 안맞는 몬스터가 존재한다.
            //이펙트보다 조금더 넓은 범위만큼 탐색하고 공격한다.
            Vector2 targetAreaPos = _startPosition - _attackDirection.normalized * 1.0f;
            float targetAreaRadius = _attackRadius + 2f;
            _targetArea = new CircularSectorTargetArea(targetAreaPos, _attackDirection, targetAreaRadius, AttackAngle * _attackAmount);

            //공격 범위만큼 이펙트 생성
            for (int i = -(_attackAmount - 1); i <= _attackAmount - 1; i += 2)
            {
                Vector2 dir = Quaternion.Euler(0.0f, 0.0f, 0.5f * (float)i * AttackAngle) * _attackDirection;
                stage.CreateEnchantingGlowBodyObject(_owner, _startPosition, dir, _attackRadius, _attackDuration);
            }
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;

            // 공격한 캐릭터 리스트 초기화
            if (_clearHittedCharactersAt <= now)
            {
                _exceptedCharacters.Clear();
                _clearHittedCharactersAt = now + _attackPeriod;
            }
            // 스턴 걸렸던 캐릭터 리스트 초기화
            if (_clearStunedCharactersAt <= now)
            {
                _stunedCharacters.Clear();
                _clearStunedCharactersAt = now + _stunPeriod;
            }

            //공격 대상 탐색 후 데미지 및 스턴 처리
            List<Character> findedCharacters = new List<Character>();
            stage.FindAliveCharactersInArea(_owner.Alliance.ToEnemyAlliance(), _targetArea, findedCharacters);
            foreach (var target in findedCharacters)
            {
                //공격 제외 대상 검색
                if (null != _exceptedCharacters && _exceptedCharacters.Contains(target))
                {
                    continue;
                }
                float damage = CalculateDamage(_targetArea.Center, target.Pos, _damage * 0.85f, _damage, _targetArea.Radius * 0.2f, _targetArea.Radius);
                target.Hitted(stage, _owner, damage, Vector2.zero, target.Pos, null);


                //스턴 적용 가능한지 확인한다.
                if (_stunDuration > 0f &&
                    !target.IsBoss &&
                    !_stunedCharacters.Contains(target))
                {
                    //엘리트는 스턴 시간을 감소시킨다
                    float stunDuration = target.IsElite ? _stunDuration * 0.7f : _stunDuration;
                    target.StatusEffects.AddOrUpdateStatusEffect(stage, target, StatusEffectType.Stun, duration: stunDuration, Time.time, 0f);
                    _stunedCharacters.Add(target);
                }

                //공격 제외대상에 추가한다.
                _exceptedCharacters.Add(target);
            }

            // 타겟 범위에 맞는 아이템 오브젝트도 확인해 공격한다.
            IReadOnlyList<BreakableItemObject> breakableItemObjects = stage.BreakableItemObjects;
            foreach (var breakableItemObject in breakableItemObjects)
            {
                if (_targetArea.Contains(breakableItemObject.transform.position))
                {
                    float damage = CalculateDamage(_targetArea.Center, breakableItemObject.transform.position, _damage * 0.85f, _damage, _targetArea.Radius * 0.3f, _targetArea.Radius);
                    breakableItemObject.OnBroken(_owner, damage, stage);
                }
            }


            if (_removePoisons)
            {
                stage.ForAllAliveAreaEffects((areaEffect) =>
                {
                    if (areaEffect.AreaEffectObjectType != AreaEffectType.PoisonousArea)
                    {
                        return true;
                    }

                    var poisonousAreaEffect = areaEffect as PoisonousAreaEffectObject;
                    if (poisonousAreaEffect == null)
                    {
                        return true;
                    }

                    if (!_targetArea.Contains(poisonousAreaEffect.Center, poisonousAreaEffect.Radius))
                    {
                        return true;
                    }

                    poisonousAreaEffect.TryDisappear();
                    return true;
                });
            }
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
        }

        /// <summary>
        /// 거리에 따른 데미지를 계산해 반환해준다.
        /// </summary>
        /// <param name="attackPosition">공격 시작점</param>
        /// <param name="monsterPosition">몬수터 위치 </param>
        /// <param name="minDamage">최소 데미지 </param>
        /// <param name="maxDamage">최대 데미지</param>
        /// <param name="effectiveRange">데미지가 감소하기 시작하는 거리 </param>
        /// <param name="maxRange">최대 거리</param>
        /// <returns></returns>
        public float CalculateDamage(Vector2 attackPosition, Vector2 monsterPosition, float minDamage, float maxDamage, float effectiveRange, float maxRange)
        {
            // 플레이어와 몬스터 사이의 거리 계산
            float distance = Vector2.Distance(attackPosition, monsterPosition);

            // 거리가 효과 범위 이내면 최대 데미지
            if (distance <= effectiveRange)
            {
                return maxDamage;
            }

            // 효과 범위와 최대 범위 사이에서는 선형적으로 데미지 감소
            float t = (distance - effectiveRange) / (maxRange - effectiveRange);
            return Mathf.Lerp(maxDamage, minDamage, t); // 선형 보간을 통해 데미지 계산
        }


    }

}
