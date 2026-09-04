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
}
