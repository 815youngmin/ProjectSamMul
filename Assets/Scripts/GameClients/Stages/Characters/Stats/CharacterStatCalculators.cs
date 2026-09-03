using UnityEngine;

namespace Z.GameClients.Stages.Characters.Stats
{
    public interface IReadOnlyCharacterStatCalculators
    {
        public float MaxHPValue { get; }
        public float AttackPowerValue { get; }
        public float CharacterAttackSpeedValue { get; }
        public float SkillAttackSpeedValue { get; }
        public float MoveSpeedValue { get; }
        public float KnockBackResistanceValue { get; }
        public float DamageReductionValue { get; }
        public float DurationIncreaseRateValue { get; }
        public float ProjectileMoveSpeedIncreaseRateValue { get; }
        public float ReceivedDamageIncreaseValue { get; }
    }

    /// <summary>
    /// 캐릭터가의 스탯들을 계산해줍니다.
    /// 캐릭터의 정적데이터로부터 기본 스탯(BaseValue)을 결정하고,
    /// 장비나 스킬에 따른 추가 스탯(StatModifier)에 따른 동적인 변화값을 추가하여
    /// 현재 캐릭터의 스탯 최종 값(statCalculator.Value)을 계산해줍니다.
    /// </summary>
    public class CharacterStatCalculators : IReadOnlyCharacterStatCalculators
    {
        // 최대 체력
        public readonly StatCalculator MaxHP;
        // 공격력
        public readonly StatCalculator AttackPower;
        // 기본 공격의 공격속도 (초당 공격 횟수) 
        public readonly StatCalculator CharacterAttackSpeed;
        // 기본 스킬의 공격속도
        public readonly StatCalculator SkillAttackSpeed;
        // 이동속도
        public readonly StatCalculator MoveSpeed;
        // 넉백 저항 (피격시 밀리는 힘에서 넉백저항만큼 차감), 유효범위 : 0.0~1.0
        public readonly StatCalculator KnockBackResistance;
        // 데미지 감소율. 백분율(0 ~ 1)
        public readonly StatCalculator DamageReduction;
        // 스킬 지속시간 증가 비율.(1 ~ 2 권장) 
        public readonly StatCalculator DurationIncreaseRate;
        // 발사체 이동속도 증가 비율.(0 ~ 1) 
        public readonly StatCalculator ProjectileMoveSpeedIncreaseRate;
        // 받는 피해량 증가.
        public readonly StatCalculator ReceivedDamageIncrease;
        // 크리티컬 확률 백분율. (0 ~ 1)
        public readonly StatCalculator CriticalChance;
        // 크리티컬 계수. 기본값은 2로, 크리티컬 공격 시 2배의 피해를 준다.
        public readonly StatCalculator CriticalCoefficient;

        float IReadOnlyCharacterStatCalculators.MaxHPValue => MaxHP.Value;
        float IReadOnlyCharacterStatCalculators.AttackPowerValue => AttackPower.Value;
        float IReadOnlyCharacterStatCalculators.CharacterAttackSpeedValue => CharacterAttackSpeed.Value;
        float IReadOnlyCharacterStatCalculators.SkillAttackSpeedValue => SkillAttackSpeed.Value;
        float IReadOnlyCharacterStatCalculators.MoveSpeedValue => MoveSpeed.Value;
        float IReadOnlyCharacterStatCalculators.KnockBackResistanceValue => KnockBackResistance.Value;
        float IReadOnlyCharacterStatCalculators.DamageReductionValue => DamageReduction.Value;
        float IReadOnlyCharacterStatCalculators.DurationIncreaseRateValue => DurationIncreaseRate.Value;
        float IReadOnlyCharacterStatCalculators.ProjectileMoveSpeedIncreaseRateValue => ProjectileMoveSpeedIncreaseRate.Value; // 발사체 이동속도 증가 비율.(0 ~ 1) 
        float IReadOnlyCharacterStatCalculators.ReceivedDamageIncreaseValue => ReceivedDamageIncrease.Value;


        public CharacterStatCalculators()
        {
            this.MaxHP = new StatCalculator();
            this.AttackPower = new StatCalculator();
            this.CharacterAttackSpeed = new StatCalculator();
            this.SkillAttackSpeed = new StatCalculator();
            this.MoveSpeed = new StatCalculator();
            this.KnockBackResistance = new StatCalculator();
            this.DamageReduction = new StatCalculator();
            this.DurationIncreaseRate = new StatCalculator(1.0f);
            this.ProjectileMoveSpeedIncreaseRate = new StatCalculator(1.0f);
            this.ReceivedDamageIncrease = new StatCalculator();
            this.CriticalChance = new StatCalculator();
            this.CriticalCoefficient = new StatCalculator(2.0f);
        }

        public void SetBaseStats(BaseStats baseStats)
        {
            this.MaxHP.SetBaseValue(baseStats.MaxHP);
            this.AttackPower.SetBaseValue(baseStats.AttackPower);
            this.CharacterAttackSpeed.SetBaseValue(baseStats.BasicAttackSpeed);
            this.SkillAttackSpeed.SetBaseValue(baseStats.SkillAttackSpeed);
            this.MoveSpeed.SetBaseValue(baseStats.MoveSpeed);
            this.KnockBackResistance.SetBaseValue(baseStats.KnockBackResistance);

            if (KnockBackResistance.Value < 0f ||
                KnockBackResistance.Value > 1f)
            {
                Debug.LogError($"넉백저항이 잘못 설정되었습니다. 입력값[{KnockBackResistance.Value}] 0.0f<= value <=1.0f 의 조건을 만족해야 합니다. 수정해주세요.");
            }
        }

        public virtual void ClearModifiers()
        {
            this.MaxHP.ClearModifiers();
            this.AttackPower.ClearModifiers();
            this.CharacterAttackSpeed.ClearModifiers();
            this.SkillAttackSpeed.ClearModifiers();
            this.MoveSpeed.ClearModifiers();
            this.KnockBackResistance.ClearModifiers();
            this.DamageReduction.ClearModifiers();
            this.DurationIncreaseRate.ClearModifiers();
            this.ProjectileMoveSpeedIncreaseRate.ClearModifiers();
            this.ReceivedDamageIncrease.ClearModifiers();
            this.CriticalChance.ClearModifiers();
            this.CriticalCoefficient.ClearModifiers();
        }

        public override string ToString()
        {
            // TODO : 유니티 GC는 메모리 단편화가 심하기 때문에, 아래와 같은 방식의 string interpolation 사용은 피하는 것이 좋다.
            //        string format 을 사용한 방법 혹은 StringBuilder를 사용해야 한다.
            return $"MaxHP\n{MaxHP.DetailToString()}" +
                   $"AttackPower\n{AttackPower.DetailToString()}" +
                   $"CharacterAttackSpeed\n{CharacterAttackSpeed.DetailToString()}" +
                   $"SkillAttackSpeed\n{SkillAttackSpeed.DetailToString()}" +
                   $"MoveSpeed\n{MoveSpeed.DetailToString()}" +
                   $"KnockBackResistance\n{KnockBackResistance.DetailToString()}" +
                   $"DamageReduction\n{DamageReduction.DetailToString()}" +
                   $"DurationIncreaseRate\n{DurationIncreaseRate.DetailToString()}" +
                   $"ProjectileMoveSpeedIncreaseRate\n{ProjectileMoveSpeedIncreaseRate.DetailToString()}" +
                   $"ReceivedDamageIncrease\n{ReceivedDamageIncrease.DetailToString()}" +
                   $"CriticalChance\n{CriticalChance.DetailToString()}" +
                   $"CriticalCoefficient\n{CriticalCoefficient.DetailToString()}";
        }
    }
}
