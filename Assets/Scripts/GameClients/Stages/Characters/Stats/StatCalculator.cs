using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Z.GameClients.Stages.Characters.Stats
{

    /// <summary>
    /// 캐릭터의 스탯들을 계산해줍니다.
    /// 캐릭터의 정적데이터로부터 기본 스탯(BaseValue)을 결정하고,
    /// 장비나 스킬에 따른 추가 스탯(StatModifier)에 따른 동적인 변화값을 추가하여
    /// 현재 캐릭터의 스탯 최종 값(statCalculator.Value)을 계산해줍니다.
    /// </summary>
    [Serializable]
    public sealed class StatCalculator
    {
        /// <summary>
        /// 스탯의 기준이되는 최초 기본 값. BaseValue에 Modifier들을 계산한 값인 <see cref="Value"/>가 현재 스탯을 나타낸다.
        /// </summary>
        private float baseValue;
        private bool isDirty;
        private float lastBaseValue;
        private float _valueAfterSum;
        private float _value;
        private readonly List<StatModifier> statModifiers;

        public float BaseValue => this.baseValue;

        /// <summary>
        /// 합연산만을 계산한 값. StatModType.Flat인 Modifier들만 계산되었다. 이 값에 곱연산을 하여 최종값을 구한다.
        /// </summary>
        public float ValueAfterSum
        {
            get
            {
                if (isDirty || lastBaseValue != baseValue)
                {
                    lastBaseValue = baseValue;
                    _value = CalculateFinalValue();
                    isDirty = false;
                }
                return _valueAfterSum;
            }
        }

        /// <summary>
        /// 현재 스탯. 계산된 스탯의 최종값.
        /// Value = BaseValue + Sum of Modifiers
        /// </summary>
        public float Value
        {
            get
            {
                if (isDirty || lastBaseValue != baseValue)
                {
                    lastBaseValue = baseValue;
                    _value = CalculateFinalValue();
                    isDirty = false;
                }
                return _value;
            }
        }

        public readonly ReadOnlyCollection<StatModifier> StatModifiers;
        public StatCalculator()
        {
            this.baseValue = 0f;
            this.statModifiers = new List<StatModifier>();
            this.StatModifiers = this.statModifiers.AsReadOnly();
            this.isDirty = true;
        }

        public StatCalculator(float baseValue) : this()
        {
            this.baseValue = baseValue;
        }

        public void SetBaseValue(float baseValue)
        {
            this.baseValue = baseValue;
        }

        public void AddModifier(StatModifier mod)
        {
            isDirty = true;
            statModifiers.Add(mod);
        }

        public void AddModifier(StatCalculator stat)
        {
            isDirty = true;
            for (int i = 0; i < stat.statModifiers.Count; i++)
            {
                statModifiers.Add(stat.statModifiers[i]);
            }
        }

        public bool RemoveModifier(StatModifier mod)
        {
            if (statModifiers.Remove(mod))
            {
                isDirty = true;
                return true;
            }
            return false;
        }

        public void ClearModifiers()
        {
            isDirty = true;
            statModifiers.Clear();
        }

        private int CompareModifierOrder(StatModifier a, StatModifier b)
        {
            if (a.Order < b.Order)
                return -1;
            else if (a.Order > b.Order)
                return 1;
            return 0; //if (a.Order == b.Order)
        }

        private float CalculateFinalValue()
        {
            float finalValue = this.baseValue;
            float sumPercentAdd = 0;

            statModifiers.Sort(CompareModifierOrder);

            for (int i = 0; i < statModifiers.Count; i++)
            {
                StatModifier mod = statModifiers[i];

                if (mod.Type == StatModType.Flat)
                {
                    finalValue += mod.Value;
                }
                else if (mod.Type == StatModType.PercentAdd)
                {
                    sumPercentAdd += mod.Value;

                    if (i + 1 >= statModifiers.Count || statModifiers[i + 1].Type != StatModType.PercentAdd)
                    {
                        _valueAfterSum = finalValue;
                        finalValue *= 1 + sumPercentAdd;
                        sumPercentAdd = 0;
                    }
                }
                else if (mod.Type == StatModType.PercentMult)
                {
                    finalValue *= 1 + mod.Value;
                }
            }

            // Workaround for float calculation errors, like displaying 12.00001 instead of 12
            return (float)Math.Round(finalValue, 4);
        }
    

        public string DetailToString()
        {
            string s = $"기본값:{baseValue.ToString("N2")}\n";
            for (int i = 0; i < statModifiers.Count; i++)
            {
                if (statModifiers[i].Value != 0)
                    s += $"({statModifiers[i].ToString()}\n";
            }
            s += $"최종값:{Value.ToString("N2")}\n";
            return s;
        }
    }
}
