#nullable enable
using System.Collections.Generic;
using Shared.GameDataTypes;
using Shared.UserDatas;

namespace Shared.GameLogics
{
    /// <summary>Read view over the user's equipment and what the selected hero wears.</summary>
    public readonly struct EquipmentInventory
    {
        public readonly IReadOnlyDictionary<EquipmentInstanceId, EquipmentData> Equipments;
        public readonly IReadOnlyDictionary<EquipmentSlot, EquipmentInstanceId> EquipmentSlots;

        public EquipmentInventory(IReadOnlyDictionary<EquipmentInstanceId, EquipmentData> equipments, IReadOnlyDictionary<EquipmentSlot, EquipmentInstanceId> equippedEquipments)
        {
            Equipments = equipments;
            EquipmentSlots = equippedEquipments;
        }

        public IReadOnlyDictionary<EquipmentSlot, EquipmentData> GetEquippedEquipments()
        {
            var result = new Dictionary<EquipmentSlot, EquipmentData>();
            foreach (var pair in EquipmentSlots)
            {
                if (Equipments.TryGetValue(pair.Value, out var equipment))
                {
                    result.Add(pair.Key, equipment);
                }
            }
            return result;
        }
    }

    /// <summary>Read view over the user's heroes.</summary>
    public readonly struct ReadOnlyHeroInventory
    {
        public readonly IReadOnlyDictionary<HeroInstanceId, HeroData> Heroes;
        public readonly HeroInstanceId SelectedHero;

        public ReadOnlyHeroInventory(IReadOnlyDictionary<HeroInstanceId, HeroData> heroes, HeroInstanceId selectedHero)
        {
            Heroes = heroes;
            SelectedHero = selectedHero;
        }

        public HeroData SelectedHeroData => Heroes[SelectedHero];
    }

    public interface IHeroInventory : IEnumerable<HeroData>
    {
        HeroData MainHero { get; }
        IEnumerable<HeroData> SubHeroes { get; }
    }

    public interface IEquipmentInventory : IEnumerable<EquipmentData>
    {
    }
}
