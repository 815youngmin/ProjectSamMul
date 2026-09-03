#nullable enable
using Shared.GameDataTypes;

namespace Shared.GameLogics
{
    /// <summary>Element cycle: Wind beats Water, Earth beats Wind, Fire beats Earth, Water beats Fire. Water/Earth and Wind/Fire are even.</summary>
    public static class ElementLogic
    {
        public static ElementComparisonResult Compare(ElementType left, ElementType right)
        {
            if (left == right)
            {
                return ElementComparisonResult.Same;
            }
            if (right == GetWeakerElement(left))
            {
                return ElementComparisonResult.Dominant;
            }
            if (right == GetStrongerElement(left))
            {
                return ElementComparisonResult.Recessive;
            }
            return ElementComparisonResult.Equal;
        }

        /// <summary>The element this one beats.</summary>
        public static ElementType GetWeakerElement(ElementType element) => element switch
        {
            ElementType.Water => ElementType.Fire,
            ElementType.Wind => ElementType.Water,
            ElementType.Earth => ElementType.Wind,
            _ => ElementType.Earth,
        };

        /// <summary>The element that beats this one.</summary>
        public static ElementType GetStrongerElement(ElementType element) => element switch
        {
            ElementType.Water => ElementType.Wind,
            ElementType.Wind => ElementType.Earth,
            ElementType.Earth => ElementType.Fire,
            _ => ElementType.Water,
        };
    }
}
