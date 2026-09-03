#nullable enable
using Shared.DataTables;
using Shared.GameDataTypes;
using Shared.StaticDatas;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.GameClients.Stages.Characters.PCs;
using SamMul.GameClients.Stages.Characters.Stats;

namespace SamMul.GameClients.Stages.Characters.ConditionalEffects
{
    //일반 진화 공격력 증가 +%N
    public class EvolutionStrength : ConditionalEffectBase
    {
        private StatModifier _addAttackPower;

        public EvolutionStrength(float attackPowerIncrement) : base(new List<InstantConditionType>() { })
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

    //일반 진화 HP증가 +N
    public class EvolutionStamina : ConditionalEffectBase
    {
        private StatModifier _addMaxHp;

        public EvolutionStamina(float maxHpIncrement) : base(new List<InstantConditionType>() { })
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

    //일반진화 방어력 증가 +N%
    public class EvolutionTenacity : ConditionalEffectBase
    {
        StatModifier _addDamageReduction;
        public EvolutionTenacity(float addDamageRedction) : base(new List<InstantConditionType>() { })
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

    //일반진화 고기 회복 증가 +N%
    public class EvolutionRestoration : ConditionalEffectBase
    {
        StatModifier _eatingHPRecoveryRateIncreaser;
        public EvolutionRestoration(float eatingHPRecoveryRateIncreaser) : base(new List<InstantConditionType>() { })
        {
            float rateFromPercentage = eatingHPRecoveryRateIncreaser * 0.01f;
            _eatingHPRecoveryRateIncreaser = new StatModifier(rateFromPercentage, StatModType.Flat);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.EatingHPRecoveryRate.AddModifier(_eatingHPRecoveryRateIncreaser);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.EatingHPRecoveryRate.RemoveModifier(_eatingHPRecoveryRateIncreaser);
        }
    }

    //게임 시작 시 공용스킬 1개 획득
    public class LetsBringThisToo : ConditionalEffectBase
    {
        public LetsBringThisToo() : base(new List<InstantConditionType>() { })
        {

        }
        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.LetsBringThisToo, 1f);
        }
        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.LetsBringThisToo, 0f);
        }
    }

    //스킬 재선택 기회 +N회
    public class OneMoreTime : ConditionalEffectBase
    {
        private readonly int _additionalSkillRefreshCount;

        public OneMoreTime(int additionalSkillRefreshCount) : base(new List<InstantConditionType>() { })
        {
            _additionalSkillRefreshCount = additionalSkillRefreshCount;
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.CustomParameters.IncreaseParameterValue(CustomParameterType.AdditionalSkillRefreshCount, _additionalSkillRefreshCount);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.CustomParameters.DecreaseParameterValue(CustomParameterType.AdditionalSkillRefreshCount, _additionalSkillRefreshCount);
        }
    }

    //회복 효과 +N%
    public class BurningMeat : ConditionalEffectBase
    {
        StatModifier _hpRecoveryRateIncreaser;
        public BurningMeat(float hpRecoveryIncrementPercentage) : base(new List<InstantConditionType>() { })
        {
            float rateFromPercentage = hpRecoveryIncrementPercentage * 0.01f;
            _hpRecoveryRateIncreaser = new StatModifier(rateFromPercentage, StatModType.PercentAdd);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.HPRecoveryRate.AddModifier(_hpRecoveryRateIncreaser);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.HPRecoveryRate.RemoveModifier(_hpRecoveryRateIncreaser);
        }
    }

    //치명타 확률 +N
    public class HAHAHTormentYou : ConditionalEffectBase
    {
        StatModifier _criticalRateIncreaser;
        public HAHAHTormentYou(float criticalIncrementPercentage) : base(new List<InstantConditionType>() { })
        {
            float rateFromPercentage = criticalIncrementPercentage * 0.01f;
            _criticalRateIncreaser = new StatModifier(rateFromPercentage, StatModType.Flat);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.CriticalChance.AddModifier(_criticalRateIncreaser);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.CriticalChance.RemoveModifier(_criticalRateIncreaser);
        }
    }

    //스킬 재선택 기회 +N회 추가
    public class GivingGirlAChance : ConditionalEffectBase
    {
        private readonly int _additionalSkillRefreshCount;

        public GivingGirlAChance(int additionalSkillRefreshCount) : base(new List<InstantConditionType>() { })
        {
            _additionalSkillRefreshCount = additionalSkillRefreshCount;
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.CustomParameters.IncreaseParameterValue(CustomParameterType.AdditionalSkillRefreshCount, _additionalSkillRefreshCount);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.CustomParameters.DecreaseParameterValue(CustomParameterType.AdditionalSkillRefreshCount, _additionalSkillRefreshCount);
        }
    }

    //투사체 속도 +N%
    public class SwooshAway : ConditionalEffectBase
    {
        StatModifier _projectileSpeedRateIncreaser;
        public SwooshAway(float projectileSpeedIncrementPercentage) : base(new List<InstantConditionType>() { })
        {
            float rateFromPercentage = projectileSpeedIncrementPercentage * 0.01f;
            _projectileSpeedRateIncreaser = new StatModifier(rateFromPercentage, StatModType.PercentAdd);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.ProjectileMoveSpeedIncreaseRate.AddModifier(_projectileSpeedRateIncreaser);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.ProjectileMoveSpeedIncreaseRate.RemoveModifier(_projectileSpeedRateIncreaser);
        }
    }
    //기본 이동속도 +N%
    public class ComeHereFood : ConditionalEffectBase
    {
        StatModifier _moveSpeedRateIncreaser;
        public ComeHereFood(float moveSpeedIncrementPercentage) : base(new List<InstantConditionType>() { })
        {
            float rateFromPercentage = moveSpeedIncrementPercentage * 0.01f;
            _moveSpeedRateIncreaser = new StatModifier(rateFromPercentage, StatModType.PercentAdd);
        }
        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.MoveSpeed.AddModifier(_moveSpeedRateIncreaser);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.MoveSpeed.RemoveModifier(_moveSpeedRateIncreaser);
        }
    }
    //치명타 확률 +N%
    public class TodayExperimentFun : ConditionalEffectBase
    {
        StatModifier _criticalRateIncreaser;
        public TodayExperimentFun(float criticalIncrementPercentage) : base(new List<InstantConditionType>() { })
        {
            float rateFromPercentage = criticalIncrementPercentage * 0.01f;
            _criticalRateIncreaser = new StatModifier(rateFromPercentage, StatModType.Flat);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.CriticalChance.AddModifier(_criticalRateIncreaser);
        }
        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.CriticalChance.RemoveModifier(_criticalRateIncreaser);
        }
    }
    //회복 효과 +N% 추가
    public class SweetFragranceLovely : ConditionalEffectBase
    {
        StatModifier _hpRecoveryRateIncreaser;
        public SweetFragranceLovely(float hpRecoveryIncrementPercentage) : base(new List<InstantConditionType>() { })
        {
            float rateFromPercentage = hpRecoveryIncrementPercentage * 0.01f;
            _hpRecoveryRateIncreaser = new StatModifier(rateFromPercentage, StatModType.PercentAdd);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.HPRecoveryRate.AddModifier(_hpRecoveryRateIncreaser);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.HPRecoveryRate.RemoveModifier(_hpRecoveryRateIncreaser);
        }
    }
    //치명타 확률 +N%
    public class WeaknessIsHere : ConditionalEffectBase
    {
        StatModifier _criticalRateIncreaser;
        public WeaknessIsHere(float criticalIncrementPercentage) : base(new List<InstantConditionType>() { })
        {
            float rateFromPercentage = criticalIncrementPercentage * 0.01f;
            _criticalRateIncreaser = new StatModifier(rateFromPercentage, StatModType.Flat);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.CriticalChance.AddModifier(_criticalRateIncreaser);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.CriticalChance.RemoveModifier(_criticalRateIncreaser);
        }
    }

    //스킬 재선택 기회 +N회 추가
    public class HoldOnRedo : ConditionalEffectBase
    {
        private readonly int _additionalSkillRefreshCount;

        public HoldOnRedo(int additionalSkillRefreshCount) : base(new List<InstantConditionType>() { })
        {
            _additionalSkillRefreshCount = additionalSkillRefreshCount;
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.CustomParameters.IncreaseParameterValue(CustomParameterType.AdditionalSkillRefreshCount, _additionalSkillRefreshCount);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.CustomParameters.DecreaseParameterValue(CustomParameterType.AdditionalSkillRefreshCount, _additionalSkillRefreshCount);
        }
    }

    //공격 범위 +N%
    public class ComeHereFight : ConditionalEffectBase
    {
        StatModifier _attackRangeRateIncreaser;
        public ComeHereFight(float attackRangeIncrementPercentage) : base(new List<InstantConditionType>() { })
        {
            float rateFromPercentage = attackRangeIncrementPercentage * 0.01f;
            _attackRangeRateIncreaser = new StatModifier(rateFromPercentage, StatModType.PercentAdd);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.AttackRangeDistanceRatio.AddModifier(_attackRangeRateIncreaser);
        }
        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.AttackRangeDistanceRatio.RemoveModifier(_attackRangeRateIncreaser);
        }
    }
    //회복 효과 +N% 추가
    public class SeasoningIsDelicious : ConditionalEffectBase
    {
        StatModifier _hpRecoveryRateIncreaser;
        public SeasoningIsDelicious(float hpRecoveryIncrementPercentage) : base(new List<InstantConditionType>() { })
        {
            float rateFromPercentage = hpRecoveryIncrementPercentage * 0.01f;
            _hpRecoveryRateIncreaser = new StatModifier(rateFromPercentage, StatModType.PercentAdd);
        }
        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.HPRecoveryRate.AddModifier(_hpRecoveryRateIncreaser);
        }
        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.HPRecoveryRate.RemoveModifier(_hpRecoveryRateIncreaser);
        }
    }
    //공격속도 +N%
    public class GirlWillFinishFaster : ConditionalEffectBase
    {
        StatModifier _attackSpeedRateIncreaser;
        public GirlWillFinishFaster(float attackSpeedIncrementPercentage) : base(new List<InstantConditionType>() { })
        {
            float rateFromPercentage = attackSpeedIncrementPercentage * 0.01f;
            _attackSpeedRateIncreaser = new StatModifier(rateFromPercentage, StatModType.PercentAdd);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.CharacterAttackSpeed.AddModifier(_attackSpeedRateIncreaser);
            owner.Stats.SkillAttackSpeed.AddModifier(_attackSpeedRateIncreaser);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.CharacterAttackSpeed.RemoveModifier(_attackSpeedRateIncreaser);
            owner.Stats.SkillAttackSpeed.RemoveModifier(_attackSpeedRateIncreaser);
        }
    }

    //기존 장비 스탯을 가져와서 추가 스탯을 만들어준다음 적용해준다.
    //모든 장비 기본 스탯 +N%
    public class YoungLadyOutfitBest : ConditionalEffectBase
    {
        private StatModifier? _additionalAttackPower;
        private StatModifier? _additionalHP;

        private readonly float _statEmphasizingRate;

        public YoungLadyOutfitBest(float equipmentStatIncrementPercentage) : base(new List<InstantConditionType>() { })
        {
            _statEmphasizingRate = equipmentStatIncrementPercentage * 0.01f;
            _additionalAttackPower = null;
            _additionalHP = null;
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);

            if (owner.EquipmentStatModifiers == null)
            {
                Debug.LogError($"PlayerCharacter 객체에 EquipmentStatModifiers가 초기화되기 전에 {nameof(YoungLadyOutfitBest)}가 처리되었습니다. 올바로 동작하지 않습니다.");
                return;
            }

            float attackPowerIncrement = owner.EquipmentStatModifiers.Value.AttackPowerAddValue.Value * _statEmphasizingRate;
            float maxHPIncrement = owner.EquipmentStatModifiers.Value.MaxHPAddValue.Value * _statEmphasizingRate;

            _additionalAttackPower = new StatModifier(attackPowerIncrement, StatModType.Flat);
            _additionalHP = new StatModifier(maxHPIncrement, StatModType.Flat);

            owner.Stats.AttackPower.AddModifier(_additionalAttackPower!);
            owner.Stats.MaxHP.AddModifier(_additionalHP!);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);

            if (_additionalAttackPower != null)
            {
                owner.Stats.AttackPower.RemoveModifier(_additionalAttackPower!);
            }
            if (_additionalHP != null)
            {
                owner.Stats.MaxHP.RemoveModifier(_additionalHP!);
            }

            _additionalAttackPower = null;
            _additionalHP = null;
        }
    }

    public class KetchapiDressesWell : ConditionalEffectBase
    {
        private List<EquipmentSetType> _equipmentSetTypes;
        private int _mostSetTypeCount;
        private int _equipmentSlotCount;

        private float _rateFromPercentagePerPiece;
        private float _rateFromPercentageMaxDamageRedction;

        private StatModifier _addDamageRedction;

        //장비의 이름이 동일할 시 개당+N1%, 세트템 전부 착용시 +N2%
        public KetchapiDressesWell(float addDamageRedctionPerPiece, float maxAddDamageRedction) : base(new List<InstantConditionType>() { })
        {
            _equipmentSetTypes = new List<EquipmentSetType>();
            _rateFromPercentagePerPiece = addDamageRedctionPerPiece * 0.01f;
            _rateFromPercentageMaxDamageRedction = maxAddDamageRedction * 0.01f;
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);

            var equipmentStaticData = StaticDataRepository.Instance.Equipments;
            var equippedEquipments = owner.EquippedEquipments;

            if (equippedEquipments.Count() <= 0)
            {
                return;
            }

            foreach (var equipment in equippedEquipments)
            {
                var setType = equipmentStaticData.Get(equipment.EquipmentId).SetType;
                _equipmentSetTypes.Add(setType);
            }

            _equipmentSlotCount = GameConstants.VALID_EQUIPMENT_SLOTS.Count;

            //가장 많은 세트타입, 개수 찾는 코드
            var mostSetTypeValue = _equipmentSetTypes.GroupBy(i => i)
                .OrderByDescending(grp => grp.Count())
                .Select(grp => new { Value = grp.Key, count = grp.Count() })
                .FirstOrDefault();
            _mostSetTypeCount = mostSetTypeValue.count;

            if (1 < _mostSetTypeCount && _mostSetTypeCount < _equipmentSlotCount)
            {
                //세트템 스탯 적용 (한개 이상일때)
                _addDamageRedction = new StatModifier(_rateFromPercentagePerPiece * _mostSetTypeCount, StatModType.Flat);
                owner.Stats.DamageReduction.AddModifier(_addDamageRedction);
            }
            else if (_mostSetTypeCount >= _equipmentSlotCount)
            {
                //모든 아이템 슬롯이 동일한 세트 템일때
                //만약 아이템 슬롯 중 세트템과 관련없는 슬롯이 있다면 이 코드는 수정되어야 한다
                _addDamageRedction = new StatModifier(_rateFromPercentageMaxDamageRedction, StatModType.Flat);
                owner.Stats.DamageReduction.AddModifier(_addDamageRedction);
            }
            else
            {
                _addDamageRedction = null;
            }
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);

            if (_addDamageRedction != null)
            {
                owner.Stats.DamageReduction.RemoveModifier(_addDamageRedction);
            }
        }
    }
    //N% 체력을 갖고 한번 부활
    public class ThisMeatIsFierce : ConditionalEffectBase
    {
        private readonly StatModifier _increaseResurrectCount;
        public ThisMeatIsFierce(int increaseResurrectionCount) : base(new List<InstantConditionType>() { })
        {
            _increaseResurrectCount = new StatModifier(increaseResurrectionCount, StatModType.Flat);

        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.MaxResurrectCount.AddModifier(_increaseResurrectCount);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.MaxResurrectCount.RemoveModifier(_increaseResurrectCount);
        }

    }
    //보스 처치시 N%확률로 스킬박스가 1개더 드랍
    public class JACKPOT : ConditionalEffectBase
    {
        private float _rateFromPercentage;
        public JACKPOT(float dropSkillBoxPercentage) : base(new List<InstantConditionType>() { InstantConditionType.KilledBoss })
        {
            _rateFromPercentage = dropSkillBoxPercentage * 0.01f;
        }


        public override void KilledBoss(Stage stage, PlayerCharacter owner, Monster boss)
        {
            if (Random.value <= _rateFromPercentage)
            {
                Vector2 randomAddPosition = new Vector2(Random.Range(-2.0f, 2.0f), Random.Range(-2.0f, 2.0f));
                stage.CreateSkillBoxObject(randomAddPosition + boss.CenterPos);
            }
        }
    }
    //레벨업시 스킬 재선택 기회가 +1회
    public class LotsOfChancesLotsOfMeat : ConditionalEffectBase
    {
        public LotsOfChancesLotsOfMeat() : base(new List<InstantConditionType>() { })
        {
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.LotsOfChancesLotsOfMeat, 1.0f);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.CustomParameters.ChangeParameterValue(CustomParameterType.LotsOfChancesLotsOfMeat, 0f);
        }

    }

    public class AcquisitionDistance : ConditionalEffectBase
    {
        private readonly StatModifier _increaseAcquisitionDistance;

        public AcquisitionDistance(float increasePercent) : base(new List<InstantConditionType>() { })
        {
            _increaseAcquisitionDistance = new StatModifier(0.01f * increasePercent, StatModType.PercentAdd);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.AcquisitionDistance.AddModifier(_increaseAcquisitionDistance);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Destroy(stage, owner);
            owner.Stats.AcquisitionDistance.RemoveModifier(_increaseAcquisitionDistance);
        }
    }

    public class GoldIncreaseRate : ConditionalEffectBase
    {
        private readonly StatModifier _increaseGoldIncreaseRate;

        public GoldIncreaseRate(float increasePercent) : base(new List<InstantConditionType>() { })
        {
            _increaseGoldIncreaseRate = new StatModifier(0.01f * increasePercent, StatModType.Flat);
        }

        public override void Initialize(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.GoldIncreaseRate.AddModifier(_increaseGoldIncreaseRate);
        }

        public override void Destroy(Stage stage, PlayerCharacter owner)
        {
            base.Initialize(stage, owner);
            owner.Stats.GoldIncreaseRate.RemoveModifier(_increaseGoldIncreaseRate);
        }
    }
}
