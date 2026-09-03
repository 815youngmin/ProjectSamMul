#nullable enable

namespace Shared.GameDataTypes
{
    /// <summary>One reward line shown to the player. Only the fields relevant to the reward type are set.</summary>
    public readonly struct RewardItemData
    {
        public readonly RewardItemType RewardItemType;
        public readonly long Amount;
        public readonly EquipmentSlot? SlotType;
        public readonly Grade? Grade;
        public readonly EquipmentId? EquipmentId;
        public readonly HeroType? CharacterType;

        private RewardItemData(
            RewardItemType rewardItemType,
            long amount,
            EquipmentSlot? slotType = null,
            Grade? grade = null,
            EquipmentId? equipmentId = null,
            HeroType? characterType = null)
        {
            RewardItemType = rewardItemType;
            Amount = amount;
            SlotType = slotType;
            Grade = grade;
            EquipmentId = equipmentId;
            CharacterType = characterType;
        }

        public static RewardItemData Create(RewardItemType rewardItemType, long amount)
            => new RewardItemData(rewardItemType, amount);

        public static RewardItemData CreateForCharacter(HeroType heroType, Grade grade, long amount)
            => new RewardItemData(RewardItemType.Character, amount, grade: grade, characterType: heroType);
        public static RewardItemData CreateForEquipment(EquipmentId equipmentId, Grade grade, long amount)
            => new RewardItemData(RewardItemType.Equipment, amount, grade: grade, equipmentId: equipmentId);
    }
}
