#nullable enable
using System.Collections.Generic;
using Shared.GameDataTypes;
using Shared.StaticDatas;
using Shared.UserDatas;

namespace Shared.GameLogics
{
    /// <summary>
    /// Out-of-stage stat calculation: hero base stat (HeroStats table by rarity and grade, growing per level)
    /// + worn equipment (EquipmentStats table).
    /// </summary>
    public static class AvatarLogic
    {
        public static readonly float DOMINANT_BONUS_RATE = 0.15f;
        public static readonly float RECESSIVE_BONUS_RATE = 0f;
        public static readonly float EQUAL_BONUS_RATE = 0.05f;

        public static float CalculateElementBonusRate(ElementType heroElement, ElementType stageElement) => ElementLogic.Compare(heroElement, stageElement) switch
        {
            ElementComparisonResult.Dominant => DOMINANT_BONUS_RATE,
            ElementComparisonResult.Recessive => RECESSIVE_BONUS_RATE,
            _ => EQUAL_BONUS_RATE,
        };

        public static float CalculateAttackPower(HeroData selectedHero, IEnumerable<EquipmentData> equippedEquipments)
            => CalculateAttackPower(selectedHero.HeroType, selectedHero.Grade, selectedHero.Level, equippedEquipments);

        public static float CalculateAttackPower(HeroType heroType, Grade grade, int level, IEnumerable<EquipmentData> equippedEquipments)
            => CalculateHeroBasicAttackPower(heroType, grade, level)
               + SumEquipmentStats(equippedEquipments, StatType.AttackPower);

        public static float CalculateMaxHp(HeroData selectedHero, IEnumerable<EquipmentData> equippedEquipments)
            => CalculateMaxHp(selectedHero.HeroType, selectedHero.Grade, selectedHero.Level, equippedEquipments);

        public static float CalculateMaxHp(HeroType heroType, Grade grade, int level, IEnumerable<EquipmentData> equippedEquipments)
            => CalculateHeroBasicMaxHp(heroType, grade, level)
               + SumEquipmentStats(equippedEquipments, StatType.MaxHP);

        public static float CalculateHeroBasicAttackPower(HeroData heroData)
            => CalculateHeroBasicAttackPower(heroData.HeroType, heroData.Grade, heroData.Level);

        public static float CalculateHeroBasicAttackPower(HeroType heroType, Grade grade, int level)
            => CalculateHeroBasicAttackPower(StaticDataRepository.Instance.Heroes.Get(heroType).Rarity, grade, level);

        public static float CalculateHeroBasicAttackPower(Rarity rarity, Grade grade, int level)
        {
            var stat = StaticDataRepository.Instance.Heroes.GetHeroStat(rarity, grade);
            return stat.AttackPowerDefaultValue + stat.AttackPowerIncrementalValue * (level - 1);
        }

        public static float CalculateHeroBasicMaxHp(HeroData heroData)
            => CalculateHeroBasicMaxHp(heroData.HeroType, heroData.Grade, heroData.Level);

        public static float CalculateHeroBasicMaxHp(HeroType heroType, Grade grade, int level)
            => CalculateHeroBasicMaxHp(StaticDataRepository.Instance.Heroes.Get(heroType).Rarity, grade, level);

        public static float CalculateHeroBasicMaxHp(Rarity rarity, Grade grade, int level)
        {
            var stat = StaticDataRepository.Instance.Heroes.GetHeroStat(rarity, grade);
            return stat.MaxHpDefaultValue + stat.MaxHpIncrementalValue * (level - 1);
        }

        public static (StatType StatType, float CalculatedValue) CalculateEquipmentStat(EquipmentData equipmentData)
        {
            var equipment = StaticDataRepository.Instance.Equipments.Get(equipmentData.EquipmentId);
            return CalculateEquipmentStat(equipment.Slot, equipment.Rarity, equipmentData.Grade, equipmentData.Level);
        }

        public static (StatType StatType, float CalculatedValue) CalculateEquipmentStat(EquipmentSlot slot, Rarity rarity, Grade grade, int level)
        {
            var stat = StaticDataRepository.Instance.Equipments.GetEquipmentStat(slot, rarity, grade);
            return (stat.StatType, stat.DefaultValue + stat.IncrementalValue * (level - 1));
        }

        private static float SumEquipmentStats(IEnumerable<EquipmentData> equippedEquipments, StatType statType)
        {
            float sum = 0f;
            foreach (var equipment in equippedEquipments)
            {
                var (type, value) = CalculateEquipmentStat(equipment);
                if (type == statType)
                {
                    sum += value;
                }
            }
            return sum;
        }
    }
}
