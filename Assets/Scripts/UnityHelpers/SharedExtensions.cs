#nullable enable
using Shared.GameDataTypes;

namespace Z.UnityHelpers
{
    /// <summary>Shared 데이터 타입을 UI 리소스/표시 문자열로 바꾸는 확장 메서드.</summary>
    public static class IconPathExtensions
    {
        public static string IconPath(this ElementType element) => element switch
        {
            ElementType.Water => "Commons/Icons/Elements/icon_element_water.png",
            ElementType.Wind => "Commons/Icons/Elements/icon_element_wind.png",
            ElementType.Earth => "Commons/Icons/Elements/icon_element_earth.png",
            ElementType.Fire => "Commons/Icons/Elements/icon_element_fire.png",
            _ => "Commons/Icons/Elements/icon_element_none.png",
        };

        public static string ToDisplayText(this ElementType element) => element.ToString();

        public static string IconPath(this EquipmentSlot slot) => $"Commons/Icons/EquipmentSlots/icon_slot_{slot.ToString().ToLowerInvariant()}.png";

        public static string IconPath(this StatType statType) => $"Commons/Icons/Stats/icon_stat_{statType.ToString().ToLowerInvariant()}.png";
    }

    public static class GradeExtension
    {
        public static string ToDisplayText(this Grade grade) => grade.ToString();
    }

    public static class NumericTextExtensions
    {
        /// <summary>큰 수를 1.2K / 3.4M 형식으로 줄인다. <paramref name="threshold"/> 미만이면 그대로 표시한다.</summary>
        public static string ToShortNumericText(this long value, long threshold = 0)
        {
            if (threshold > 0 && value < threshold)
            {
                return value.ToString("N0");
            }
            if (value >= 1_000_000_000) return (value / 1_000_000_000f).ToString("0.#") + "B";
            if (value >= 1_000_000) return (value / 1_000_000f).ToString("0.#") + "M";
            if (value >= 10_000) return (value / 1_000f).ToString("0.#") + "K";
            return value.ToString("N0");
        }

        public static string ToShortNumericText(this int value, long threshold = 0) => ((long)value).ToShortNumericText(threshold);
    }
}
