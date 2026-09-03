#nullable enable

namespace Shared.GameDataTypes
{
    public enum StageType
    {
        Chapter = 0,
    }

    public enum StageFormType
    {
        Infinite = 0,
        Rectangle = 1,
        Vertical = 2,
    }

    public enum SpawnLogicType
    {
        FromEveryBoundary = 0,
        FromThreeQuadrant = 1,
        FromVerticalBoundary = 2,
    }

    public enum RewardGoblinType
    {
        Gold = 0,
        Gem = 1,
    }

    public enum StatType
    {
        MaxHP = 0,
        AttackPower = 1,
    }
}
