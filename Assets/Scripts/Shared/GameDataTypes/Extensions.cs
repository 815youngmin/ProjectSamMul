#nullable enable
using System;
using System.Collections.Generic;

namespace Shared.GameDataTypes
{
    public static class HeroTypeExtension
    {
        // Hero bodies carry the same name in both enums.
        private static readonly Dictionary<HeroType, CharacterType> s_toCharacter = Build();

        private static Dictionary<HeroType, CharacterType> Build()
        {
            var map = new Dictionary<HeroType, CharacterType>();
            foreach (HeroType hero in Enum.GetValues(typeof(HeroType)))
            {
                if (hero != HeroType.Invalid && Enum.TryParse<CharacterType>(hero.ToString(), out var character))
                {
                    map.Add(hero, character);
                }
            }
            return map;
        }

        public static CharacterType ToCharacterType(this HeroType type)
            => s_toCharacter.TryGetValue(type, out var character) ? character : throw new ArgumentOutOfRangeException(nameof(type), type, null);

        public static HeroType ToHeroType(this CharacterType type)
        {
            foreach (var pair in s_toCharacter)
            {
                if (pair.Value == type)
                {
                    return pair.Key;
                }
            }
            throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        public static bool IsHeroType(this CharacterType type) => s_toCharacter.ContainsValue(type);
    }

    public static class DropItemExtension
    {
        public static bool IsExpItem(this DropItemType dropItemType) => dropItemType switch
        {
            DropItemType.ExpS or DropItemType.ExpM or DropItemType.ExpL or DropItemType.ExpXL => true,
            _ => false,
        };
    }

    /// <summary>Maps reward identities onto <see cref="ItemType"/> (the icon/description identity of an item).</summary>
    public static class ItemTypeExtension
    {
        public static ItemType ToItemType(this RewardItemType type) => type switch
        {
            RewardItemType.Exp => ItemType.AccountExp,
            RewardItemType.EquipmentTicket => ItemType.RandomEquipmentReinforceTicket,
            _ => ParseByName(type.ToString()),
        };

        public static ItemType ToItemType(this HeroType type) => ParseByName(type.ToString());

        public static ItemType ToItemType(this EquipmentId type) => ParseByName(type.ToString());

        private static ItemType ParseByName(string name)
            => Enum.TryParse<ItemType>(name, out var item) ? item : throw new NotImplementedException($"{name} has no item type.");
    }
}
