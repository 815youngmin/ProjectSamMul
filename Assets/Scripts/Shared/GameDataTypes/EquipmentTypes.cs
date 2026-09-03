#nullable enable

namespace Shared.GameDataTypes
{
    public enum EquipmentSlot
    {
        Invalid = -1,
        Armor = 0,
        Gloves = 1,
        Shoes = 2,
        Necklace = 3,
        Ring = 4,
    }

    public enum EquipmentId
    {
        Invalid = -1,
        PredatorArmor = 0,
        InvaderArmor = 1,
        GuardianArmor = 2,
        AvengerArmor = 3,
        TimeRulerArmor = 4,
        PredatorGloves = 500,
        InvaderGloves = 501,
        GuardianGloves = 502,
        AvengerGloves = 503,
        TimeRulerGloves = 504,
        PredatorShoes = 1000,
        InvaderShoes = 1001,
        GuardianShoes = 1002,
        AvengerShoes = 1003,
        TimeRulerShoes = 1004,
        PredatorNecklace = 1500,
        InvaderNecklace = 1501,
        GuardianNecklace = 1502,
        AvengerNecklace = 1503,
        TimeRulerNecklace = 1504,
        PredatorRing = 2000,
        InvaderRing = 2001,
        GuardianRing = 2002,
        AvengerRing = 2003,
        TimeRulerRing = 2004,
    }

    public enum EquipmentSetType
    {
        None = 0,
        Predator = 1,
        Invader = 2,
        Guardian = 3,
        Avenger = 4,
        TimeRuler = 5,
    }
}
