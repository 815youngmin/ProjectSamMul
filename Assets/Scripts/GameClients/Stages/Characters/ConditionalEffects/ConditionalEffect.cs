using DG.Tweening;
using Shared.GameDataTypes;
using Shared.Localizers;
using Shared.StaticDatas;
using SamMul.Animations.Placeholder;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using SamMul.GameClients.Stages.AreaEffectObjects;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.GameClients.Stages.Characters.PCs;
using SamMul.GameClients.Stages.Characters.Stats;
using SamMul.GameClients.Stages.Characters.StatusEffects;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.Loggers;
using SamMul.ResourcePools;
using SamMul.Scenes;
using SamMul.UIs.Stages.Popups;
using SamMul.UnityHelpers;
using Random = UnityEngine.Random;

namespace SamMul.GameClients.Stages.Characters.ConditionalEffects
{

    public enum InstantConditionType
    {
        AttackedEnemy, // 적을 타격한 직후
        KilledEnemy, // 적을 죽인 직후
        KilledBoss, //보스를 죽인 직후
        EnterredIntoBossStageEvent, //보스 스테이지 입장 직후
        BeingHittedByEnemy, //적에게 피격 직전 (아직 데미지를 입기 전)
        HittedByEnemy, // 적에게 피격된 직후 (이미 데미지를 입은 상황)
        Resurrected, //부활 직후
        OnLevelUp, // 레벨 업 시
        AcquiredHeart, // 하트로 HP 회복할 시
        OnSkillSetInitialized,  // 스킬 셋 초기화 직후
        OnBasicSkillUsed, //플레이어의 기본 공격이 사용된 후 (Activate일때 아님, Update에서 공격이 발동된 직후)
        KilledStunnedEnemy, //스턴 상태의 적을 죽인 직후
    }

    public struct BeingHittedResultData
    {
        // 방어막, 무적 등 등급효과에 의해 적의 공격을 무시한 경우 true
        public bool _isEnemyAttackIgnored;
        public float _calculatedDamage;

        public BeingHittedResultData(bool isEnemyAttackIgnored, float calculatedDamage)
        {
            this._isEnemyAttackIgnored = isEnemyAttackIgnored;
            this._calculatedDamage = calculatedDamage;
        }
    }


    public abstract class ConditionalEffectBase
    {
        public IReadOnlyList<InstantConditionType> SubscribingInstantConditions;

        public ConditionalEffectBase(IReadOnlyList<InstantConditionType> subscribingInstantConditions)
        {
            this.SubscribingInstantConditions = subscribingInstantConditions;
        }

        protected void MarkNotInterestedEvent(string methodName)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            Log.I.Warn($"{className}에 {methodName} 구현 안 되었는데 호출됨");
        }

        /// <summary>
        /// 캐릭터가 스테이지에 입장하고 조건을 확인해야하는 가장 이른 시점에 호출된다.
        /// 조건부 효과 동작을 위한 초기화를 구현해야 한다.
        /// 스킬 획득보다 먼저 실행되기때문에 스킬에 스탯을 재적용 하는 코드는 제외해도 된다.
        /// 게임 시작 시 스킬 셋에 영향을 주는 효과는 여기가 아닌 <see cref="OnSkillSetInitialized"/>에서 추가해야 한다.
        /// </summary>
        public virtual void Initialize(Stage stage, PlayerCharacter owner) { }
        /// <summary>
        /// 스테이지 내에서 조건을 확인하고 효과를 발동해야하는 동안 매 프레임 호출된다.
        /// </summary>
        public virtual void Update(Stage stage, PlayerCharacter owner) { }
        /// <summary>
        /// 캐릭터가 스테이지에서 퇴장하기 직전, 혹은 조건부효과가 캐릭터에게서 더 이상유효하지 않게되는 시점에 호출된다.
        /// 조건부 효과를 정리하는 동작을 구현해야 한다.
        /// </summary>
        public virtual void Destroy(Stage stage, PlayerCharacter owner) { }


        #region InstantConditionEvents
        public virtual void AttackedEnemy(Stage stage, PlayerCharacter owner, Character enemy, float damage)
        {
            MarkNotInterestedEvent(nameof(AttackedEnemy));
        }

        public virtual void KilledStunnedEnemy(Stage stage, PlayerCharacter owner, Character enemy)
        {
            MarkNotInterestedEvent(nameof(KilledStunnedEnemy));
        }

        public virtual void KilledEnemy(Stage stage, PlayerCharacter owner, Character enemy)
        {
            MarkNotInterestedEvent(nameof(KilledEnemy));
        }

        public virtual void KilledBoss(Stage stage, PlayerCharacter owner, Monster boss)
        {
            MarkNotInterestedEvent(nameof(KilledBoss));
        }

        public virtual void EnterredIntoBossStageEvent(Stage stage, PlayerCharacter owner, MonsterStaticData bossStaticData)
        {
            MarkNotInterestedEvent(nameof(EnterredIntoBossStageEvent));
        }

        public virtual BeingHittedResultData BeingHittedByEnemy(Stage stage, PlayerCharacter owner, Character enemy, float damage)
        {
            MarkNotInterestedEvent(nameof(BeingHittedByEnemy));
            return new BeingHittedResultData(true, 0f);
        }

        public virtual void HittedByEnemy(Stage stage, PlayerCharacter owner, Character enemy, float damage)
        {
            MarkNotInterestedEvent(nameof(HittedByEnemy));
        }

        public virtual void Resurrected(Stage stage, PlayerCharacter owner)
        {
            MarkNotInterestedEvent(nameof(Resurrected));
        }

        public virtual void OnLevelup(Stage stage, PlayerCharacter owner)
        {
            MarkNotInterestedEvent(nameof(OnLevelup));
        }

        public virtual void AcquiredHeart(Stage stage, PlayerCharacter owner)
        {
            MarkNotInterestedEvent(nameof(AcquiredHeart));
        }

        public virtual void OnSkillSetInitialized(Stage stage, PlayerCharacter owner)
        {
            MarkNotInterestedEvent(nameof(OnSkillSetInitialized));
        }

        public virtual void OnBasicSkillUsed(Stage stage, PlayerCharacter owner)
        {
            MarkNotInterestedEvent(nameof(OnBasicSkillUsed));
        }
        #endregion
    }

    /// <summary>
    /// 생명체에게 입힌 대미지 {0}%를 체력으로 회복
    /// </summary>
    public class RecoverHpWhenAttackedEnemy : ConditionalEffectBase
    {
        private float _hpRecoverRateByDamage;

        public RecoverHpWhenAttackedEnemy(float hpRecoverPercantageByDamage) :
            base(new List<InstantConditionType> { InstantConditionType.AttackedEnemy })
        {
            _hpRecoverRateByDamage = 0.01f * hpRecoverPercantageByDamage;
        }

        public override void AttackedEnemy(Stage stage, PlayerCharacter owner, Character enemy, float damage)
        {
            if (owner.MaxHP <= owner.CurrentHP)
            {
                return;
            }

            float recoverAmount = damage * _hpRecoverRateByDamage;

            // TODO : 회복 이벤트가 너무 많아질 것 같다.
            // 초단위로 지연처리할 필요가 있을 듯
            owner.RecoverHP(stage, recoverAmount);
        }
    }

    /// <summary>
    /// 생명체 {0}마리 처치시 체력을 {1} 회복
    /// </summary>
    public class RecoverHpOnKillEnemy : ConditionalEffectBase
    {
        private int _currentKillCount;
        private int _recoveryKillCount;
        private float _increment;

        public RecoverHpOnKillEnemy(int recoveryKillCount, float increment) :
            base(new List<InstantConditionType> { InstantConditionType.KilledEnemy })
        {
            _recoveryKillCount = recoveryKillCount;
            _increment = increment;
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            _currentKillCount = 0;
        }

        public override void KilledEnemy(Stage stage, PlayerCharacter owner, Character enemy)
        {
            _currentKillCount++;
            if (_currentKillCount % _recoveryKillCount == 0)
            {
                owner.RecoverHP(stage, _increment);
            }
        }
    }

    /// <summary>
    /// 적을 죽일 때마다 공격력을 증가시킨다, 내가 피격되면 초기화함
    /// 생명체 {0}마리 처치시 공격력 {1} 증가, 피격시 초기화
    /// </summary>
    public class StackAttackPowerOnKillEnemy : ConditionalEffectBase
    {
        private readonly int _maxStackCount;
        private readonly int _killCountToAddStack;
        private float _incrementPerStack;

        // 공격력을 증가시킨 횟수 (스택 몇개쌓였는지)
        private int _currentStackCount;
        // 이번 스택에서 카운팅한, 몬스터 죽인 횟수
        private int _currentKillCountOnStack;

        private StatModifier _addAttackPower;


        public StackAttackPowerOnKillEnemy(int killCountToAddStack, float incrementPerStack) :
            base(
                new List<InstantConditionType> { InstantConditionType.KilledEnemy, InstantConditionType.HittedByEnemy }
            )
        {
            _maxStackCount = 100;
            _killCountToAddStack = killCountToAddStack;
            _incrementPerStack = incrementPerStack;

            _currentStackCount = 0;
            _currentKillCountOnStack = 0;

            _addAttackPower = new StatModifier(0f, StatModType.Flat);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);

            _currentStackCount = 0;
            _currentKillCountOnStack = 0;

            owner.Stats.AttackPower.AddModifier(_addAttackPower);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);

            owner.Stats.AttackPower.RemoveModifier(_addAttackPower);
        }

        public override void KilledEnemy(Stage stage, PlayerCharacter owner, Character enemy)
        {
            if (_currentStackCount >= _maxStackCount)
            {
                return;
            }

            ++_currentKillCountOnStack;
            if (_currentKillCountOnStack < _killCountToAddStack)
            {
                return;
            }

            _currentKillCountOnStack = 0;
            ++_currentStackCount;

            owner.Stats.AttackPower.RemoveModifier(_addAttackPower);

            float increment = _incrementPerStack * _currentStackCount;

            _addAttackPower.ReInitializeValueForReusingStatModifier(increment);
            owner.Stats.AttackPower.AddModifier(_addAttackPower);
        }

        public override void HittedByEnemy(Stage stage, PlayerCharacter owner, Character enemy, float damage)
        {
            if (_currentStackCount <= 0)
            {
                return;
            }

            _currentStackCount = 0;
            _currentKillCountOnStack = 0;

            owner.Stats.AttackPower.RemoveModifier(_addAttackPower);

            _addAttackPower.ReInitializeValueForReusingStatModifier(0f);
            owner.Stats.AttackPower.AddModifier(_addAttackPower);

            return;
        }
    }

    public class AddMaxHp : ConditionalEffectBase
    {
        private readonly StatModifier _addMaxHp;

        public AddMaxHp(float maxHpIncrement) : base(new List<InstantConditionType>() { })
        {
            _addMaxHp = new StatModifier(maxHpIncrement, StatModType.Flat);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.MaxHP.AddModifier(_addMaxHp);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.MaxHP.RemoveModifier(_addMaxHp);
        }
    }

    public class MultiplyMaxHp : ConditionalEffectBase
    {
        private readonly StatModifier _multiplyMaxHp;

        public MultiplyMaxHp(float maxHpIncrementPercentage) : base(new List<InstantConditionType>() { })
        {
            float rateFromPercentage = maxHpIncrementPercentage * 0.01f;
            _multiplyMaxHp = new StatModifier(rateFromPercentage, StatModType.PercentAdd);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.MaxHP.AddModifier(_multiplyMaxHp);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.MaxHP.RemoveModifier(_multiplyMaxHp);
        }
    }

    public class AddAttackPower : ConditionalEffectBase
    {
        private readonly StatModifier _addAttackPower;

        public AddAttackPower(float attackPowerIncrement) : base(new List<InstantConditionType>() { })
        {
            _addAttackPower = new StatModifier(attackPowerIncrement, StatModType.Flat);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.AttackPower.AddModifier(_addAttackPower);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.AttackPower.RemoveModifier(_addAttackPower);
        }
    }


    public class MultiplyAttackPower : ConditionalEffectBase
    {
        private readonly StatModifier _multiplyAttackPower;

        public MultiplyAttackPower(float attackPowerIncrementPercentage) : base(new List<InstantConditionType>() { })
        {
            float rateFromPercentage = attackPowerIncrementPercentage * 0.01f;
            _multiplyAttackPower = new StatModifier(rateFromPercentage, StatModType.PercentAdd);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.AttackPower.AddModifier(_multiplyAttackPower);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.AttackPower.RemoveModifier(_multiplyAttackPower);
        }
    }


    public class AddCharacterAttackSpeed : ConditionalEffectBase
    {
        private StatModifier _addAttackSpeed;

        public AddCharacterAttackSpeed(float attackSpeedIncrement) : base(new List<InstantConditionType>() { })
        {

            _addAttackSpeed = new StatModifier(attackSpeedIncrement, StatModType.Flat);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.CharacterAttackSpeed.AddModifier(_addAttackSpeed);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.CharacterAttackSpeed.RemoveModifier(_addAttackSpeed);
        }
    }

    public class MultiplyCharacterAttackSpeed : ConditionalEffectBase
    {
        private StatModifier _multiplyAttackSpeed;

        public MultiplyCharacterAttackSpeed(float attackSpeedIncrementPercentage) : base(new List<InstantConditionType>() { })
        {
            float rateFromPercentage = attackSpeedIncrementPercentage * 0.01f;
            _multiplyAttackSpeed = new StatModifier(rateFromPercentage, StatModType.PercentAdd);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);

            owner.Stats.CharacterAttackSpeed.AddModifier(_multiplyAttackSpeed);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.CharacterAttackSpeed.RemoveModifier(_multiplyAttackSpeed);
        }
    }

    public class AddMoveSpeed : ConditionalEffectBase
    {
        private StatModifier _addMoveSpeed;

        public AddMoveSpeed(float increment) : base(new List<InstantConditionType>() { })
        {
            _addMoveSpeed = new StatModifier(increment, StatModType.Flat);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);

            owner.Stats.MoveSpeed.AddModifier(_addMoveSpeed);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.MoveSpeed.RemoveModifier(_addMoveSpeed);
        }
    }

    public class MultiplyMoveSpeed : ConditionalEffectBase
    {
        private StatModifier _multiplyMoveSpeed;

        public MultiplyMoveSpeed(float incrementPercentage) : base(new List<InstantConditionType>() { })
        {
            float rateFromPercentage = incrementPercentage * 0.01f;
            _multiplyMoveSpeed = new StatModifier(rateFromPercentage, StatModType.PercentAdd);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);

            owner.Stats.MoveSpeed.AddModifier(_multiplyMoveSpeed);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.MoveSpeed.RemoveModifier(_multiplyMoveSpeed);
        }
    }

    /// <summary>
    /// 체력이 {hpPercent}% 보다 낮으면 공격력이 {attackPowerIncrementPercentage}% 만큼 증가한다.
    /// </summary>
    public class MultiplyAttackPowerBelowHp : ConditionalEffectBase
    {
        private StatModifier _multiplyAttackPower;
        private float _hpPercent;
        private bool _isApplyStat;

        //InstantConditionType.HittedByEnemy 타입이 아닌 이유는 자연 회복이 있기때문에 Update에서 계속 체크한다.
        public MultiplyAttackPowerBelowHp(float hpPercent, float attackPowerIncrementPercentage) : base(new List<InstantConditionType>() { })
        {
            float rateFromPercentage = attackPowerIncrementPercentage * 0.01f;
            _hpPercent = hpPercent * 0.01f;
            _multiplyAttackPower = new StatModifier(rateFromPercentage, StatModType.PercentAdd);

        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            _isApplyStat = false;
        }

        public override void Update(Stage stage, PlayerCharacter owner)
        {
            base.Update(stage, owner);
            float hpPercent = owner.CurrentHP / owner.MaxHP;

            if (hpPercent <= _hpPercent)
            {
                if (_isApplyStat == false)
                {
                    _isApplyStat = true;
                    owner.Stats.AttackPower.AddModifier(_multiplyAttackPower);
                    owner.ReApplyStatsToAcquiredSkills(stage, owner.Stats);
                }
            }
            else
            {
                if (_isApplyStat == true)
                {
                    _isApplyStat = false;
                    owner.Stats.AttackPower.RemoveModifier(_multiplyAttackPower);
                    owner.ReApplyStatsToAcquiredSkills(stage, owner.Stats);
                }
            }
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            if (_isApplyStat == true)
            {
                _isApplyStat = false;
                owner.Stats.AttackPower.RemoveModifier(_multiplyAttackPower);
                owner.ReApplyStatsToAcquiredSkills(stage, owner.Stats);
            }
        }
    }

    /// <summary>
    /// 생명체 {0}마리 처치시 이동속도가 {1}% 증가, 피격시 초기화
    /// </summary>
    public class StackMultiplyMoveSpeedOnKillEnemy : ConditionalEffectBase
    {
        private readonly int _maxStackCount;
        private readonly int _killCountToAddStack;
        private float _incrementPerStackRate;

        // 이동속도를 증가시킨 횟수 (스택 몇개쌓였는지)
        private int _currentStackCount;
        // 이번 스택에서 카운팅한, 몬스터 죽인 횟수
        private int _currentKillCountOnStack;

        private StatModifier _addMoveSpeed;

        public StackMultiplyMoveSpeedOnKillEnemy(int killCountToAddStack, float incrementPerStack) :
            base(
                new List<InstantConditionType> { InstantConditionType.KilledEnemy, InstantConditionType.HittedByEnemy }
            )
        {
            _maxStackCount = 100;
            _killCountToAddStack = killCountToAddStack;
            _incrementPerStackRate = incrementPerStack * 0.01f;

            _currentStackCount = 0;
            _currentKillCountOnStack = 0;

            _addMoveSpeed = new StatModifier(0f, StatModType.PercentAdd);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);

            _currentStackCount = 0;
            _currentKillCountOnStack = 0;

            owner.Stats.MoveSpeed.AddModifier(_addMoveSpeed);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);

            owner.Stats.MoveSpeed.RemoveModifier(_addMoveSpeed);
        }

        public override void KilledEnemy(Stage stage, PlayerCharacter owner, Character enemy)
        {
            if (_currentStackCount >= _maxStackCount)
            {
                return;
            }

            ++_currentKillCountOnStack;
            if (_currentKillCountOnStack < _killCountToAddStack)
            {
                return;
            }

            _currentKillCountOnStack = 0;
            ++_currentStackCount;

            owner.Stats.MoveSpeed.RemoveModifier(_addMoveSpeed);

            float increment = _incrementPerStackRate * _currentStackCount;

            _addMoveSpeed.ReInitializeValueForReusingStatModifier(increment);
            owner.Stats.MoveSpeed.AddModifier(_addMoveSpeed);
        }

        public override void HittedByEnemy(Stage stage, PlayerCharacter owner, Character enemy, float damage)
        {
            if (_currentStackCount <= 0)
            {
                return;
            }

            _currentStackCount = 0;
            _currentKillCountOnStack = 0;

            owner.Stats.MoveSpeed.RemoveModifier(_addMoveSpeed);

            _addMoveSpeed.ReInitializeValueForReusingStatModifier(0f);
            owner.Stats.MoveSpeed.AddModifier(_addMoveSpeed);

            return;
        }
    }

    /// <summary>
    /// "{0}초간 공격력이 {1}%까지 점점 높아진다, 피격시 초기화"
    /// </summary>
    public class StackAttackPowerByTime : ConditionalEffectBase
    {
        private StatModifier _multiplyAttackPower;
        private float _incrementMaxTime;
        private float _incrementPercentage;
        private float _accumulatedTime;
        private bool _isMax;
        public StackAttackPowerByTime(float incrementMaxTime, float incrementPercentage) : base(new List<InstantConditionType>() { InstantConditionType.HittedByEnemy })
        {
            _incrementMaxTime = incrementMaxTime;
            _incrementPercentage = incrementPercentage * 0.01f;
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            _accumulatedTime = 0;
            _multiplyAttackPower = new StatModifier(_incrementPercentage * Mathf.Clamp01(_accumulatedTime / _incrementMaxTime), StatModType.PercentAdd);
            _isMax = false;
        }

        public override void Update(Stage stage, PlayerCharacter owner)
        {
            base.Update(stage, owner);
            _accumulatedTime += Time.deltaTime;

            if (_isMax)
            {
                return;
            }

            //이전 스탯 제거
            owner.Stats.AttackPower.RemoveModifier(_multiplyAttackPower);

            //최신 스탯 적용
            _multiplyAttackPower = new StatModifier(_incrementPercentage * Mathf.Clamp01(_accumulatedTime / _incrementMaxTime), StatModType.PercentAdd);
            owner.Stats.AttackPower.AddModifier(_multiplyAttackPower);

            //스킬에 스탯 업데이트
            owner.ReApplyStatsToAcquiredSkills(stage, owner.Stats);

            _isMax = _accumulatedTime / _incrementMaxTime > 1;
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            _isMax = false;
            owner.Stats.AttackPower.RemoveModifier(_multiplyAttackPower);
        }

        public override void HittedByEnemy(Stage stage, PlayerCharacter owner, Character enemy, float damage)
        {
            _isMax = false;
            _accumulatedTime = 0;
        }
    }

    public class MultiplyAttackRange : ConditionalEffectBase
    {
        private StatModifier _addAttackRange;

        public MultiplyAttackRange(float attackRangeIncrementPercentage) : base(new List<InstantConditionType>() { })
        {
            float ratePercentage = attackRangeIncrementPercentage * 0.01f;
            _addAttackRange = new StatModifier(ratePercentage, StatModType.PercentAdd);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.AttackRangeDistanceRatio.AddModifier(_addAttackRange);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.AttackRangeDistanceRatio.RemoveModifier(_addAttackRange);
        }
    }

    public class MultiplyKnockbackPower : ConditionalEffectBase
    {
        private StatModifier _addAttackKnockbackPower;

        public MultiplyKnockbackPower(float knockbackPowerIncrementPercentage) : base(new List<InstantConditionType>() { })
        {
            float percentageRate = knockbackPowerIncrementPercentage * 0.01f;
            _addAttackKnockbackPower = new StatModifier(percentageRate, StatModType.PercentAdd);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.CharacterAttackKnockBackPower.AddModifier(_addAttackKnockbackPower);
            owner.Stats.SkillAttackKnockBackPower.AddModifier(_addAttackKnockbackPower);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.CharacterAttackKnockBackPower.RemoveModifier(_addAttackKnockbackPower);
            owner.Stats.SkillAttackKnockBackPower.RemoveModifier(_addAttackKnockbackPower);
        }
    }

    //해당 기믹의 실제 구현은 tentisweepSkill 내부에 구현되어있다.
    //2초마다 기본공격의 {0}%의 데미지를 입히는 촉수를 소환하고 체력을{1}% 회복합니다
    public class HpAbsolbingTenticle : ConditionalEffectBase
    {
        private float _hpDrainAmountPercent;
        private float _attackDamagePercent;
        private float _attackPeriod;

        public HpAbsolbingTenticle(float attackDamagePercent, float hpDrainPercent) : base(new List<InstantConditionType>() { })
        {
            _hpDrainAmountPercent = hpDrainPercent * 0.01f;
            _attackDamagePercent = attackDamagePercent * 0.01f;
            _attackPeriod = 2f;
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.HpAbsolbingTenticle_HPDrainAmountPercent, _hpDrainAmountPercent);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.HpAbsolbingTenticle_DamagePercent, _attackDamagePercent);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.HpAbsolbingTenticle_AttackPeriod, _attackPeriod);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.HpAbsolbingTenticle_HPDrainAmountPercent, 0f);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.HpAbsolbingTenticle_DamagePercent, 0f);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.HpAbsolbingTenticle_AttackPeriod, -1);
        }
    }

    public class IgnatiaSS : ConditionalEffectBase
    {
        private float _stunDuration;

        public IgnatiaSS(float stunDuration) : base(new List<InstantConditionType>() { })
        {
            _stunDuration = stunDuration;
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.IgnatiaSS_StunDuration, _stunDuration);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.IgnatiaSS_StunDuration, 0f);
        }
    }
    public class DecreaseAttackPowerButIncreaseAttackSpeed : ConditionalEffectBase
    {
        private StatModifier _decreaseAttackPower;
        private StatModifier _increaseAttackSpeed;

        public DecreaseAttackPowerButIncreaseAttackSpeed(float decreaseAttackPowerPercentage, float increaseAttackSpeedPercentage) : base(new List<InstantConditionType>() { })
        {
            float decreaseAttackPowerRate = -decreaseAttackPowerPercentage * 0.01f;
            float increaseAttackSpeedRate = increaseAttackSpeedPercentage * 0.01f;

            _decreaseAttackPower = new StatModifier(decreaseAttackPowerRate, StatModType.PercentAdd);
            _increaseAttackSpeed = new StatModifier(increaseAttackSpeedRate, StatModType.PercentAdd);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.AttackPower.AddModifier(_decreaseAttackPower);
            owner.Stats.CharacterAttackSpeed.AddModifier(_increaseAttackSpeed);
            owner.Stats.SkillAttackSpeed.AddModifier(_increaseAttackSpeed);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.AttackPower.RemoveModifier(_decreaseAttackPower);
            owner.Stats.CharacterAttackSpeed.RemoveModifier(_increaseAttackSpeed);
            owner.Stats.SkillAttackSpeed.RemoveModifier(_increaseAttackSpeed);
        }
    }

    public class AddDamageReduction : ConditionalEffectBase
    {
        StatModifier _addDamageReduction;
        public AddDamageReduction(float addDamageRedction) : base(new List<InstantConditionType>() { })
        {
            float rateFromPercentage = addDamageRedction * 0.01f;
            _addDamageReduction = new StatModifier(rateFromPercentage, StatModType.Flat);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.DamageReduction.AddModifier(_addDamageReduction);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.DamageReduction.RemoveModifier(_addDamageReduction);
        }
    }

    public class MultiplyDamageReduction : ConditionalEffectBase
    {
        StatModifier _multiplyDamageReduction;
        public MultiplyDamageReduction(float multiplyDamageRedction) : base(new List<InstantConditionType>() { })
        {
            float rateFromPercentage = multiplyDamageRedction * 0.01f;
            _multiplyDamageReduction = new StatModifier(rateFromPercentage, StatModType.PercentAdd);
        }
        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.DamageReduction.AddModifier(_multiplyDamageReduction);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.DamageReduction.RemoveModifier(_multiplyDamageReduction);
        }
    }

    public class AddCharacterAttackKnockBackPower : ConditionalEffectBase
    {
        private StatModifier _addKnockbackPowerStatModifier;

        public AddCharacterAttackKnockBackPower(float addBasicSkillKnockBackPower) : base(new List<InstantConditionType>() { })
        {
            _addKnockbackPowerStatModifier = new StatModifier(addBasicSkillKnockBackPower, StatModType.Flat);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.CharacterAttackKnockBackPower.AddModifier(_addKnockbackPowerStatModifier);
        }
        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.CharacterAttackKnockBackPower.RemoveModifier(_addKnockbackPowerStatModifier);
        }
    }

    public class AddBurnEffectOnIgnitionWave : ConditionalEffectBase
    {
        private float _burnDuration;
        public AddBurnEffectOnIgnitionWave(float burnDuration) : base(new List<InstantConditionType>() { })
        {
            _burnDuration = burnDuration;
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.AddBurnEffectOnIgnitionWave_BurnDuration, _burnDuration);
        }
        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.AddBurnEffectOnIgnitionWave_BurnDuration, 0f);
        }
    }
    public class HalfOffSaleKetchup : ConditionalEffectBase
    {
        private float _decreasePercent;

        public HalfOffSaleKetchup(float decreasePercent) : base(new List<InstantConditionType>() { })
        {
            _decreasePercent = decreasePercent * 0.01f;
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.HalfOffSaleKetchup_TranscendentAttackCountDecreasePercent, _decreasePercent);
        }
        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.HalfOffSaleKetchup_TranscendentAttackCountDecreasePercent, 0f);
        }
    }

    public class RecoverHpWhenAttackByRedUmbrella : ConditionalEffectBase
    {
        private float _hpDrainPercent;
        private float _hpDrainAmountPercent;

        public RecoverHpWhenAttackByRedUmbrella(float hpDrainPercent, float hpDrainAmountPercent) : base(new List<InstantConditionType>() { })
        {
            _hpDrainPercent = hpDrainPercent * 0.01f;
            _hpDrainAmountPercent = hpDrainAmountPercent * 0.01f;
        }
        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.RecoverHpWhenAttackByRedUmbrella_HPDrainPercent, _hpDrainPercent);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.RecoverHpWhenAttackByRedUmbrella_HPDrainAmountPercent, _hpDrainAmountPercent);
        }
        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.RecoverHpWhenAttackByRedUmbrella_HPDrainPercent, 0f);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.RecoverHpWhenAttackByRedUmbrella_HPDrainAmountPercent, 0f);
        }
    }

    public class MultiplyBurnDamage : ConditionalEffectBase
    {
        private float _multiplyBurnDamagePercent;
        private float _prevBurnDamagePercent;

        public MultiplyBurnDamage(float multiplyBurnDamagePercent) : base(new List<InstantConditionType>() { })
        {
            _multiplyBurnDamagePercent = multiplyBurnDamagePercent * 0.01f;
        }
        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            _prevBurnDamagePercent = owner.CustomParameters.GetParameterValue(CustomParameterType.Ignatia_BurnDamagePercent);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.Ignatia_BurnDamagePercent, _prevBurnDamagePercent * (1 + _multiplyBurnDamagePercent));
        }
        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.Ignatia_BurnDamagePercent, _prevBurnDamagePercent);
        }
    }

    public class DoubleTheJackpot : ConditionalEffectBase
    {
        private float _prevMultiplyPercent;
        public DoubleTheJackpot() : base(new List<InstantConditionType>() { })
        {
        }
        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            _prevMultiplyPercent = owner.CustomParameters.GetParameterValue(CustomParameterType.DoubleTheJackpot_MultiplyJackpotPercent);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.DoubleTheJackpot_MultiplyJackpotPercent, 2);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.DoubleTheJackpot_MultiplyJackpotPercent, _prevMultiplyPercent);
        }
    }

    public class Dice777 : ConditionalEffectBase
    {
        public Dice777() : base(new List<InstantConditionType>() { })
        {
        }
        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.Dice777_ActivePercent, 0.07f);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.Dice777_Duration, 7f);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.Dice777_AddChipAmount, 7f);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.Dice777_ActivePercent, 0f);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.Dice777_Duration, 0f);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.Dice777_AddChipAmount, 0f);
        }
    }

    public class UpgradeAlphaGattling : ConditionalEffectBase
    {
        private float _correctionAngle;
        public UpgradeAlphaGattling(float correctionAngle) : base(new List<InstantConditionType>() { })
        {
            _correctionAngle = correctionAngle;
        }
        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.UpgradeAlphaGattling_CorrectionAngle, _correctionAngle);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.UpgradeAlphaGattling_CorrectionAngle, 0f);
        }
    }

    public class AddDodgeRate : ConditionalEffectBase
    {
        StatModifier _addDodgeStatModifer;
        private float _addDodgeRate;
        public AddDodgeRate(float addDodgeRate) : base(new List<InstantConditionType>() { })
        {
            _addDodgeRate = addDodgeRate * 0.01f;
            _addDodgeStatModifer = new StatModifier(_addDodgeRate, StatModType.Flat);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.DodgeRate.AddModifier(_addDodgeStatModifer);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.DodgeRate.RemoveModifier(_addDodgeStatModifer);
        }
    }

    public class AddAttackPowerOnKillBoss : ConditionalEffectBase
    {
        private StatModifier _addAttackPower;

        public AddAttackPowerOnKillBoss(float attackPowerIncrement) : base(new List<InstantConditionType>() { InstantConditionType.KilledBoss })
        {
            _addAttackPower = new StatModifier(attackPowerIncrement, StatModType.Flat);
        }

        public override void KilledBoss(Stage stage, PlayerCharacter owner, Monster boss)
        {
            owner.Stats.AttackPower.AddModifier(_addAttackPower);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.AttackPower.RemoveModifier(_addAttackPower);
        }
    }

    public class AddHPOnKillBoss : ConditionalEffectBase
    {
        private StatModifier _addMaxHp;

        public AddHPOnKillBoss(float maxHpIncrement) : base(new List<InstantConditionType>() { InstantConditionType.KilledBoss })
        {
            _addMaxHp = new StatModifier(maxHpIncrement, StatModType.Flat);
        }

        public override void KilledBoss(Stage stage, PlayerCharacter owner, Monster boss)
        {
            base.Initialize(stage, owner);
            owner.Stats.MaxHP.AddModifier(_addMaxHp);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.MaxHP.RemoveModifier(_addMaxHp);
        }
    }

    public class AddDamageReductionOnKillBoss : ConditionalEffectBase
    {
        StatModifier _addDamageReduction;
        public AddDamageReductionOnKillBoss(float addDamageRedction) : base(new List<InstantConditionType>() { InstantConditionType.KilledBoss })
        {
            float rateFromPercentage = addDamageRedction * 0.01f;
            _addDamageReduction = new StatModifier(rateFromPercentage, StatModType.Flat);
        }

        public override void KilledBoss(Stage stage, PlayerCharacter owner, Monster boss)
        {
            base.Initialize(stage, owner);
            owner.Stats.DamageReduction.AddModifier(_addDamageReduction);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.DamageReduction.RemoveModifier(_addDamageReduction);
        }
    }

    public class AddMoveSpeedOnKillBoss : ConditionalEffectBase
    {
        private StatModifier _addMoveSpeed;

        public AddMoveSpeedOnKillBoss(float increment) : base(new List<InstantConditionType>() { InstantConditionType.KilledBoss })
        {
            _addMoveSpeed = new StatModifier(increment, StatModType.Flat);
        }

        public override void KilledBoss(Stage stage, PlayerCharacter owner, Monster boss)
        {
            owner.Stats.MoveSpeed.AddModifier(_addMoveSpeed);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.MoveSpeed.RemoveModifier(_addMoveSpeed);
        }
    }
    public class AcquireShieldOnBossAppears : ConditionalEffectBase
    {
        private readonly int _shieldAmount;

        public AcquireShieldOnBossAppears(int shieldAmount) :
            base(new List<InstantConditionType>() { InstantConditionType.EnterredIntoBossStageEvent })
        {
            _shieldAmount = shieldAmount;
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.CreateShield();
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.DestroyShield();
        }

        public override void EnterredIntoBossStageEvent(Stage stage, PlayerCharacter owner, MonsterStaticData bossStaticData)
        {
            owner.Shield.IncreaseShieldAmount(_shieldAmount);
        }
    }

    /// <summary>
    /// 적 {0}마리 처치시 경험치 획득량 +{1}
    /// </summary>
    public class AddExpUpOnKillEnemy : ConditionalEffectBase
    {
        private StatModifier _addExpAmount;
        private int _incrementMonsterKillCount;
        private int _incrementExpAmount;

        private int _currentAddExpAmount;
        private int _currentMonsterKillCount;

        private int _maxStackCount;
        private int _currentStackCount;


        public AddExpUpOnKillEnemy(int incrementMonsterKillCount, int incrementExpAmount) :
                 base(new List<InstantConditionType>() { InstantConditionType.KilledEnemy })
        {
            _incrementMonsterKillCount = incrementMonsterKillCount;
            _incrementExpAmount = incrementExpAmount;
            _addExpAmount = new StatModifier(0, StatModType.Flat);
            _currentAddExpAmount = 0;
            _currentMonsterKillCount = 0;

            _maxStackCount = 100;
            _currentStackCount = 0;
        }

        public override void KilledEnemy(Stage stage, PlayerCharacter owner, Character enemy)
        {
            if (_currentStackCount >= _maxStackCount)
            {
                return;
            }

            _currentMonsterKillCount++;
            if (_currentMonsterKillCount >= _incrementMonsterKillCount)
            {
                _currentStackCount++;
                owner.Stats.AdditionalExpRate.RemoveModifier(_addExpAmount);

                _currentAddExpAmount += _incrementExpAmount;
                _addExpAmount.ReInitializeValueForReusingStatModifier(_currentAddExpAmount);

                owner.Stats.AdditionalExpRate.AddModifier(_addExpAmount);
                _currentMonsterKillCount -= _incrementMonsterKillCount;
            }
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.AdditionalExpRate.RemoveModifier(_addExpAmount);
        }
    }

    public class MultiplyAttackSpeedOnKillEnemy : ConditionalEffectBase
    {
        private StatModifier _addMultiplyAttackSpeed;
        private int _incrementMonsterKillCount;
        private float _incrementAttackSpeedRate;

        private int _currentAddAttackSpeedCount;
        private int _currentMonsterKillCount;
        private int _maxAddCount = 5;

        public MultiplyAttackSpeedOnKillEnemy(int incrementMonsterKillCount, float incrementAttackSpeedPercentage) :
                 base(new List<InstantConditionType>() { InstantConditionType.KilledEnemy })
        {
            _incrementMonsterKillCount = incrementMonsterKillCount;
            _incrementAttackSpeedRate = incrementAttackSpeedPercentage * 0.01f;
            _addMultiplyAttackSpeed = new StatModifier(0, StatModType.PercentAdd);
            _currentAddAttackSpeedCount = 0;
            _currentMonsterKillCount = 0;
        }

        public override void KilledEnemy(Stage stage, PlayerCharacter owner, Character enemy)
        {
            _currentMonsterKillCount++;
            if (_currentAddAttackSpeedCount < _maxAddCount &&
                _incrementMonsterKillCount <= _currentMonsterKillCount)
            {
                _currentAddAttackSpeedCount++;

                owner.Stats.CharacterAttackSpeed.RemoveModifier(_addMultiplyAttackSpeed);
                owner.Stats.SkillAttackSpeed.RemoveModifier(_addMultiplyAttackSpeed);

                _addMultiplyAttackSpeed.ReInitializeValueForReusingStatModifier(_incrementAttackSpeedRate * _currentAddAttackSpeedCount);

                owner.Stats.CharacterAttackSpeed.AddModifier(_addMultiplyAttackSpeed);
                owner.Stats.SkillAttackSpeed.AddModifier(_addMultiplyAttackSpeed);

                _currentMonsterKillCount -= _incrementMonsterKillCount;

                owner.ReApplyStatsToAcquiredSkills(stage, owner.Stats);
            }
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.CharacterAttackSpeed.RemoveModifier(_addMultiplyAttackSpeed);
            owner.Stats.SkillAttackSpeed.RemoveModifier(_addMultiplyAttackSpeed);
        }
    }


    public class AddAttackPowerOnWhenHit : ConditionalEffectBase
    {
        private float _incrementAttackPower;
        private float _currentAddPower;
        private float _maxAddPower;
        private StatModifier _addAttackPower;
        private bool _isMaxAddPower;

        public AddAttackPowerOnWhenHit(float incrementAttackPower, float maxAttackPower) :
            base(new List<InstantConditionType>() { InstantConditionType.HittedByEnemy })
        {
            _incrementAttackPower = incrementAttackPower;
            _maxAddPower = maxAttackPower;
            _currentAddPower = 0;
            _addAttackPower = new StatModifier(0, StatModType.Flat);
            _isMaxAddPower = false;
        }
        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.AttackPower.AddModifier(_addAttackPower);
        }

        public override void HittedByEnemy(Stage stage, PlayerCharacter owner, Character enemy, float damage)
        {
            if (_isMaxAddPower)
            {
                return;
            }

            if (_currentAddPower + _incrementAttackPower <= _maxAddPower)
            {
                owner.Stats.AttackPower.RemoveModifier(_addAttackPower);
                _currentAddPower += _incrementAttackPower;
                _addAttackPower.ReInitializeValueForReusingStatModifier(_currentAddPower);
                owner.Stats.AttackPower.AddModifier(_addAttackPower);
                owner.ReApplyStatsToAcquiredSkills(stage, owner.Stats);
            }
            else
            {
                owner.Stats.AttackPower.RemoveModifier(_addAttackPower);
                _addAttackPower.ReInitializeValueForReusingStatModifier(_maxAddPower);
                owner.Stats.AttackPower.AddModifier(_addAttackPower);
                owner.ReApplyStatsToAcquiredSkills(stage, owner.Stats);
                _isMaxAddPower = true;
            }
        }
        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.AttackPower.RemoveModifier(_addAttackPower);
        }
    }

    public class AddAttackSpeedOnWhenHit : ConditionalEffectBase
    {
        private float _incrementAttackSpeedAmount;
        private float _currentAttackSpeedAmount;
        private float _maxAttackSpeedAmount;
        private StatModifier _addAttackSpeedAmount;

        private bool _isMaxAttackSpeed;

        public AddAttackSpeedOnWhenHit(float incrementAttackSpeedAmount, float maxAttackSpeedAmount) :
            base(new List<InstantConditionType>() { InstantConditionType.HittedByEnemy })
        {
            _incrementAttackSpeedAmount = incrementAttackSpeedAmount;
            _maxAttackSpeedAmount = maxAttackSpeedAmount;
            _currentAttackSpeedAmount = 0;
            _addAttackSpeedAmount = new StatModifier(0, StatModType.Flat);
            _isMaxAttackSpeed = false;
        }
        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.CharacterAttackSpeed.AddModifier(_addAttackSpeedAmount);
            owner.Stats.SkillAttackSpeed.AddModifier(_addAttackSpeedAmount);
        }

        public override void HittedByEnemy(Stage stage, PlayerCharacter owner, Character enemy, float damage)
        {
            if (_isMaxAttackSpeed)
            {
                return;
            }

            if (_currentAttackSpeedAmount + _incrementAttackSpeedAmount <= _maxAttackSpeedAmount)
            {
                owner.Stats.CharacterAttackSpeed.RemoveModifier(_addAttackSpeedAmount);
                owner.Stats.SkillAttackSpeed.RemoveModifier(_addAttackSpeedAmount);

                _currentAttackSpeedAmount += _incrementAttackSpeedAmount;
                _addAttackSpeedAmount.ReInitializeValueForReusingStatModifier(_currentAttackSpeedAmount);

                owner.Stats.CharacterAttackSpeed.AddModifier(_addAttackSpeedAmount);
                owner.Stats.SkillAttackSpeed.AddModifier(_addAttackSpeedAmount);
                owner.ReApplyStatsToAcquiredSkills(stage, owner.Stats);
            }
            else
            {
                owner.Stats.CharacterAttackSpeed.RemoveModifier(_addAttackSpeedAmount);
                owner.Stats.SkillAttackSpeed.RemoveModifier(_addAttackSpeedAmount);
                _addAttackSpeedAmount.ReInitializeValueForReusingStatModifier(_maxAttackSpeedAmount);

                owner.Stats.CharacterAttackSpeed.AddModifier(_addAttackSpeedAmount);
                owner.Stats.SkillAttackSpeed.AddModifier(_addAttackSpeedAmount);
                owner.ReApplyStatsToAcquiredSkills(stage, owner.Stats);
                _isMaxAttackSpeed = true;
            }
        }
        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.CharacterAttackSpeed.RemoveModifier(_addAttackSpeedAmount);
            owner.Stats.SkillAttackSpeed.RemoveModifier(_addAttackSpeedAmount);
        }
    }

    /// <summary>
    /// 체력이 {hpPercent}% 보다 낮으면 공격속도가 {skillAttackSpeedIncrementPercentage}% 만큼 증가한다.
    /// </summary>
    public class MultiplyAttackSpeedBelowHp : ConditionalEffectBase
    {
        private StatModifier _multiplyAttackSpeed;
        private float _hpPercent;
        private bool _isApplyStat;

        //InstantConditionType.HittedByEnemy 타입이 아닌 이유는 자연 회복이 있기때문에 Update에서 계속 체크한다.
        public MultiplyAttackSpeedBelowHp(float hpPercent, float attackSpeedIncrementPercentage) : base(new List<InstantConditionType>() { })
        {
            float rateFromPercentage = attackSpeedIncrementPercentage * 0.01f;
            _hpPercent = hpPercent * 0.01f;
            _multiplyAttackSpeed = new StatModifier(rateFromPercentage, StatModType.PercentAdd);

        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            _isApplyStat = false;
        }

        public override void Update(Stage stage, PlayerCharacter owner)
        {
            base.Update(stage, owner);
            float hpPercent = owner.CurrentHP / owner.MaxHP;

            if (hpPercent <= _hpPercent)
            {
                if (_isApplyStat == false)
                {
                    _isApplyStat = true;
                    owner.Stats.CharacterAttackSpeed.AddModifier(_multiplyAttackSpeed);
                    owner.Stats.SkillAttackSpeed.AddModifier(_multiplyAttackSpeed);
                }
            }
            else
            {
                if (_isApplyStat == true)
                {
                    _isApplyStat = false;
                    owner.Stats.CharacterAttackSpeed.RemoveModifier(_multiplyAttackSpeed);
                    owner.Stats.SkillAttackSpeed.RemoveModifier(_multiplyAttackSpeed);
                }
            }
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            if (_isApplyStat == true)
            {
                _isApplyStat = false;
                owner.Stats.CharacterAttackSpeed.RemoveModifier(_multiplyAttackSpeed);
                owner.Stats.SkillAttackSpeed.RemoveModifier(_multiplyAttackSpeed);
            }
        }
    }

    public class BelowHPInvincibility : ConditionalEffectBase
    {
        private bool _isApply;
        private float _endAt;
        private float _hpPercent;
        private float _duration;

        StatModifier _incrementAttackPower;
        StatModifier _incrementAttackSpeed;
        StatModifier _incrementMoveSpeed;
        StatModifier _incrementDodgePercent;

        private const string _effectPath = "Stages/GradeEffects/ch_SpeedBuff.prefab";
        private SkeletonAnimation _effect;
        private MeshRenderer _effectMeshRenderer;

        public BelowHPInvincibility(float hpPercent, float duration) : base(new List<InstantConditionType>() { InstantConditionType.HittedByEnemy })
        {
            _isApply = false;
            _hpPercent = hpPercent * 0.01f;
            _duration = duration;

            _incrementAttackPower = new StatModifier(0.5f, StatModType.PercentAdd);
            _incrementAttackSpeed = new StatModifier(0.5f, StatModType.PercentAdd);
            _incrementMoveSpeed = new StatModifier(0.5f, StatModType.PercentAdd);
            _incrementDodgePercent = new StatModifier(1.0f, StatModType.Flat);
            _endAt = 0;
        }
        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            _effect = ResourcePool.Instance.InstantiateFromResource<SkeletonAnimation>(_effectPath);
            _effect.transform.SetParent(owner.transform);
            _effect.transform.localPosition = new Vector3(0, -0.25f, 0);
            _effect.transform.localScale = Vector3.one * 0.5f;
            _effectMeshRenderer = _effect.gameObject.GetComponent<MeshRenderer>();
            _effect.gameObject.SetActive(false);
        }

        //해당 등급효과는 체력이 _hpPercent 보다 낮을때 한번만 발동된다. 
        //체력이 감소되는 시점인 HittedByEnemy에서 처리한다.
        public override void HittedByEnemy(Stage stage, PlayerCharacter owner, Character enemy, float damage)
        {
            if (_isApply)
            {
                return;
            }

            float hpPercent = owner.CurrentHP / owner.MaxHP;
            if (hpPercent <= _hpPercent)
            {
                owner.Stats.AttackPower.AddModifier(_incrementAttackPower);
                owner.Stats.CharacterAttackSpeed.AddModifier(_incrementAttackSpeed);
                owner.Stats.SkillAttackSpeed.AddModifier(_incrementMoveSpeed);
                owner.Stats.MoveSpeed.AddModifier(_incrementMoveSpeed);
                owner.Stats.DodgeRate.AddModifier(_incrementDodgePercent);

                _effect.gameObject.SetActive(true);
                _effect.AnimationState.SetAnimation(0, "animation", true);
                _effectMeshRenderer.sortingOrder = (int)(owner.transform.position.y * -100.0f) - 1;
                _endAt = Time.time + _duration;
                _isApply = true;
            }
        }


        public override void Update(Stage stage, PlayerCharacter owner)
        {
            if (!_isApply)
            {
                return;
            }

            //활성화 중이면 이펙트를 계속 업데이트 해준다.
            if (_effect != null && _effectMeshRenderer != null)
            {
                _effectMeshRenderer.sortingOrder = (int)(owner.transform.position.y * -100.0f) - 1;
            }

            //지속시간 종료
            if (_endAt <= Time.time)
            {
                owner.Stats.AttackPower.RemoveModifier(_incrementAttackPower);
                owner.Stats.CharacterAttackSpeed.RemoveModifier(_incrementAttackSpeed);
                owner.Stats.SkillAttackSpeed.RemoveModifier(_incrementMoveSpeed);
                owner.Stats.MoveSpeed.RemoveModifier(_incrementMoveSpeed);
                owner.Stats.DodgeRate.RemoveModifier(_incrementDodgePercent);

                _effect.AnimationState.SetEmptyAnimation(0, 0);
                _effect.gameObject.SetActive(false);
                ResourcePool.Instance.PutBackInstance(_effectPath, _effect.gameObject);
                _effect = null;
                _effectMeshRenderer = null;

                _endAt = float.MaxValue;
            }
        }
        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            owner.Stats.AttackPower.RemoveModifier(_incrementAttackPower);
            owner.Stats.CharacterAttackSpeed.RemoveModifier(_incrementAttackSpeed);
            owner.Stats.SkillAttackSpeed.RemoveModifier(_incrementMoveSpeed);
            owner.Stats.MoveSpeed.RemoveModifier(_incrementMoveSpeed);
            owner.Stats.DodgeRate.RemoveModifier(_incrementDodgePercent);

            if (_effect != null)
            {
                ResourcePool.Instance.PutBackInstance(_effectPath, _effect.gameObject);
                _effect = null;
            }

            if (_effectMeshRenderer != null)
            {
                _effectMeshRenderer = null;
            }

        }
    }

    /// <summary>
    /// 체력이 {hpPercent}% 보다 낮으면 회피율이 {dodgeIncrementPercentage}% 만큼 증가한다.
    /// </summary>
    public class AddDodgeRateBelowHP : ConditionalEffectBase
    {
        private StatModifier _addDodgeRate;
        private float _hpPercent;
        private bool _isApplyStat;

        //InstantConditionType.HittedByEnemy 타입이 아닌 이유는 자연 회복이 있기때문에 Update에서 계속 체크한다.
        public AddDodgeRateBelowHP(float hpPercent, float dodgeIncrementPercentage) : base(new List<InstantConditionType>() { })
        {
            float rateFromPercentage = dodgeIncrementPercentage * 0.01f;
            _hpPercent = hpPercent * 0.01f;
            _addDodgeRate = new StatModifier(rateFromPercentage, StatModType.Flat);

        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            _isApplyStat = false;
        }

        public override void Update(Stage stage, PlayerCharacter owner)
        {
            base.Update(stage, owner);
            float hpPercent = owner.CurrentHP / owner.MaxHP;

            if (hpPercent <= _hpPercent)
            {
                if (_isApplyStat == false)
                {
                    _isApplyStat = true;
                    owner.Stats.DodgeRate.AddModifier(_addDodgeRate);
                }
            }
            else
            {
                if (_isApplyStat == true)
                {
                    _isApplyStat = false;
                    owner.Stats.DodgeRate.RemoveModifier(_addDodgeRate);
                }
            }
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            if (_isApplyStat == true)
            {
                _isApplyStat = false;
                owner.Stats.DodgeRate.RemoveModifier(_addDodgeRate);
            }
        }
    }

    public class BelowHPExtraHit : ConditionalEffectBase
    {
        private readonly float _additionalAttackDamagePercentRate;
        private readonly float _hpPercent;

        public BelowHPExtraHit(float hpPercent, float additionalAttackDamagePercentage) : base(new List<InstantConditionType>() { InstantConditionType.AttackedEnemy })
        {
            _additionalAttackDamagePercentRate = additionalAttackDamagePercentage * 0.01f;
            _hpPercent = hpPercent * 0.01f;
        }

        public override void AttackedEnemy(Stage stage, PlayerCharacter owner, Character enemy, float damage)
        {
            float hpPercent = owner.CurrentHP / owner.MaxHP;
            if (hpPercent < _hpPercent)
            {
                enemy.Hitted(stage, null, damage * _additionalAttackDamagePercentRate, Vector2.zero, enemy.Pos, null);
            }
        }
    }

    public class BelowHPInstantDeath : ConditionalEffectBase
    {
        private float _instantDeathPercentRate;
        private float _hpPercent;
        public BelowHPInstantDeath(float hpPercent, float instantDeathPercentage) : base(new List<InstantConditionType>() { InstantConditionType.AttackedEnemy })
        {
            _instantDeathPercentRate = instantDeathPercentage * 0.01f;
            _hpPercent = hpPercent * 0.01f;
        }

        public override void AttackedEnemy(Stage stage, PlayerCharacter owner, Character enemy, float damage)
        {
            //보스는 즉사하지 않는다.
            if (enemy.IsBoss)
            {
                return;
            }

            float hpPercent = owner.CurrentHP / owner.MaxHP;
            if (hpPercent <= _hpPercent)
            {
                float rnd = Random.value;
                if (rnd <= _instantDeathPercentRate)
                {
                    enemy.ForceKillSelf(stage);
                }
            }
        }
    }

    /// <summary>
    /// 체력이 {hpPercent}% 보다 낮으면 회복량이 {recoveryIncrementPercentage}% 만큼 증가한다.
    /// </summary>
    public class MultiplyRecoveryBelowHP : ConditionalEffectBase
    {
        private StatModifier _multiplyRecoveryRate;
        private float _hpPercent;
        private bool _isApplyStat;

        //InstantConditionType.HittedByEnemy 타입이 아닌 이유는 자연 회복이 있기때문에 Update에서 계속 체크한다.
        public MultiplyRecoveryBelowHP(float hpPercent, float recoveryIncrementPercentage) : base(new List<InstantConditionType>() { })
        {
            float rateFromPercentage = recoveryIncrementPercentage * 0.01f;
            _hpPercent = hpPercent * 0.01f;
            _multiplyRecoveryRate = new StatModifier(rateFromPercentage, StatModType.PercentAdd);

        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            _isApplyStat = false;
        }

        public override void Update(Stage stage, PlayerCharacter owner)
        {
            base.Update(stage, owner);
            float hpPercent = owner.CurrentHP / owner.MaxHP;

            if (hpPercent <= _hpPercent)
            {
                if (_isApplyStat == false)
                {
                    _isApplyStat = true;
                    owner.Stats.HPRecoveryRate.AddModifier(_multiplyRecoveryRate);
                }
            }
            else
            {
                if (_isApplyStat == true)
                {
                    _isApplyStat = false;
                    owner.Stats.HPRecoveryRate.RemoveModifier(_multiplyRecoveryRate);
                }
            }
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            if (_isApplyStat == true)
            {
                _isApplyStat = false;
                owner.Stats.HPRecoveryRate.RemoveModifier(_multiplyRecoveryRate);
            }
        }
    }
    /// <summary>
    /// 체력이 {hpPercent}% 보다 높으면 공격력이 {attackPowerIncrementPercentage}% 만큼 증가한다.
    /// </summary>
    public class MultiplyAttackPowerMoreHP : ConditionalEffectBase
    {
        private StatModifier _multiplyAttackPower;
        private float _hpPercent;
        private bool _isApplyStat;

        //InstantConditionType.HittedByEnemy 타입이 아닌 이유는 자연 회복이 있기때문에 Update에서 계속 체크한다.
        public MultiplyAttackPowerMoreHP(float hpPercent, float attackPowerIncrementPercentage) : base(new List<InstantConditionType>() { })
        {
            float rateFromPercentage = attackPowerIncrementPercentage * 0.01f;
            _hpPercent = hpPercent * 0.01f;
            _multiplyAttackPower = new StatModifier(rateFromPercentage, StatModType.PercentAdd);

        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            _isApplyStat = false;
        }

        public override void Update(Stage stage, PlayerCharacter owner)
        {
            base.Update(stage, owner);
            float hpPercent = owner.CurrentHP / owner.MaxHP;

            if (hpPercent >= _hpPercent)
            {
                if (_isApplyStat == false)
                {
                    _isApplyStat = true;
                    owner.Stats.AttackPower.AddModifier(_multiplyAttackPower);
                }
            }
            else
            {
                if (_isApplyStat == true)
                {
                    _isApplyStat = false;
                    owner.Stats.AttackPower.RemoveModifier(_multiplyAttackPower);
                }
            }
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            if (_isApplyStat == true)
            {
                _isApplyStat = false;
                owner.Stats.AttackPower.RemoveModifier(_multiplyAttackPower);
            }
        }
    }


    /// <summary>
    /// 체력이 {hpPercent}% 보다 높으면 이동속도가 {moveSpeedIncrementPercentage}% 만큼 증가한다.
    /// </summary>
    public class MultiplyMoveSpeedMoreHP : ConditionalEffectBase
    {
        private StatModifier _multiplyMoveSpeed;
        private float _hpPercent;
        private bool _isApplyStat;

        //InstantConditionType.HittedByEnemy 타입이 아닌 이유는 자연 회복이 있기때문에 Update에서 계속 체크한다.
        public MultiplyMoveSpeedMoreHP(float hpPercent, float moveSpeedIncrementPercentage) : base(new List<InstantConditionType>() { })
        {
            float rateFromPercentage = moveSpeedIncrementPercentage * 0.01f;
            _hpPercent = hpPercent * 0.01f;
            _multiplyMoveSpeed = new StatModifier(rateFromPercentage, StatModType.PercentAdd);

        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            _isApplyStat = false;
        }

        public override void Update(Stage stage, PlayerCharacter owner)
        {
            base.Update(stage, owner);
            float hpPercent = owner.CurrentHP / owner.MaxHP;

            if (hpPercent >= _hpPercent)
            {
                if (_isApplyStat == false)
                {
                    _isApplyStat = true;
                    owner.Stats.MoveSpeed.AddModifier(_multiplyMoveSpeed);
                }
            }
            else
            {
                if (_isApplyStat == true)
                {
                    _isApplyStat = false;
                    owner.Stats.MoveSpeed.RemoveModifier(_multiplyMoveSpeed);
                }
            }
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            if (_isApplyStat == true)
            {
                _isApplyStat = false;
                owner.Stats.MoveSpeed.RemoveModifier(_multiplyMoveSpeed);
            }
        }
    }


    /// <summary>
    /// 체력이 {hpPercent}% 보다 높으면 넉백이 {knockbackPowerIncrementPercentage}% 만큼 증가한다.
    /// </summary>
    public class MultiplyKnockBackMoreHP : ConditionalEffectBase
    {
        private StatModifier _multiplyKnockbackPower;
        private float _hpPercent;
        private bool _isApplyStat;

        //InstantConditionType.HittedByEnemy 타입이 아닌 이유는 자연 회복이 있기때문에 Update에서 계속 체크한다.
        public MultiplyKnockBackMoreHP(float hpPercent, float knockbackPowerIncrementPercentage) : base(new List<InstantConditionType>() { })
        {
            float rateFromPercentage = knockbackPowerIncrementPercentage * 0.01f;
            _hpPercent = hpPercent * 0.01f;
            _multiplyKnockbackPower = new StatModifier(rateFromPercentage, StatModType.PercentAdd);

        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            _isApplyStat = false;
        }

        public override void Update(Stage stage, PlayerCharacter owner)
        {
            base.Update(stage, owner);
            float hpPercent = owner.CurrentHP / owner.MaxHP;

            if (hpPercent >= _hpPercent)
            {
                if (_isApplyStat == false)
                {
                    _isApplyStat = true;
                    owner.Stats.CharacterAttackKnockBackPower.AddModifier(_multiplyKnockbackPower);
                    owner.Stats.SkillAttackKnockBackPower.AddModifier(_multiplyKnockbackPower);
                }
            }
            else
            {
                if (_isApplyStat == true)
                {
                    _isApplyStat = false;
                    owner.Stats.CharacterAttackKnockBackPower.RemoveModifier(_multiplyKnockbackPower);
                    owner.Stats.SkillAttackKnockBackPower.RemoveModifier(_multiplyKnockbackPower);
                }
            }
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            if (_isApplyStat == true)
            {
                _isApplyStat = false;
                owner.Stats.CharacterAttackKnockBackPower.RemoveModifier(_multiplyKnockbackPower);
                owner.Stats.SkillAttackKnockBackPower.RemoveModifier(_multiplyKnockbackPower);
            }
        }
    }

    /// <summary>
    /// 체력이 {hpPercent}% 보다 높으면 지속시간이{durationIncrementPercentage}% 만큼 증가한다.
    /// </summary>
    public class MultiplyDurationUpMoreHP : ConditionalEffectBase
    {
        private StatModifier _multiplyDurationIncreaseRate;
        private float _hpPercent;
        private bool _isApplyStat;

        //InstantConditionType.HittedByEnemy 타입이 아닌 이유는 자연 회복이 있기때문에 Update에서 계속 체크한다.
        public MultiplyDurationUpMoreHP(float hpPercent, float durationIncrementPercentage) : base(new List<InstantConditionType>() { })
        {
            float rateFromPercentage = durationIncrementPercentage * 0.01f;
            _hpPercent = hpPercent * 0.01f;
            _multiplyDurationIncreaseRate = new StatModifier(rateFromPercentage, StatModType.PercentAdd);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            _isApplyStat = false;
        }

        public override void Update(Stage stage, PlayerCharacter owner)
        {
            base.Update(stage, owner);
            float hpPercent = owner.CurrentHP / owner.MaxHP;

            if (hpPercent >= _hpPercent)
            {
                if (_isApplyStat == false)
                {
                    _isApplyStat = true;
                    owner.Stats.DurationIncreaseRate.AddModifier(_multiplyDurationIncreaseRate);
                }
            }
            else
            {
                if (_isApplyStat == true)
                {
                    _isApplyStat = false;
                    owner.Stats.DurationIncreaseRate.RemoveModifier(_multiplyDurationIncreaseRate);
                }
            }
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            if (_isApplyStat == true)
            {
                _isApplyStat = false;
                owner.Stats.DurationIncreaseRate.RemoveModifier(_multiplyDurationIncreaseRate);
            }
        }
    }

    //{shieldDelay} 초마다 피해량을 {shieldAmount}회 차단하는 방어막 획득
    public class AcquireShieldCertainInterval : ConditionalEffectBase
    {
        private readonly float _shieldDelay;
        private readonly int _shieldAmount;
        private float _increaseShieldAmountAt;

        public AcquireShieldCertainInterval(float shieldDelay, int shieldAmount) :
            base(new List<InstantConditionType>() { })
        {
            _shieldDelay = shieldDelay;
            _shieldAmount = shieldAmount;
            _increaseShieldAmountAt = 0;
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.CreateShield();
            _increaseShieldAmountAt = Time.time + _shieldDelay;
        }

        public override void Update(Stage stage, PlayerCharacter owner)
        {
            base.Update(stage, owner);
            float now = Time.time;
            if (_increaseShieldAmountAt <= now)
            {
                owner.Shield.IncreaseShieldAmount(_shieldAmount);
                _increaseShieldAmountAt = now + _shieldDelay;
            }
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.DestroyShield();
        }
    }

    //엘리트 처치시 화면안에 적에게 현재 공격력의 {damagePercentage}%의 피해를 가함
    public class FullAttackOnKillElite : ConditionalEffectBase
    {
        private float _damagePercentageRate;
        public FullAttackOnKillElite(float damagePercentage) :
            base(new List<InstantConditionType>() { InstantConditionType.KilledEnemy })
        {
            _damagePercentageRate = damagePercentage * 0.01f;
        }

        public override void KilledEnemy(Stage stage, PlayerCharacter owner, Character enemy)
        {
            if (enemy.IsElite)
            {
                float effectiveRange = 20f * GameClient.CameraController.OrthographicSize / 18f;
                float damage = owner.Stats.AttackPower.Value * _damagePercentageRate;
                CircularTargetArea areaAttack = new CircularTargetArea(owner.Pos, effectiveRange); // 폭탄 터지는 범위.

                List<Character> characters = new List<Character>();
                stage.FindAliveCharactersInArea(owner.Alliance.ToEnemyAlliance(), areaAttack, characters);
                foreach (Character character in characters)
                {
                    character.Hitted(stage, owner, damage, Vector2.zero, character.CenterPos, hitSoundPrefabPath: string.Empty);
                }
            }
        }
    }

    //공격력이 {decreaseAttackPowerPercentage}% 감소하는 대신, 피해량 감소가 {increaseDamageRedcutionPercentage}% 상승한다
    public class DecreaseAttackPowerButIncreaseDamageReduction : ConditionalEffectBase
    {
        private StatModifier _decreaseAttackPower;
        private StatModifier _increaseDamageReduction;

        public DecreaseAttackPowerButIncreaseDamageReduction(float decreaseAttackPowerPercentage, float increaseDamageRedcutionPercentage)
            : base(new List<InstantConditionType>() { })
        {
            float decreaseAttackPowerRate = decreaseAttackPowerPercentage * 0.01f * -1f;
            float increaseDamageReductionRate = increaseDamageRedcutionPercentage * 0.01f;

            _decreaseAttackPower = new StatModifier(decreaseAttackPowerRate, StatModType.PercentAdd);
            _increaseDamageReduction = new StatModifier(increaseDamageReductionRate, StatModType.Flat);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.AttackPower.AddModifier(_decreaseAttackPower);
            owner.Stats.DamageReduction.AddModifier(_increaseDamageReduction);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.AttackPower.RemoveModifier(_decreaseAttackPower);
            owner.Stats.DamageReduction.RemoveModifier(_increaseDamageReduction);
        }
    }

    //캐릭터 주변{slowRange}m안의 적의 이동속도를 {slowPercentage}% 감소한다.
    public class AmbientSlow : ConditionalEffectBase
    {
        private const string EFFECT_PATH = "Stages/GradeEffects/slow_eff.prefab";

        private readonly float _slowRange;
        private readonly float _moveSpeedChangeRate;
        private readonly List<Character> _slowCharacters;

        private SkeletonAnimation _effect;

        private float _lastCheckedAt = 0f;

        public AmbientSlow(float slowRange, float slowPercentage) : base(new List<InstantConditionType>() { })
        {
            _slowRange = slowRange;

            _moveSpeedChangeRate = (1f - slowPercentage * 0.01f);
            if (_moveSpeedChangeRate <= 0f)
            {
                _moveSpeedChangeRate = 0.01f;
            }
            if (_moveSpeedChangeRate > 1f)
            {
                _moveSpeedChangeRate = 1f;
            }

            _slowCharacters = new List<Character>();
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            _effect = ResourcePool.Instance.InstantiateFromResource<SkeletonAnimation>(EFFECT_PATH);
            _effect.transform.SetParent(owner.transform);
            _effect.transform.localPosition = Vector3.zero;
            _effect.transform.localScale = 0.8f * _slowRange * Vector3.one;
            _effect.gameObject.SetActive(true);

            // 투명화 옵션 없어도 더 색을 뺸다. 너무 칙칙함
            var originalEffectColor = _effect.Skeleton.GetColor();
            originalEffectColor.a *= 0.45f;
            _effect.Skeleton.SetColor(originalEffectColor);
            

            _lastCheckedAt = 0f;
        }

        public override void Update(Stage stage, PlayerCharacter owner)
        {
            float now = Time.time;

            if (now - _lastCheckedAt < 0.25f)
            {
                // 0.25초마다 체크한다.
                return;
            }

            _lastCheckedAt = now;

            CircularTargetArea areaAttack = new CircularTargetArea(owner.Pos, _slowRange);
            _slowCharacters.Clear();
            stage.FindAliveCharactersInArea(owner.Alliance.ToEnemyAlliance(), areaAttack, _slowCharacters);
            foreach (Character character in _slowCharacters)
            {
                character.StatusEffects.AddOrUpdateStatusEffect(
                        stage, character, StatusEffectType.SlowMove,
                        statusEffectKey: "AmbientSlow", duration: 1f, now, effectParameter1: _moveSpeedChangeRate);
            }
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            _slowCharacters.Clear();

            ResourcePool.Instance.PutBackInstance(EFFECT_PATH, _effect.gameObject);
            _effect = null;
        }
    }

    public class CleavageGum : ConditionalEffectBase
    {
        private int _miniGumAmount;
        private int _prevCustomParamterValue;
        public CleavageGum(int miniGumAmount) : base(new List<InstantConditionType>() { })
        {
            _miniGumAmount = miniGumAmount;
        }
        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.CleavageGum, _miniGumAmount);
        }
        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.CleavageGum, 0f);
        }
    }

    public class ChewingBag : ConditionalEffectBase
    {
        private readonly float _chewingBagBubbleAttackPowerRate = 0.0375f;
        private readonly float _chewingBagBubblePeriod = 0.2f;  //거품 생성 간격
        private readonly float _chewingBagBubbleDuration;
        private readonly float _moveSpeedChangeRate;

        private float _creatAt;
        private bool _isLeft;

        public ChewingBag(float duration, float slowPercentage) : base(new List<InstantConditionType>() { })
        {
            _chewingBagBubbleDuration = duration;
            _moveSpeedChangeRate = (1.0f - (slowPercentage * 0.01f));
            if (_moveSpeedChangeRate <= 0)
            {
                _moveSpeedChangeRate = 0.01f;
            }
            else if (_moveSpeedChangeRate > 1f)
            {
                _moveSpeedChangeRate = 1f;
            }


        }
        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            _creatAt = Time.time;
        }
        public override void Update(Stage stage, PlayerCharacter owner)
        {
            float now = Time.time;
            if (_creatAt <= now)
            {
                float damage = owner.Stats.AttackPower.Value * _chewingBagBubbleAttackPowerRate;
                stage.CreateChewingBagAreaEffectObject(owner, damage, _moveSpeedChangeRate, _chewingBagBubbleDuration, _isLeft);
                _isLeft = !_isLeft;
                _creatAt = now + _chewingBagBubblePeriod;
            }
        }
    }

    // 게임 시작 시 기본 체력의 {0}%의 체력 버퍼 생성
    public class CreateHPBuffer : ConditionalEffectBase
    {
        private readonly float _maxHpPercentage;

        public CreateHPBuffer(float maxHpPercentage) : base(new List<InstantConditionType>() { })
        {
            _maxHpPercentage = 0.01f * maxHpPercentage;
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.CreateHPBuffer(_maxHpPercentage);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.DestroyHPBuffer();
        }
    }

    // 생명체 {0}마리 처치할 때마다 체력 버퍼의 {1}% 충전
    public class ChargeHPBufferOnKillEnemy : ConditionalEffectBase
    {
        private readonly int _targetKillCount;
        private readonly float _chargePercentage;

        private int _currentKillCount;

        public ChargeHPBufferOnKillEnemy(int targetKillCount, float chargePercentage)
            : base(new List<InstantConditionType> { InstantConditionType.KilledEnemy })
        {
            _targetKillCount = targetKillCount;
            _chargePercentage = 0.01f * chargePercentage;

            _currentKillCount = 0;
        }

        public override void KilledEnemy(Stage stage, PlayerCharacter owner, Character enemy)
        {
            ++_currentKillCount;
            if (_currentKillCount >= _targetKillCount)
            {
                _currentKillCount = 0;
                owner.HPBuffer?.ChargeHP(_chargePercentage * owner.HPBuffer.MaxHP.Value);
            }
        }
    }

    // 생명체 {0}마리 처치할 때마다 이동 속도 {1} 증가(최대 0.8)
    public class IncreaseMoveSpeedOnKillEnemy : ConditionalEffectBase
    {
        private static readonly float MAX_INCREASE_AMOUNT = 0.8f;

        private readonly int _targetKillCount;
        private readonly float _moveSpeedIncreaseAmount;
        private readonly StatModifier _increaseMoveSpeed;

        private int _currentKillCount;

        public IncreaseMoveSpeedOnKillEnemy(int targetKillCount, float moveSpeedIncreaseAmount)
            : base(new List<InstantConditionType> { InstantConditionType.KilledEnemy })
        {
            _targetKillCount = targetKillCount;
            _moveSpeedIncreaseAmount = moveSpeedIncreaseAmount;
            _increaseMoveSpeed = new StatModifier(0.0f, StatModType.Flat);

            _currentKillCount = 0;
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.MoveSpeed.AddModifier(_increaseMoveSpeed);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.MoveSpeed.RemoveModifier(_increaseMoveSpeed);
        }

        public override void KilledEnemy(Stage stage, PlayerCharacter owner, Character enemy)
        {
            if (_increaseMoveSpeed.Value >= MAX_INCREASE_AMOUNT)
            {
                return;
            }

            ++_currentKillCount;
            if (_currentKillCount >= _targetKillCount)
            {
                _currentKillCount = 0;
                owner.Stats.MoveSpeed.RemoveModifier(_increaseMoveSpeed);
                _increaseMoveSpeed.ReInitializeValueForReusingStatModifier(_increaseMoveSpeed.Value + _moveSpeedIncreaseAmount);
                owner.Stats.MoveSpeed.AddModifier(_increaseMoveSpeed);
            }
        }
    }

    // 이동 속도가 {0} 이상일 경우 회피율 {1}% 증가
    public class IncreaseDodgeRateOnHighMoveSpeed : ConditionalEffectBase
    {
        private readonly float _moveSpeedThresholdAmount;
        private readonly StatModifier _increaseDodgeRate;

        private bool _isActive;

        public IncreaseDodgeRateOnHighMoveSpeed(float moveSpeedThresholdAmount, float dodgeRateIncreaseAmount) : base(new List<InstantConditionType>() { })
        {
            _moveSpeedThresholdAmount = moveSpeedThresholdAmount;
            _increaseDodgeRate = new StatModifier(0.01f * dodgeRateIncreaseAmount, StatModType.Flat);

            _isActive = false;
        }

        public override void Update(Stage stage, PlayerCharacter owner)
        {
            base.Update(stage, owner);
            if (owner.Stats.MoveSpeed.Value >= _moveSpeedThresholdAmount)
            {
                if (!_isActive)
                {
                    owner.Stats.DodgeRate.AddModifier(_increaseDodgeRate);
                    _isActive = true;
                }
            }
            else
            {
                if (_isActive)
                {
                    owner.Stats.DodgeRate.RemoveModifier(_increaseDodgeRate);
                    _isActive = false;
                }
            }
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.DodgeRate.RemoveModifier(_increaseDodgeRate);
            _isActive = false;
        }
    }

    // 방어막 또는 체력 버퍼 활성화 시 공격 속도 +{0}%
    public class IncreaseAttackSpeedOnShieldOrHPBufferActive : ConditionalEffectBase
    {
        private readonly StatModifier _increaseAttackSpeed;

        private bool _isActive;

        public IncreaseAttackSpeedOnShieldOrHPBufferActive(float attackSpeedIncreaseRate) : base(new List<InstantConditionType>() { })
        {
            _increaseAttackSpeed = new StatModifier(0.01f * attackSpeedIncreaseRate, StatModType.PercentAdd);

            _isActive = false;
        }

        public override void Update(Stage stage, PlayerCharacter owner)
        {
            base.Update(stage, owner);
            if (owner.IsShieldActive || owner.IsHPBufferActive)
            {
                if (!_isActive)
                {
                    owner.Stats.CharacterAttackSpeed.AddModifier(_increaseAttackSpeed);
                    owner.Stats.SkillAttackSpeed.AddModifier(_increaseAttackSpeed);
                    _isActive = true;
                }
            }
            else
            {
                if (_isActive)
                {
                    owner.Stats.CharacterAttackSpeed.RemoveModifier(_increaseAttackSpeed);
                    owner.Stats.SkillAttackSpeed.RemoveModifier(_increaseAttackSpeed);
                    _isActive = false;
                }
            }
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.CharacterAttackSpeed.RemoveModifier(_increaseAttackSpeed);
            owner.Stats.SkillAttackSpeed.RemoveModifier(_increaseAttackSpeed);
            _isActive = false;
        }
    }

    // 방어막 또는 체력 버퍼 활성화 시 회복량 +{0}%
    public class IncreaseHPRecoveryRateOnShieldOrHPBufferActive : ConditionalEffectBase
    {
        private readonly StatModifier _increaseHPRecovery;

        private bool _isActive;

        public IncreaseHPRecoveryRateOnShieldOrHPBufferActive(float hpRecoveryIncreaseRate) : base(new List<InstantConditionType>() { })
        {
            _increaseHPRecovery = new StatModifier(0.01f * hpRecoveryIncreaseRate, StatModType.PercentAdd);

            _isActive = false;
        }

        public override void Update(Stage stage, PlayerCharacter owner)
        {
            base.Update(stage, owner);
            if (owner.IsShieldActive || owner.IsHPBufferActive)
            {
                if (!_isActive)
                {
                    owner.Stats.HPRecoveryRate.AddModifier(_increaseHPRecovery);
                    _isActive = true;
                }
            }
            else
            {
                if (_isActive)
                {
                    owner.Stats.HPRecoveryRate.RemoveModifier(_increaseHPRecovery);
                    _isActive = false;
                }
            }
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.HPRecoveryRate.RemoveModifier(_increaseHPRecovery);
            _isActive = false;
        }
    }

    // 레벨 업 할 때마다 최대 체력의 {0}% 회복
    public class RecoverHPOnLevelUp : ConditionalEffectBase
    {
        private readonly float _maxHpPercentage;

        public RecoverHPOnLevelUp(float maxHpPercentage)
            : base(new List<InstantConditionType>() { InstantConditionType.OnLevelUp })
        {
            _maxHpPercentage = 0.01f * maxHpPercentage;
        }

        public override void OnLevelup(Stage stage, PlayerCharacter owner)
        {
            owner.RecoverHP(stage, _maxHpPercentage * owner.MaxHP);
        }
    }

    // 크리티컬 확률 +{0}% 증가
    public class IncreaseCriticalChance : ConditionalEffectBase
    {
        private readonly StatModifier _increaseCriticalChance;

        public IncreaseCriticalChance(float criticalChanceIncreaseAmount) : base(new List<InstantConditionType>() { })
        {
            _increaseCriticalChance = new StatModifier(0.01f * criticalChanceIncreaseAmount, StatModType.Flat);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.CriticalChance.AddModifier(_increaseCriticalChance);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.CriticalChance.RemoveModifier(_increaseCriticalChance);
        }
    }

    // 경험치 획득량 +{0}%
    public class IncreaseExpIncreaseRate : ConditionalEffectBase
    {
        private readonly StatModifier _increaseExpIncreaseRate;

        public IncreaseExpIncreaseRate(float expIcreaseRateIncreaseAmount) : base(new List<InstantConditionType>() { })
        {
            _increaseExpIncreaseRate = new StatModifier(0.01f * expIcreaseRateIncreaseAmount, StatModType.Flat);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.ExpIncreaseRate.AddModifier(_increaseExpIncreaseRate);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.ExpIncreaseRate.RemoveModifier(_increaseExpIncreaseRate);
        }
    }

    // 레벨 {0} 달성 시 공격 속도 +{0}%
    public class IncreaseAttackSpeedOnLevelUp : ConditionalEffectBase
    {
        private readonly int _targetLevel;
        private readonly StatModifier _increaseAttackSpeed;

        public IncreaseAttackSpeedOnLevelUp(int targetLevel, float attackSpeedIncreaseRate)
            : base(new List<InstantConditionType>() { InstantConditionType.OnLevelUp })
        {
            _targetLevel = targetLevel;
            _increaseAttackSpeed = new StatModifier(0.01f * attackSpeedIncreaseRate, StatModType.PercentAdd);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.CharacterAttackSpeed.RemoveModifier(_increaseAttackSpeed);
            owner.Stats.SkillAttackSpeed.RemoveModifier(_increaseAttackSpeed);
        }

        public override void OnLevelup(Stage stage, PlayerCharacter owner)
        {
            if (owner.Level == _targetLevel)
            {
                owner.Stats.CharacterAttackSpeed.AddModifier(_increaseAttackSpeed);
                owner.Stats.SkillAttackSpeed.AddModifier(_increaseAttackSpeed);
            }
        }
    }

    // 레벨 {0} 달성 시 공격 범위 +{0}%
    public class IncreaseAttackRangeDistanceRatioOnLevelUp : ConditionalEffectBase
    {
        private readonly int _targetLevel;
        private readonly StatModifier _increaseAttackRangeDistanceRatio;

        public IncreaseAttackRangeDistanceRatioOnLevelUp(int targetLevel, float attackRangeDistanceRatioIncreaseRate)
            : base(new List<InstantConditionType>() { InstantConditionType.OnLevelUp })
        {
            _targetLevel = targetLevel;
            _increaseAttackRangeDistanceRatio = new StatModifier(0.01f * attackRangeDistanceRatioIncreaseRate, StatModType.PercentAdd);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.AttackRangeDistanceRatio.RemoveModifier(_increaseAttackRangeDistanceRatio);
        }

        public override void OnLevelup(Stage stage, PlayerCharacter owner)
        {
            if (owner.Level == _targetLevel)
            {
                owner.Stats.AttackRangeDistanceRatio.AddModifier(_increaseAttackRangeDistanceRatio);
            }
        }
    }

    // 체력 회복량 +{0}%
    public class IncreaseHPRecoveryRate : ConditionalEffectBase
    {
        private readonly StatModifier _increaseHPRecoveryRate;

        public IncreaseHPRecoveryRate(float hpRecoveryRateIncreaseAmount) : base(new List<InstantConditionType>() { })
        {
            _increaseHPRecoveryRate = new StatModifier(0.01f * hpRecoveryRateIncreaseAmount, StatModType.Flat);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.HPRecoveryRate.AddModifier(_increaseHPRecoveryRate);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.HPRecoveryRate.RemoveModifier(_increaseHPRecoveryRate);
        }
    }

    // 게임 당 1번, 체력 {0}% 이하일 때 5초 동안 최대 체력의 {1}% 회복
    public class RecoverHpOnLowHpOnlyOnce : ConditionalEffectBase
    {
        private enum State { Ready, Recovering, Used }

        private static readonly float RECOVERY_TIME = 5.0f;
        private static readonly float RECOVERY_INTERVAL = 0.5f;

        private readonly float _hpThresholdRate;
        private readonly float _maxHpPercentage;

        private float _recoveringAt;
        private float _recoveryEndsAt;
        private State _state;

        public RecoverHpOnLowHpOnlyOnce(float hpThresholdRate, float maxHpPercentage)
            : base(new List<InstantConditionType>() { InstantConditionType.HittedByEnemy })
        {
            _hpThresholdRate = 0.01f * hpThresholdRate;
            _maxHpPercentage = 0.01f * maxHpPercentage;

            _recoveringAt = 0.0f;
            _recoveryEndsAt = 0.0f;
            _state = State.Ready;
        }

        public override void Update(Stage stage, PlayerCharacter owner)
        {
            base.Update(stage, owner);

            if (_state != State.Recovering)
            {
                return;
            }

            float now = Time.time;

            if (_recoveryEndsAt < now)
            {
                _state = State.Used;
                return;
            }

            if (now < _recoveringAt)
            {
                return;
            }

            float recoveryAmountPerSecond = _maxHpPercentage * owner.MaxHP / RECOVERY_TIME;
            owner.RecoverHP(stage, RECOVERY_INTERVAL * recoveryAmountPerSecond);
            _recoveringAt += RECOVERY_INTERVAL;
        }

        public override void HittedByEnemy(Stage stage, PlayerCharacter owner, Character enemy, float damage)
        {
            if (_state != State.Ready)
            {
                return;
            }

            if (owner.CurrentHP <= _hpThresholdRate * owner.MaxHP)
            {
                _recoveringAt = Time.time;
                _recoveryEndsAt = _recoveringAt + RECOVERY_TIME;
                _state = State.Recovering;
            }
        }
    }

    // 체력 {0}% 이상일 경우 받는 피해량 {1}% 감소
    public class IncreaseDamageReductionOnHighHp : ConditionalEffectBase
    {
        private readonly float _hpThresholdRate;
        private readonly StatModifier _increaseDamageReduction;

        private bool _isActive;

        public IncreaseDamageReductionOnHighHp(float hpThresholdRate, float damageReductionIncreaseAmount) : base(new List<InstantConditionType>() { })
        {
            _hpThresholdRate = 0.01f * hpThresholdRate;
            _increaseDamageReduction = new StatModifier(0.01f * damageReductionIncreaseAmount, StatModType.Flat);

            _isActive = false;
        }

        public override void Update(Stage stage, PlayerCharacter owner)
        {
            base.Update(stage, owner);
            if (owner.CurrentHP >= _hpThresholdRate * owner.MaxHP)
            {
                if (!_isActive)
                {
                    owner.Stats.DamageReduction.AddModifier(_increaseDamageReduction);
                    _isActive = true;
                }
            }
            else
            {
                if (_isActive)
                {
                    owner.Stats.DamageReduction.RemoveModifier(_increaseDamageReduction);
                    _isActive = false;
                }
            }
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.DamageReduction.RemoveModifier(_increaseDamageReduction);
            _isActive = false;
        }
    }

    // 레벨 업 할 때마다 거리 {0} 이내의 주변 생명체에게 공격력의 {1}% 대미지
    public class AttackEnemiesNearCharacterOnLevelUp : ConditionalEffectBase
    {
        private const string EFFECT_PATH = "Stages/GradeEffects/lvup.prefab";

        private readonly float _attackRadius;
        private readonly float _attackPowerPercentage;
        private readonly List<Character> _enemies;

        private SkeletonAnimation _effect;

        public AttackEnemiesNearCharacterOnLevelUp(float attackRadius, float attackPowerPercentage)
            : base(new List<InstantConditionType>() { InstantConditionType.OnLevelUp })
        {
            _attackRadius = attackRadius;
            _attackPowerPercentage = 0.01f * attackPowerPercentage;
            _enemies = new List<Character>();
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            _effect = ResourcePool.Instance.InstantiateFromResource<SkeletonAnimation>(EFFECT_PATH);
            _effect.transform.SetParent(owner.transform);
            _effect.transform.localPosition = Vector3.zero;
            _effect.transform.localScale = 0.3f * _attackRadius * Vector3.one;
            _effect.gameObject.SetActive(false);
        }

        public override void OnLevelup(Stage stage, PlayerCharacter owner)
        {
            var targetArea = new CircularTargetArea(owner.Pos, _attackRadius);
            stage.FindAliveCharactersInArea(owner.Alliance.ToEnemyAlliance(), targetArea, _enemies);
            foreach (var enemy in _enemies)
            {
                enemy.Hitted(stage, null, _attackPowerPercentage * owner.Stats.AttackPower.Value, Vector2.zero, enemy.Pos, null);
            }

            _effect.gameObject.SetActive(true);
            _effect.AnimationState.SetAnimation(0, "animation", false);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            ResourcePool.Instance.PutBackInstance(EFFECT_PATH, _effect.gameObject);
            _effect = null;
        }
    }

    // 생명체 {0}마리 처치할 때마다 받는 피해량 {1}% 감소(최대 10%)
    public class IncreaseDamageReductionOnKillEnemy : ConditionalEffectBase
    {
        private static readonly float MAX_INCREASE_AMOUNT = 0.1f;

        private readonly int _targetKillCount;
        private readonly float _damageReductionIncreaseAmount;
        private readonly StatModifier _increaseDamageReduction;

        private int _currentKillCount;

        public IncreaseDamageReductionOnKillEnemy(int targetKillCount, float damageReductionIncreaseAmount)
            : base(new List<InstantConditionType>() { InstantConditionType.KilledEnemy })
        {
            _targetKillCount = targetKillCount;
            _damageReductionIncreaseAmount = 0.01f * damageReductionIncreaseAmount;
            _increaseDamageReduction = new StatModifier(0.0f, StatModType.Flat);

            _currentKillCount = 0;
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.DamageReduction.AddModifier(_increaseDamageReduction);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.DamageReduction.RemoveModifier(_increaseDamageReduction);
        }

        public override void KilledEnemy(Stage stage, PlayerCharacter owner, Character enemy)
        {
            if (_increaseDamageReduction.Value >= MAX_INCREASE_AMOUNT)
            {
                return;
            }

            ++_currentKillCount;
            if (_currentKillCount >= _targetKillCount)
            {
                _currentKillCount = 0;
                owner.Stats.DamageReduction.RemoveModifier(_increaseDamageReduction);
                _increaseDamageReduction.ReInitializeValueForReusingStatModifier(_increaseDamageReduction.Value + _damageReductionIncreaseAmount);
                owner.Stats.DamageReduction.AddModifier(_increaseDamageReduction);
            }
        }
    }

    // 하트 획득할 때마다 경험치 획득량 {0}% 증가(최대 {1}%)
    public class IncreaseExpIncreaseRateOnAcquiredHeart : ConditionalEffectBase
    {
        private readonly float _expIncreaseRateIncreaseAmount;
        private readonly float _maxIncreaseAmount;
        private readonly StatModifier _increaseExpIncreaseRate;

        public IncreaseExpIncreaseRateOnAcquiredHeart(float expIncreaseRateIncreaseAmount, float maxIncreaseAmount)
            : base(new List<InstantConditionType> { InstantConditionType.AcquiredHeart })
        {
            _expIncreaseRateIncreaseAmount = 0.01f * expIncreaseRateIncreaseAmount;
            _maxIncreaseAmount = 0.01f * maxIncreaseAmount;
            _increaseExpIncreaseRate = new StatModifier(0.0f, StatModType.Flat);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.ExpIncreaseRate.AddModifier(_increaseExpIncreaseRate);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.ExpIncreaseRate.RemoveModifier(_increaseExpIncreaseRate);
        }

        public override void AcquiredHeart(Stage stage, PlayerCharacter owner)
        {
            if (_increaseExpIncreaseRate.Value >= _maxIncreaseAmount)
            {
                return;
            }

            owner.Stats.ExpIncreaseRate.RemoveModifier(_increaseExpIncreaseRate);
            _increaseExpIncreaseRate.ReInitializeValueForReusingStatModifier(_increaseExpIncreaseRate.Value + _expIncreaseRateIncreaseAmount);
            owner.Stats.ExpIncreaseRate.AddModifier(_increaseExpIncreaseRate);
        }
    }

    // {0}초마다 최대 체력의 {1}% 회복
    public class RecoverHpPerTimeInterval : ConditionalEffectBase
    {
        private readonly float _timeInterval;
        private readonly float _maxHpPercentage;

        private float _recoveringAt;

        public RecoverHpPerTimeInterval(float timeInterval, float maxHpPercentage) : base(new List<InstantConditionType>() { })
        {
            _timeInterval = timeInterval;
            _maxHpPercentage = 0.01f * maxHpPercentage;

            _recoveringAt = Time.time + _timeInterval;
        }

        public override void Update(Stage stage, PlayerCharacter owner)
        {
            base.Update(stage, owner);

            if (Time.time < _recoveringAt)
            {
                return;
            }

            owner.RecoverHP(stage, _maxHpPercentage * owner.MaxHP);
            _recoveringAt += _timeInterval;
        }
    }

    // 하트 획득할 때마다 회피율 {0}% 증가(최대 {1}%)
    public class IncreaseDodgeRateOnAcquiredHeart : ConditionalEffectBase
    {
        private readonly float _dodgeRateIncreaseAmount;
        private readonly float _maxIncreaseAmount;
        private readonly StatModifier _increaseDodgeRate;

        public IncreaseDodgeRateOnAcquiredHeart(float dodgeRateIncreaseAmount, float maxIncreaseAmount)
            : base(new List<InstantConditionType> { InstantConditionType.AcquiredHeart })
        {
            _dodgeRateIncreaseAmount = 0.01f * dodgeRateIncreaseAmount;
            _maxIncreaseAmount = 0.01f * maxIncreaseAmount;
            _increaseDodgeRate = new StatModifier(0.0f, StatModType.Flat);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.DodgeRate.AddModifier(_increaseDodgeRate);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.DodgeRate.RemoveModifier(_increaseDodgeRate);
        }

        public override void AcquiredHeart(Stage stage, PlayerCharacter owner)
        {
            if (_increaseDodgeRate.Value >= _maxIncreaseAmount)
            {
                return;
            }

            owner.Stats.DodgeRate.RemoveModifier(_increaseDodgeRate);
            _increaseDodgeRate.ReInitializeValueForReusingStatModifier(_increaseDodgeRate.Value + _dodgeRateIncreaseAmount);
            owner.Stats.DodgeRate.AddModifier(_increaseDodgeRate);
        }
    }

    // 레벨 업 할 때마다 이동 속도 {0} 증가(최대 {1})
    public class IncreaseMoveSpeedOnLevelUp : ConditionalEffectBase
    {
        private readonly float _moveSpeedIncreaseAmount;
        private readonly float _maxIncreaseAmount;
        private readonly StatModifier _increaseMoveSpeed;

        public IncreaseMoveSpeedOnLevelUp(float moveSpeedIncreaseAmount, float maxIncreaseAmount)
            : base(new List<InstantConditionType> { InstantConditionType.OnLevelUp })
        {
            _moveSpeedIncreaseAmount = moveSpeedIncreaseAmount;
            _maxIncreaseAmount = maxIncreaseAmount;
            _increaseMoveSpeed = new StatModifier(0.0f, StatModType.Flat);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.MoveSpeed.AddModifier(_increaseMoveSpeed);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.MoveSpeed.RemoveModifier(_increaseMoveSpeed);
        }

        public override void OnLevelup(Stage stage, PlayerCharacter owner)
        {
            if (_increaseMoveSpeed.Value >= _maxIncreaseAmount)
            {
                return;
            }

            owner.Stats.MoveSpeed.RemoveModifier(_increaseMoveSpeed);
            _increaseMoveSpeed.ReInitializeValueForReusingStatModifier(_increaseMoveSpeed.Value + _moveSpeedIncreaseAmount);
            owner.Stats.MoveSpeed.AddModifier(_increaseMoveSpeed);
        }
    }

    // 레벨 업 할 때마다 받는 피해량 {0}% 감소(최대 {1}%)
    public class IncreaseDamageReductionOnLevelUp : ConditionalEffectBase
    {
        private readonly float _damageReductionIncreaseAmount;
        private readonly float _maxIncreaseAmount;
        private readonly StatModifier _increaseDamageReduction;

        public IncreaseDamageReductionOnLevelUp(float damageReductionIncreaseAmount, float maxIncreaseAmount)
            : base(new List<InstantConditionType> { InstantConditionType.OnLevelUp })
        {
            _damageReductionIncreaseAmount = 0.01f * damageReductionIncreaseAmount;
            _maxIncreaseAmount = 0.01f * maxIncreaseAmount;
            _increaseDamageReduction = new StatModifier(0.0f, StatModType.Flat);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.DamageReduction.AddModifier(_increaseDamageReduction);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.DamageReduction.RemoveModifier(_increaseDamageReduction);
        }

        public override void OnLevelup(Stage stage, PlayerCharacter owner)
        {
            if (_increaseDamageReduction.Value >= _maxIncreaseAmount)
            {
                return;
            }

            owner.Stats.DamageReduction.RemoveModifier(_increaseDamageReduction);
            _increaseDamageReduction.ReInitializeValueForReusingStatModifier(_increaseDamageReduction.Value + _damageReductionIncreaseAmount);
            owner.Stats.DamageReduction.AddModifier(_increaseDamageReduction);
        }
    }

    // 레벨 업 할 때마다 회복량 {0}% 증가(최대 {1}%)
    public class IncreaseHPRecoveryRateOnLevelUp : ConditionalEffectBase
    {
        private readonly float _hpRecoveryRateIncreaseAmount;
        private readonly float _maxIncreaseAmount;
        private readonly StatModifier _increaseHPRecoveryRate;

        public IncreaseHPRecoveryRateOnLevelUp(float hpRecoveryRateIncreaseAmount, float maxIncreaseAmount)
            : base(new List<InstantConditionType> { InstantConditionType.OnLevelUp })
        {
            _hpRecoveryRateIncreaseAmount = 0.01f * hpRecoveryRateIncreaseAmount;
            _maxIncreaseAmount = 0.01f * maxIncreaseAmount;
            _increaseHPRecoveryRate = new StatModifier(0.0f, StatModType.Flat);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.HPRecoveryRate.AddModifier(_increaseHPRecoveryRate);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.HPRecoveryRate.RemoveModifier(_increaseHPRecoveryRate);
        }

        public override void OnLevelup(Stage stage, PlayerCharacter owner)
        {
            if (_increaseHPRecoveryRate.Value >= _maxIncreaseAmount)
            {
                return;
            }

            owner.Stats.HPRecoveryRate.RemoveModifier(_increaseHPRecoveryRate);
            _increaseHPRecoveryRate.ReInitializeValueForReusingStatModifier(_increaseHPRecoveryRate.Value + _hpRecoveryRateIncreaseAmount);
            owner.Stats.HPRecoveryRate.AddModifier(_increaseHPRecoveryRate);
        }
    }

    // 체력이 {0}% 이상인 생명체 공격 시 공격력의 {1}%의 추가 피해
    public class AdditionalDamageForHighHpEnemy : ConditionalEffectBase
    {
        private readonly float _hpThresholdRate;
        private readonly float _attackPowerPercentage;

        public AdditionalDamageForHighHpEnemy(float hpThresholdRate, float attackPowerPercentage)
            : base(new List<InstantConditionType>() { InstantConditionType.AttackedEnemy })
        {
            _hpThresholdRate = 0.01f * hpThresholdRate;
            _attackPowerPercentage = 0.01f * attackPowerPercentage;
        }

        public override void AttackedEnemy(Stage stage, PlayerCharacter owner, Character enemy, float damage)
        {
            if (enemy.CurrentHP + damage >= _hpThresholdRate * enemy.MaxHP)
            {
                enemy.Hitted(stage, null, _attackPowerPercentage * owner.Stats.AttackPower.Value, Vector2.zero, enemy.Pos, null);
            }
        }
    }

    // 체력 {0}% 이상일 경우 공격 속도 +{1}%
    public class IncreaseAttackSpeedOnHighHp : ConditionalEffectBase
    {
        private readonly float _hpThresholdRate;
        private readonly StatModifier _increaseAttackSpeed;

        private bool _isActive;

        public IncreaseAttackSpeedOnHighHp(float hpThresholdRate, float attackSpeedIncreaseRate) : base(new List<InstantConditionType>() { })
        {
            _hpThresholdRate = 0.01f * hpThresholdRate;
            _increaseAttackSpeed = new StatModifier(0.01f * attackSpeedIncreaseRate, StatModType.PercentAdd);

            _isActive = false;
        }

        public override void Update(Stage stage, PlayerCharacter owner)
        {
            base.Update(stage, owner);
            if (owner.CurrentHP >= _hpThresholdRate * owner.MaxHP)
            {
                if (!_isActive)
                {
                    owner.Stats.CharacterAttackSpeed.AddModifier(_increaseAttackSpeed);
                    owner.Stats.SkillAttackSpeed.AddModifier(_increaseAttackSpeed);
                    _isActive = true;
                }
            }
            else
            {
                if (_isActive)
                {
                    owner.Stats.CharacterAttackSpeed.RemoveModifier(_increaseAttackSpeed);
                    owner.Stats.SkillAttackSpeed.RemoveModifier(_increaseAttackSpeed);
                    _isActive = false;
                }
            }
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.CharacterAttackSpeed.RemoveModifier(_increaseAttackSpeed);
            owner.Stats.SkillAttackSpeed.RemoveModifier(_increaseAttackSpeed);
            _isActive = false;
        }
    }

    // 체력 {0}% 이하일 경우 이동 속도 {1} 증가
    public class IncreaseMoveSpeedOnLowHp : ConditionalEffectBase
    {
        private readonly float _hpThresholdRate;
        private readonly StatModifier _increaseMoveSpeed;

        private bool _isActive;

        public IncreaseMoveSpeedOnLowHp(float hpThresholdRate, float moveSpeedIncreaseAmount) : base(new List<InstantConditionType>() { })
        {
            _hpThresholdRate = 0.01f * hpThresholdRate;
            _increaseMoveSpeed = new StatModifier(moveSpeedIncreaseAmount, StatModType.Flat);

            _isActive = false;
        }

        public override void Update(Stage stage, PlayerCharacter owner)
        {
            base.Update(stage, owner);
            if (owner.CurrentHP <= _hpThresholdRate * owner.MaxHP)
            {
                if (!_isActive)
                {
                    owner.Stats.MoveSpeed.AddModifier(_increaseMoveSpeed);
                    _isActive = true;
                }
            }
            else
            {
                if (_isActive)
                {
                    owner.Stats.MoveSpeed.RemoveModifier(_increaseMoveSpeed);
                    _isActive = false;
                }
            }
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.MoveSpeed.RemoveModifier(_increaseMoveSpeed);
            _isActive = false;
        }
    }

    // 체력 {0}% 이하일 경우 받는 피해량 {1}% 감소
    public class IncreaseDamageReductionOnLowHp : ConditionalEffectBase
    {
        private readonly float _hpThresholdRate;
        private readonly StatModifier _increaseDamageReduction;

        private bool _isActive;

        public IncreaseDamageReductionOnLowHp(float hpThresholdRate, float damageReductionIncreaseAmount) : base(new List<InstantConditionType>() { })
        {
            _hpThresholdRate = 0.01f * hpThresholdRate;
            _increaseDamageReduction = new StatModifier(0.01f * damageReductionIncreaseAmount, StatModType.Flat);

            _isActive = false;
        }

        public override void Update(Stage stage, PlayerCharacter owner)
        {
            base.Update(stage, owner);
            if (owner.CurrentHP <= _hpThresholdRate * owner.MaxHP)
            {
                if (!_isActive)
                {
                    owner.Stats.DamageReduction.AddModifier(_increaseDamageReduction);
                    _isActive = true;
                }
            }
            else
            {
                if (_isActive)
                {
                    owner.Stats.DamageReduction.RemoveModifier(_increaseDamageReduction);
                    _isActive = false;
                }
            }
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.DamageReduction.RemoveModifier(_increaseDamageReduction);
            _isActive = false;
        }
    }

    public class IncreaseMoveSpeedOnHitted : ConditionalEffectBase
    {
        private readonly float _increaseTime;
        private readonly StatModifier _increaseMoveSpeed;

        private float _increaseEndsAt;
        private bool _isActive;

        public IncreaseMoveSpeedOnHitted(float increaseTime, float moveSpeedIncreaseAmount)
            : base(new List<InstantConditionType>() { InstantConditionType.HittedByEnemy })
        {
            _increaseTime = increaseTime;
            _increaseMoveSpeed = new StatModifier(moveSpeedIncreaseAmount, StatModType.Flat);

            _increaseEndsAt = 0.0f;
            _isActive = false;
        }

        public override void Update(Stage stage, PlayerCharacter owner)
        {
            base.Update(stage, owner);

            if (Time.time < _increaseEndsAt)
            {
                return;
            }

            if (_isActive)
            {
                owner.Stats.MoveSpeed.RemoveModifier(_increaseMoveSpeed);
                _isActive = false;
            }
        }

        public override void HittedByEnemy(Stage stage, PlayerCharacter owner, Character enemy, float damage)
        {
            _increaseEndsAt = Time.time + _increaseTime;
            if (!_isActive)
            {
                owner.Stats.MoveSpeed.AddModifier(_increaseMoveSpeed);
                _isActive = true;
            }
        }
    }

    // 받는 피해량 {0}% 감소
    public class IncreaseDamageReduction : ConditionalEffectBase
    {
        private readonly StatModifier _increaseDamageReduction;

        public IncreaseDamageReduction(float damageReductionIncreaseAmount) : base(new List<InstantConditionType>() { })
        {
            _increaseDamageReduction = new StatModifier(0.01f * damageReductionIncreaseAmount, StatModType.Flat);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.DamageReduction.AddModifier(_increaseDamageReduction);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.DamageReduction.RemoveModifier(_increaseDamageReduction);
        }
    }

    // 체력 {0}% 이하일 때 공격 범위 {1}% 증가
    public class IncreaseAttackRangeDistanceRatioOnLowHp : ConditionalEffectBase
    {
        private readonly float _hpThresholdRate;
        private readonly StatModifier _increaseAttackRangeDistanceRatio;

        private bool _isActive;

        public IncreaseAttackRangeDistanceRatioOnLowHp(float hpThresholdRate, float attackRangeDistanceRatioIncreaseRate) : base(new List<InstantConditionType>() { })
        {
            _hpThresholdRate = 0.01f * hpThresholdRate;
            _increaseAttackRangeDistanceRatio = new StatModifier(0.01f * attackRangeDistanceRatioIncreaseRate, StatModType.PercentAdd);

            _isActive = false;
        }

        public override void Update(Stage stage, PlayerCharacter owner)
        {
            base.Update(stage, owner);
            if (owner.CurrentHP <= _hpThresholdRate * owner.MaxHP)
            {
                if (!_isActive)
                {
                    owner.Stats.AttackRangeDistanceRatio.AddModifier(_increaseAttackRangeDistanceRatio);
                    _isActive = true;
                }
            }
            else
            {
                if (_isActive)
                {
                    owner.Stats.AttackRangeDistanceRatio.RemoveModifier(_increaseAttackRangeDistanceRatio);
                    _isActive = false;
                }
            }
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.AttackRangeDistanceRatio.RemoveModifier(_increaseAttackRangeDistanceRatio);
            _isActive = false;
        }
    }

    // {0}%의 체력으로 부활 {1}회
    public class ResurrectAndRecoverHp : ConditionalEffectBase
    {
        private readonly StatModifier _increaseResurrectHpRatio;
        private readonly StatModifier _increaseResurrectCount;

        public ResurrectAndRecoverHp(float increaseResurrectHpPercentage, int increaseResurrectionCount)
            : base(new List<InstantConditionType>() { })
        {
            _increaseResurrectHpRatio = new StatModifier(increaseResurrectHpPercentage * 0.01f, StatModType.Flat);
            _increaseResurrectCount = new StatModifier(increaseResurrectionCount, StatModType.Flat);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.ResurrectHpRate.AddModifier(_increaseResurrectHpRatio);
            owner.Stats.MaxResurrectCount.AddModifier(_increaseResurrectCount);
        }


        public override void Update(Stage stage, PlayerCharacter owner)
        {
            base.Update(stage, owner);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.ResurrectHpRate.RemoveModifier(_increaseResurrectHpRatio);
            owner.Stats.MaxResurrectCount.RemoveModifier(_increaseResurrectCount);
        }

    }

    // {0}초마다 체력 버퍼 {1}% 충전
    public class ChargeHPBufferPerTimeInterval : ConditionalEffectBase
    {
        private readonly float _timeInterval;
        private readonly float _hpBufferPercentage;

        private float _chargingAt;

        public ChargeHPBufferPerTimeInterval(float timeInterval, float hpBufferPercentage) : base(new List<InstantConditionType>() { })
        {
            _timeInterval = timeInterval;
            _hpBufferPercentage = 0.01f * hpBufferPercentage;

            _chargingAt = Time.time + _timeInterval;
        }

        public override void Update(Stage stage, PlayerCharacter owner)
        {
            base.Update(stage, owner);

            if (Time.time < _chargingAt)
            {
                return;
            }

            owner.HPBuffer?.ChargeHP(_hpBufferPercentage * owner.HPBuffer.MaxHP.Value);
            _chargingAt += _timeInterval;
        }
    }

    // 체력 버퍼가 비활성화되면 {0}초 동안 무적
    public class InvincibleOnHpBufferDeactivated : ConditionalEffectBase
    {
        private enum State { Waiting, Ready, Invincible }

        private readonly float _invincibilityTime;

        private float _invincibilityEndsAt;
        private State _state;

        public InvincibleOnHpBufferDeactivated(float invincibilityTime) : base(new List<InstantConditionType>() { InstantConditionType.HittedByEnemy })
        {
            _invincibilityTime = invincibilityTime;

            _invincibilityEndsAt = 0.0f;
            _state = State.Waiting;
        }

        public override void Update(Stage stage, PlayerCharacter owner)
        {
            base.Update(stage, owner);

            if (_state == State.Waiting && owner.IsHPBufferActive)
            {
                _state = State.Ready;
            }

            if (_state != State.Invincible)
            {
                return;
            }

            if (Time.time < _invincibilityEndsAt)
            {
                return;
            }

            owner.SetInvincible(false);
            _state = State.Waiting;
        }

        public override void HittedByEnemy(Stage stage, PlayerCharacter owner, Character enemy, float damage)
        {
            if (_state != State.Ready)
            {
                return;
            }

            if (owner.IsHPBufferActive)
            {
                return;
            }

            owner.SetInvincible(true);
            _state = State.Invincible;
            _invincibilityEndsAt = Time.time + _invincibilityTime;
        }
    }

    // 방어막 또는 체력 버퍼 활성화 시 공격력 +{0}%
    public class IncreaseAttackPowerOnShieldOrHPBufferActive : ConditionalEffectBase
    {
        private readonly StatModifier _increaseAttackPower;

        private bool _isActive;

        public IncreaseAttackPowerOnShieldOrHPBufferActive(float attackPowerIncreaseRate) : base(new List<InstantConditionType>() { })
        {
            _increaseAttackPower = new StatModifier(0.01f * attackPowerIncreaseRate, StatModType.PercentAdd);

            _isActive = false;
        }

        public override void Update(Stage stage, PlayerCharacter owner)
        {
            base.Update(stage, owner);
            if (owner.IsShieldActive || owner.IsHPBufferActive)
            {
                if (!_isActive)
                {
                    owner.Stats.AttackPower.AddModifier(_increaseAttackPower);
                    _isActive = true;
                }
            }
            else
            {
                if (_isActive)
                {
                    owner.Stats.AttackPower.RemoveModifier(_increaseAttackPower);
                    _isActive = false;
                }
            }
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.AttackPower.RemoveModifier(_increaseAttackPower);
            _isActive = false;
        }
    }

    // 보스 및 엘리트 생명체에게 공격력의 {0}%의 추가 피해
    public class AdditionalDamageForBossAndElite : ConditionalEffectBase
    {
        private readonly float _attackPowerPercentage;

        public AdditionalDamageForBossAndElite(float attackPowerPercentage)
            : base(new List<InstantConditionType>() { InstantConditionType.AttackedEnemy })
        {
            _attackPowerPercentage = 0.01f * attackPowerPercentage;
        }

        public override void AttackedEnemy(Stage stage, PlayerCharacter owner, Character enemy, float damage)
        {
            if (enemy.IsBoss || enemy.IsElite)
            {
                enemy.Hitted(stage, null, _attackPowerPercentage * owner.Stats.AttackPower.Value, Vector2.zero, enemy.Pos, null);
            }
        }
    }

    // 이동 속도 +{0}, 회피율 +{1}%
    public class IncreaseMoveSpeedAndDodgeRate : ConditionalEffectBase
    {
        private readonly StatModifier _increaseMoveSpeed;
        private readonly StatModifier _increaseDodgeRate;

        public IncreaseMoveSpeedAndDodgeRate(float moveSpeedIncreaseAmount, float dodgeRateIncreaseAmount) : base(new List<InstantConditionType>() { })
        {
            _increaseMoveSpeed = new StatModifier(moveSpeedIncreaseAmount, StatModType.Flat);
            _increaseDodgeRate = new StatModifier(0.01f * dodgeRateIncreaseAmount, StatModType.Flat);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.MoveSpeed.AddModifier(_increaseMoveSpeed);
            owner.Stats.DodgeRate.AddModifier(_increaseDodgeRate);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.MoveSpeed.RemoveModifier(_increaseMoveSpeed);
            owner.Stats.DodgeRate.RemoveModifier(_increaseDodgeRate);
        }
    }

    // 부활할 때마다 받는 피해량 {0}% 감소
    public class IncreaseDamageReductionOnResurrected : ConditionalEffectBase
    {
        private readonly float _damageReductionIncreaseAmount;
        private readonly StatModifier _increaseDamageReduction;

        public IncreaseDamageReductionOnResurrected(float damageReductionIncreaseAmount)
            : base(new List<InstantConditionType>() { InstantConditionType.Resurrected })
        {
            _damageReductionIncreaseAmount = 0.01f * damageReductionIncreaseAmount;
            _increaseDamageReduction = new StatModifier(0.0f, StatModType.Flat);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.DamageReduction.AddModifier(_increaseDamageReduction);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.DamageReduction.RemoveModifier(_increaseDamageReduction);
        }

        public override void Resurrected(Stage stage, PlayerCharacter owner)
        {
            owner.Stats.DamageReduction.RemoveModifier(_increaseDamageReduction);
            _increaseDamageReduction.ReInitializeValueForReusingStatModifier(_increaseDamageReduction.Value + _damageReductionIncreaseAmount);
            owner.Stats.DamageReduction.AddModifier(_increaseDamageReduction);
        }
    }

    // 부활할 때마다 회복량 {0}% 증가
    public class IncreaseHPRecoveryRateOnResurrected : ConditionalEffectBase
    {
        private readonly float _hpRecoveryRateIncreaseAmount;
        private readonly StatModifier _increaseHPRecoveryRate;

        public IncreaseHPRecoveryRateOnResurrected(float hpRecoveryRateIncreaseAmount)
            : base(new List<InstantConditionType>() { InstantConditionType.Resurrected })
        {
            _hpRecoveryRateIncreaseAmount = 0.01f * hpRecoveryRateIncreaseAmount;
            _increaseHPRecoveryRate = new StatModifier(0.0f, StatModType.Flat);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.HPRecoveryRate.AddModifier(_increaseHPRecoveryRate);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.HPRecoveryRate.RemoveModifier(_increaseHPRecoveryRate);
        }

        public override void Resurrected(Stage stage, PlayerCharacter owner)
        {
            owner.Stats.HPRecoveryRate.RemoveModifier(_increaseHPRecoveryRate);
            _increaseHPRecoveryRate.ReInitializeValueForReusingStatModifier(_increaseHPRecoveryRate.Value + _hpRecoveryRateIncreaseAmount);
            owner.Stats.HPRecoveryRate.AddModifier(_increaseHPRecoveryRate);
        }
    }

    // 보스를 제외한 생명체 공격 시 {0}%의 확률로 {1}초 동안 기절
    public class StunEnemyOnAttack : ConditionalEffectBase
    {
        private readonly float _stunProbability;
        private readonly float _stunTime;

        public StunEnemyOnAttack(float stunProbability, float stunTime)
            : base(new List<InstantConditionType>() { InstantConditionType.AttackedEnemy })
        {
            _stunProbability = 0.01f * stunProbability;
            _stunTime = stunTime;
        }

        public override void AttackedEnemy(Stage stage, PlayerCharacter owner, Character enemy, float damage)
        {
            if (enemy.IsBoss)
            {
                return;
            }

            if (Random.Range(0.0f, 1.0f) <= _stunProbability)
            {
                enemy.StatusEffects.AddOrUpdateStatusEffect(stage, enemy, StatusEffectType.Stun, _stunTime, Time.time, 0.0f);
            }
        }
    }

    // 기절한 생명체에게 공격력의 {0}%의 추가 피해
    public class AdditionalDamageForStunnedEnemy : ConditionalEffectBase
    {
        private readonly float _attackPowerPercentage;

        public AdditionalDamageForStunnedEnemy(float attackPowerPercentage)
            : base(new List<InstantConditionType>() { InstantConditionType.AttackedEnemy })
        {
            _attackPowerPercentage = 0.01f * attackPowerPercentage;
        }

        public override void AttackedEnemy(Stage stage, PlayerCharacter owner, Character enemy, float damage)
        {
            if (enemy.Action.IsStunned)
            {
                enemy.Hitted(stage, null, _attackPowerPercentage * owner.Stats.AttackPower.Value, Vector2.zero, enemy.Pos, null);
            }
        }
    }

    // 35초마다 공격 범위 {0}% 증가(최대 {1}%)
    public class IncreaseAttackRangeDistanceRatioPerTimeInterval : ConditionalEffectBase
    {
        private static readonly float TIME_INTERVAL = 35.0f;

        private readonly float _attackRangeDistanceRatioIncreaseRate;
        private readonly float _maxIncreaseRate;
        private readonly StatModifier _increaseAttackRangeDistanceRatio;

        private float _increaseAt;

        public IncreaseAttackRangeDistanceRatioPerTimeInterval(float attackRangeDistanceRatioIncreaseRate, float maxIncreaseRate) : base(new List<InstantConditionType>() { })
        {
            _attackRangeDistanceRatioIncreaseRate = 0.01f * attackRangeDistanceRatioIncreaseRate;
            _maxIncreaseRate = 0.01f * maxIncreaseRate;
            _increaseAttackRangeDistanceRatio = new StatModifier(0.0f, StatModType.PercentAdd);

            _increaseAt = Time.time + TIME_INTERVAL;
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.AttackRangeDistanceRatio.AddModifier(_increaseAttackRangeDistanceRatio);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.AttackRangeDistanceRatio.RemoveModifier(_increaseAttackRangeDistanceRatio);
        }

        public override void Update(Stage stage, PlayerCharacter owner)
        {
            base.Update(stage, owner);

            if (Time.time < _increaseAt)
            {
                return;
            }

            if (_increaseAttackRangeDistanceRatio.Value >= _maxIncreaseRate)
            {
                return;
            }

            owner.Stats.AttackRangeDistanceRatio.RemoveModifier(_increaseAttackRangeDistanceRatio);
            _increaseAttackRangeDistanceRatio.ReInitializeValueForReusingStatModifier(_increaseAttackRangeDistanceRatio.Value + _attackRangeDistanceRatioIncreaseRate);
            owner.Stats.AttackRangeDistanceRatio.AddModifier(_increaseAttackRangeDistanceRatio);
            _increaseAt += TIME_INTERVAL;
        }
    }

    // 45초마다 공격 속도 {0}% 증가(최대 {1}%)
    public class IncreaseAttackSpeedPerTimeInterval : ConditionalEffectBase
    {
        private static readonly float TIME_INTERVAL = 45.0f;

        private readonly float _attackSpeedIncreaseRate;
        private readonly float _maxIncreaseRate;
        private readonly StatModifier _increaseAttackSpeed;

        private float _increaseAt;

        public IncreaseAttackSpeedPerTimeInterval(float attackSpeedIncreaseRate, float maxIncreaseRate) : base(new List<InstantConditionType>() { })
        {
            _attackSpeedIncreaseRate = 0.01f * attackSpeedIncreaseRate;
            _maxIncreaseRate = 0.01f * maxIncreaseRate;
            _increaseAttackSpeed = new StatModifier(0.0f, StatModType.PercentAdd);

            _increaseAt = Time.time + TIME_INTERVAL;
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.CharacterAttackSpeed.AddModifier(_increaseAttackSpeed);
            owner.Stats.SkillAttackSpeed.AddModifier(_increaseAttackSpeed);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.CharacterAttackSpeed.RemoveModifier(_increaseAttackSpeed);
            owner.Stats.SkillAttackSpeed.RemoveModifier(_increaseAttackSpeed);
        }

        public override void Update(Stage stage, PlayerCharacter owner)
        {
            base.Update(stage, owner);

            if (Time.time < _increaseAt)
            {
                return;
            }

            if (_increaseAttackSpeed.Value >= _maxIncreaseRate)
            {
                return;
            }

            owner.Stats.CharacterAttackSpeed.RemoveModifier(_increaseAttackSpeed);
            owner.Stats.SkillAttackSpeed.RemoveModifier(_increaseAttackSpeed);
            _increaseAttackSpeed.ReInitializeValueForReusingStatModifier(_increaseAttackSpeed.Value + _attackSpeedIncreaseRate);
            owner.Stats.CharacterAttackSpeed.AddModifier(_increaseAttackSpeed);
            owner.Stats.SkillAttackSpeed.AddModifier(_increaseAttackSpeed);
            _increaseAt += TIME_INTERVAL;
        }
    }

    // 55초마다 스킬 지속 시간 {0}% 증가(최대 {1}%)
    public class IncreaseDurationIncreaseRatePerTimeInterval : ConditionalEffectBase
    {
        private static readonly float TIME_INTERVAL = 55.0f;

        private readonly float _durationIncreaseRateIncreaseRate;
        private readonly float _maxIncreaseRate;
        private readonly StatModifier _increaseDurationIncreaseRate;

        private float _increaseAt;

        public IncreaseDurationIncreaseRatePerTimeInterval(float durationIncreaseRateIncreaseRate, float maxIncreaseRate) : base(new List<InstantConditionType>() { })
        {
            _durationIncreaseRateIncreaseRate = 0.01f * durationIncreaseRateIncreaseRate;
            _maxIncreaseRate = 0.01f * maxIncreaseRate;
            _increaseDurationIncreaseRate = new StatModifier(0.0f, StatModType.PercentAdd);

            _increaseAt = Time.time + TIME_INTERVAL;
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.DurationIncreaseRate.AddModifier(_increaseDurationIncreaseRate);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.DurationIncreaseRate.RemoveModifier(_increaseDurationIncreaseRate);
        }

        public override void Update(Stage stage, PlayerCharacter owner)
        {
            base.Update(stage, owner);

            if (Time.time < _increaseAt)
            {
                return;
            }

            if (_increaseDurationIncreaseRate.Value >= _maxIncreaseRate)
            {
                return;
            }

            owner.Stats.DurationIncreaseRate.RemoveModifier(_increaseDurationIncreaseRate);
            _increaseDurationIncreaseRate.ReInitializeValueForReusingStatModifier(_increaseDurationIncreaseRate.Value + _durationIncreaseRateIncreaseRate);
            owner.Stats.DurationIncreaseRate.AddModifier(_increaseDurationIncreaseRate);
            _increaseAt += TIME_INTERVAL;
        }
    }

    // 20초마다 공격력 {0}% 증가(최대 {1}%)
    public class IncreaseAttackPowerPerTimeInterval : ConditionalEffectBase
    {
        private static readonly float TIME_INTERVAL = 20.0f;

        private readonly float _attackPowerIncreaseRate;
        private readonly float _maxIncreaseRate;
        private readonly StatModifier _increaseAttackPower;

        private float _increaseAt;

        public IncreaseAttackPowerPerTimeInterval(float attackPowerIncreaseRate, float maxIncreaseRate) : base(new List<InstantConditionType>() { })
        {
            _attackPowerIncreaseRate = 0.01f * attackPowerIncreaseRate;
            _maxIncreaseRate = 0.01f * maxIncreaseRate;
            _increaseAttackPower = new StatModifier(0.0f, StatModType.PercentAdd);

            _increaseAt = Time.time + TIME_INTERVAL;
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.AttackPower.AddModifier(_increaseAttackPower);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.AttackPower.RemoveModifier(_increaseAttackPower);
        }

        public override void Update(Stage stage, PlayerCharacter owner)
        {
            base.Update(stage, owner);

            if (Time.time < _increaseAt)
            {
                return;
            }

            if (_increaseAttackPower.Value >= _maxIncreaseRate)
            {
                return;
            }

            owner.Stats.AttackPower.RemoveModifier(_increaseAttackPower);
            _increaseAttackPower.ReInitializeValueForReusingStatModifier(_increaseAttackPower.Value + _attackPowerIncreaseRate);
            owner.Stats.AttackPower.AddModifier(_increaseAttackPower);
            _increaseAt += TIME_INTERVAL;
        }
    }

    // 크리티컬 대미지 {0}% 증가
    public class IncreaseCriticalCoefficient : ConditionalEffectBase
    {
        private readonly StatModifier _increaseCriticalCoefficient;

        public IncreaseCriticalCoefficient(float criticalCoefficientIncreaseAmount) : base(new List<InstantConditionType>() { })
        {
            _increaseCriticalCoefficient = new StatModifier(0.01f * criticalCoefficientIncreaseAmount, StatModType.Flat);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.CriticalCoefficient.AddModifier(_increaseCriticalCoefficient);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.CriticalCoefficient.RemoveModifier(_increaseCriticalCoefficient);
        }
    }

    // {0}초마다 모든 생명체 {1}초 동안 기절
    public class StunEnemyPerTimeInterval : ConditionalEffectBase
    {
        private const string EFFECT_PATH = "Stages/GradeEffects/StunEnemies.prefab";

        private readonly float _timeInterval;
        private readonly float _stunTime;
        private readonly List<Character> _stunnedCharacters;
        private readonly List<Character> _unstunnedCharacters;

        private float _stunAt;
        private float _endAt;
        private SkeletonAnimation _effect;

        public StunEnemyPerTimeInterval(float timeInterval, float stunTime) : base(new List<InstantConditionType>() { })
        {
            _timeInterval = timeInterval;
            _stunTime = stunTime;
            _stunnedCharacters = new List<Character>();
            _unstunnedCharacters = new List<Character>();

            _stunAt = Time.time + timeInterval;
            _endAt = 0.0f;
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            _effect = ResourcePool.Instance.InstantiateFromResource<SkeletonAnimation>(EFFECT_PATH);
            _effect.transform.SetParent(owner.transform);
            _effect.transform.localPosition = Vector3.zero;
            _effect.transform.localScale = 2.0f * Vector3.one;
            _effect.gameObject.SetActive(false);
        }

        public override void Update(Stage stage, PlayerCharacter owner)
        {
            base.Update(stage, owner);

            float now = Time.time;

            if (_stunAt < now)
            {
                _endAt = now + _stunTime;
                _stunAt += _timeInterval;

                _effect.gameObject.SetActive(true);
                _effect.AnimationState.SetAnimation(0, "animation", false);
            }

            if (now < _endAt)
            {
                stage.FindCharactersInStage(owner.Alliance.ToEnemyAlliance(), x => !x.Action.IsDead && !x.Action.IsStunned, _unstunnedCharacters);
                foreach (var unstunnedCharacter in _unstunnedCharacters)
                {
                    unstunnedCharacter.SetImmuneToKnockBack();
                    unstunnedCharacter.StatusEffects.AddOrUpdateStatusEffect(stage, unstunnedCharacter, StatusEffectType.Stun, _endAt - now, now, 0.0f);
                    _stunnedCharacters.Add(unstunnedCharacter);
                }
                _unstunnedCharacters.Clear();
            }
            else if (_stunnedCharacters.Count > 0)
            {
                // 기절이 종료되면 넉백 면역을 해제해줌.
                foreach (var stunnedCharacter in _stunnedCharacters)
                {
                    stunnedCharacter.UnsetImmuneToKnockBack();
                }
                _stunnedCharacters.Clear();
            }
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            ResourcePool.Instance.PutBackInstance(EFFECT_PATH, _effect.gameObject);
            _effect = null;
        }
    }

    // 피격될 때마다 공격 속도 +{0}%(최대 {1}%)
    public class IncreaseAttackSpeedOnHitted : ConditionalEffectBase
    {
        private readonly float _attackSpeedIncreaseRate;
        private readonly float _maxIncreaseRate;
        private readonly StatModifier _increaseAttackSpeed;

        public IncreaseAttackSpeedOnHitted(float attackSpeedIncreaseRate, float maxIncreaseRate)
            : base(new List<InstantConditionType>() { InstantConditionType.HittedByEnemy })
        {
            _attackSpeedIncreaseRate = 0.01f * attackSpeedIncreaseRate;
            _maxIncreaseRate = 0.01f * maxIncreaseRate;
            _increaseAttackSpeed = new StatModifier(0.0f, StatModType.PercentAdd);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.CharacterAttackSpeed.AddModifier(_increaseAttackSpeed);
            owner.Stats.SkillAttackSpeed.AddModifier(_increaseAttackSpeed);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.CharacterAttackSpeed.RemoveModifier(_increaseAttackSpeed);
            owner.Stats.SkillAttackSpeed.RemoveModifier(_increaseAttackSpeed);
        }

        public override void HittedByEnemy(Stage stage, PlayerCharacter owner, Character enemy, float damage)
        {
            if (_increaseAttackSpeed.Value >= _maxIncreaseRate)
            {
                return;
            }

            owner.Stats.CharacterAttackSpeed.RemoveModifier(_increaseAttackSpeed);
            owner.Stats.SkillAttackSpeed.RemoveModifier(_increaseAttackSpeed);
            _increaseAttackSpeed.ReInitializeValueForReusingStatModifier(_increaseAttackSpeed.Value + _attackSpeedIncreaseRate);
            owner.Stats.CharacterAttackSpeed.AddModifier(_increaseAttackSpeed);
            owner.Stats.SkillAttackSpeed.AddModifier(_increaseAttackSpeed);
        }
    }

    // 공격력이 {0}% 감소하는 대신 공격 범위 {1}% 증가
    public class DecreaseAttackPowerAndIncreaseAttackRange : ConditionalEffectBase
    {
        private readonly StatModifier _decreaseAttackPower;
        private readonly StatModifier _increaseAttackRangeDistanceRatio;

        public DecreaseAttackPowerAndIncreaseAttackRange(float attackPowerDecreaseRate, float attackRangeDistanceRatioIncreaseRate) : base(new List<InstantConditionType>() { })
        {
            _decreaseAttackPower = new StatModifier(-0.01f * attackPowerDecreaseRate, StatModType.PercentAdd);
            _increaseAttackRangeDistanceRatio = new StatModifier(0.01f * attackRangeDistanceRatioIncreaseRate, StatModType.PercentAdd);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.AttackPower.AddModifier(_decreaseAttackPower);
            owner.Stats.AttackRangeDistanceRatio.AddModifier(_increaseAttackRangeDistanceRatio);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.AttackPower.RemoveModifier(_decreaseAttackPower);
            owner.Stats.AttackRangeDistanceRatio.RemoveModifier(_increaseAttackRangeDistanceRatio);
        }
    }

    // 라이카가 생성하는 바람장이 독 제거
    public class WindFieldsRemovePoisonousAreaEffects : ConditionalEffectBase
    {
        public WindFieldsRemovePoisonousAreaEffects() : base(new List<InstantConditionType>() { })
        {

        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.WindFieldsRemovePoisonousAreaEffects, 1.0f);    // ture.
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.WindFieldsRemovePoisonousAreaEffects, 0.0f);    // false.
        }
    }

    // 특정 스킬을 가지고 시작. 가지고 시작할 스킬 식별자를 생성자에 전달해주면 됩니다.
    public class StartWithSkill : ConditionalEffectBase
    {
        private readonly SkillId _skillId;
        private readonly int _skillLevel;

        public StartWithSkill(SkillId skillId) : base(new List<InstantConditionType>() { InstantConditionType.OnSkillSetInitialized })
        {
            _skillId = skillId;
            _skillLevel = 1;
        }

        public StartWithSkill(SkillId skillId, int level) : base(new List<InstantConditionType>() { InstantConditionType.OnSkillSetInitialized })
        {
            _skillId = skillId;
            _skillLevel = level;
        }

        public override void OnSkillSetInitialized(Stage stage, PlayerCharacter owner)
        {
            for(int i = 0; i < _skillLevel; i++)
            {
                owner.AcquireOrUpgradeSkill(_skillId, owner, stage);
            }
        }
    }

    // 강철 주먹이 바람장을 생성.
    public class IronFistsCreateWindFields : ConditionalEffectBase
    {
        public IronFistsCreateWindFields() : base(new List<InstantConditionType>() { })
        {

        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.IronFistsCreateWindFields, 1.0f);   // true.
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.IronFistsCreateWindFields, 0.0f);   // false.
        }
    }

    // 캐릭터 전용 스킬 레벨 {0}부터 시작
    public class StartWithBasicSkillLevel : ConditionalEffectBase
    {
        private readonly int _skillLevel;

        public StartWithBasicSkillLevel(int skillLevel) : base(new List<InstantConditionType>() { InstantConditionType.OnSkillSetInitialized })
        {
            _skillLevel = skillLevel;
        }

        public override void OnSkillSetInitialized(Stage stage, PlayerCharacter owner)
        {
            SkillId basicSkill = owner.StaticData.BasicSkill;
            while (owner.GetSkillLevel(basicSkill) < _skillLevel)
            {
                owner.AcquireOrUpgradeSkill(basicSkill, owner, stage);
            }
        }
    }

    // 텐티 스윕 공격 범위 +{0}%
    public class IncreaseTentiSweepAttackRange : ConditionalEffectBase
    {
        private readonly float _attackRangeIncreaseRate;

        public IncreaseTentiSweepAttackRange(float attackRangeIncreaseRate) : base(new List<InstantConditionType>() { })
        {
            _attackRangeIncreaseRate = 0.01f * attackRangeIncreaseRate;
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.TentiSweepAttackRangeRatio, 1.0f + _attackRangeIncreaseRate);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.TentiSweepAttackRangeRatio, 1.0f);
        }
    }

    // 배틀 요요 공격 범위 +{0}%
    public class IncreaseBattleYoyoAttackRange : ConditionalEffectBase
    {
        private readonly float _attackRangeIncreaseRate;

        public IncreaseBattleYoyoAttackRange(float attackRangeIncreaseRate) : base(new List<InstantConditionType>() { })
        {
            _attackRangeIncreaseRate = 0.01f * attackRangeIncreaseRate;
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.BattleYoyoAttackRangeRatio, 1.0f + _attackRangeIncreaseRate);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.BattleYoyoAttackRangeRatio, 1.0f);
        }
    }

    // 알파 초월 시 유도 미사일 공격 주기 {0}% 감소
    public class DecreaseAlphaHomingMissileAttackPeriod : ConditionalEffectBase
    {
        private readonly float _attackPeriodDecreaseRate;

        public DecreaseAlphaHomingMissileAttackPeriod(float attackPeriodDecreaseRate) : base(new List<InstantConditionType>() { })
        {
            _attackPeriodDecreaseRate = -0.01f * attackPeriodDecreaseRate;
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.AlphaHomingMissileAttackPeriodRatio, 1.0f + _attackPeriodDecreaseRate);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.AlphaHomingMissileAttackPeriodRatio, 1.0f);
        }
    }

    // 스페이스 코인 최소 개수 +{0}
    public class IncreaseSpaceCoinMinAmount : ConditionalEffectBase
    {
        private readonly int _additionalMinAmount;

        public IncreaseSpaceCoinMinAmount(int additionalMinAmount) : base(new List<InstantConditionType>() { })
        {
            _additionalMinAmount = additionalMinAmount;
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.SpaceCoinAdditionalMinAmount, (float)_additionalMinAmount);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.SpaceCoinAdditionalMinAmount, 0f);
        }
    }

    // 생명체 {0}마리 처치 시 부활 횟수 {1}회 추가
    public class AddResurrectionAmountOnKillEnemy : ConditionalEffectBase
    {
        private readonly int _targetKillCount;
        private readonly int _additionalResurrectionAmount;

        private int _currentKillCount;
        private bool _hasAdded;

        public AddResurrectionAmountOnKillEnemy(int targetKillCount, int additionalResurrectionAmount)
            : base(new List<InstantConditionType> { InstantConditionType.KilledEnemy })
        {
            _targetKillCount = targetKillCount;
            _additionalResurrectionAmount = additionalResurrectionAmount;

            _currentKillCount = 0;
            _hasAdded = false;
        }

        public override void KilledEnemy(Stage stage, PlayerCharacter owner, Character enemy)
        {
            if (_hasAdded)
            {
                return;
            }

            ++_currentKillCount;
            if (_currentKillCount >= _targetKillCount)
            {
                owner.IncreaseResurrectCount(_additionalResurrectionAmount);
                _hasAdded = true;
            }
        }
    }

    // 풍선껌에 닿은 생명체 {0}초 간 이동 속도 -{1}%
    public class BubbleGumSlowsEnemies : ConditionalEffectBase
    {
        private readonly float _moveSpeedChangeDuration;
        private readonly float _moveSpeedChangeRatio;

        public BubbleGumSlowsEnemies(float moveSpeedChangeDuration, float moveSpeedChangeRatio) : base(new List<InstantConditionType>() { })
        {
            _moveSpeedChangeDuration = moveSpeedChangeDuration;
            _moveSpeedChangeRatio = 1.0f - 0.01f * moveSpeedChangeRatio;
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.BubbleGumMoveSpeedChangeDuration, _moveSpeedChangeDuration);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.BubbleGumMoveSpeedChangeRatio, _moveSpeedChangeRatio);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.SpaceCoinAdditionalMinAmount, 0.0f);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.SpaceCoinAdditionalMinAmount, 1.0f);
        }
    }

    // 풍선껌 지속 시간 +{0}%
    public class IncreaseBubbleGumDuration : ConditionalEffectBase
    {
        private readonly float _durationIncreaseRate;

        public IncreaseBubbleGumDuration(float durationIncreaseRate) : base(new List<InstantConditionType>() { })
        {
            _durationIncreaseRate = 0.01f * durationIncreaseRate;
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.BubbleGumDurationRatio, 1.0f + _durationIncreaseRate);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.BubbleGumDurationRatio, 1.0f);
        }
    }

    public class AddStunEffectOnMambaSkill : ConditionalEffectBase
    {
        private float _stunDuration;
        public AddStunEffectOnMambaSkill(float stunDuration) : base(new List<InstantConditionType>() { })
        {
            _stunDuration = stunDuration;
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.CustomParameters.IncreaseParameterValue(CustomParameterType.MambaSkill_StunDuration, _stunDuration);
        }
        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.CustomParameters.DecreaseParameterValue(CustomParameterType.MambaSkill_StunDuration, _stunDuration);
        }
    }

    public class MoreDamageToCloseOnMambaSkill : ConditionalEffectBase
    {
        public MoreDamageToCloseOnMambaSkill() : base(new List<InstantConditionType>() { })
        {

        }
    }

    public class MambaSkillTranscendentObjectMultiplyDurationUp : ConditionalEffectBase
    {
        private float _multiplyDurationUpRate;
        public MambaSkillTranscendentObjectMultiplyDurationUp(float multiplyDurationUpPercent) : base(new List<InstantConditionType>() { })
        {
            _multiplyDurationUpRate = multiplyDurationUpPercent * 0.01f;
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.MambaSkill_TranscendentObjectMultiplyDurationUp, _multiplyDurationUpRate);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.MambaSkill_TranscendentObjectMultiplyDurationUp, 0f);
        }
    }

    public class MambaSkillRemovePoisonousAreaEffects : ConditionalEffectBase
    {
        public MambaSkillRemovePoisonousAreaEffects() : base(new List<InstantConditionType>() { })
        {
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.MambaSkill_RemovePoisonousAreaEffects, 1.0f);    // ture.
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.MambaSkill_RemovePoisonousAreaEffects, 0.0f);    // false.
        }
    }


    public class UndineSkillGetGoldOrGem : ConditionalEffectBase
    {
        private float _dropChanceRateFromPercentage;
        public UndineSkillGetGoldOrGem(float dropChancePercentage) : base(new List<InstantConditionType>() { })
        {
            _dropChanceRateFromPercentage = dropChancePercentage * 0.01f;
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.CustomParameters.IncreaseParameterValue(CustomParameterType.UndineBonusItemDropChance, _dropChanceRateFromPercentage);    // ture.
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.CustomParameters.DecreaseParameterValue(CustomParameterType.UndineBonusItemDropChance, _dropChanceRateFromPercentage);    // false.
        }
    }

    public class UndineSkillAcquisitionRangeUp : ConditionalEffectBase
    {
        private float _additionalRange;
        public UndineSkillAcquisitionRangeUp(float additionalRange) : base(new List<InstantConditionType>() { })
        {
            _additionalRange = additionalRange;
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.CustomParameters.IncreaseParameterValue(CustomParameterType.UndineSkillItemAcquireAdditionalRange, _additionalRange);    // ture.
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.CustomParameters.DecreaseParameterValue(CustomParameterType.UndineSkillItemAcquireAdditionalRange, _additionalRange);    // false.
        }
    }

    public class UndineTranscendentSkillMoreGetGoldOrGem : ConditionalEffectBase
    {
        private float _dropChanceRateFromPercentage;
        public UndineTranscendentSkillMoreGetGoldOrGem(float dropChancePercentage) : base(new List<InstantConditionType>() { })
        {
            _dropChanceRateFromPercentage = dropChancePercentage * 0.01f;
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.CustomParameters.IncreaseParameterValue(CustomParameterType.UndineTranscendentBonusItemDropAdditionalChance, _dropChanceRateFromPercentage);    // ture.
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.CustomParameters.DecreaseParameterValue(CustomParameterType.UndineTranscendentBonusItemDropAdditionalChance, _dropChanceRateFromPercentage);    // false.
        }
    }

    public class MultiplyMaxHpAndAttackPower : ConditionalEffectBase
    {
        private readonly StatModifier _multiplyMaxHp;
        private readonly StatModifier _multiplyAttackPower;

        public MultiplyMaxHpAndAttackPower(float maxHpIncrementPercentage, float attackPowerIncrementPercentage) : base(new List<InstantConditionType>() { })
        {
            float rateFromPercentageHP = maxHpIncrementPercentage * 0.01f;
            _multiplyMaxHp = new StatModifier(rateFromPercentageHP, StatModType.PercentAdd);

            float rateFromPercentageAttackPower = attackPowerIncrementPercentage * 0.01f;
            _multiplyAttackPower = new StatModifier(rateFromPercentageAttackPower, StatModType.PercentAdd);

        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.MaxHP.AddModifier(_multiplyMaxHp);
            owner.Stats.AttackPower.AddModifier(_multiplyAttackPower);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.MaxHP.RemoveModifier(_multiplyMaxHp);
            owner.Stats.AttackPower.RemoveModifier(_multiplyAttackPower);
        }
    }

}
