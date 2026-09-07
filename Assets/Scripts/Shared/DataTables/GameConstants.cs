#nullable enable
using System;
using System.Collections.Generic;
using Shared.GameDataTypes;

namespace Shared.DataTables
{
    public static class GameConstants
    {
        // Distances in Unity units.
        public static readonly float AI_TARGET_SEARCH_MAX_DISTANCE = 48f;
        public static readonly float PC_TARGET_SEARCH_MAX_DISTANCE = 20f;
        public static readonly float ZONE_WIDTH = 5f;
        public static readonly float ZONE_HEIGHT = 5f;

        public static readonly int SKILL_MAX_LEVEL = 5;
        public static readonly int SKILL_TRANSCENDENT_LEVEL = SKILL_MAX_LEVEL + 1;
        public static readonly int SKILLSLOT_NUMBER_OF_ACTIVE_SLOTS = 5;
        public static readonly int SKILLSLOT_NUMBER_OF_PASSIVE_SLOTS = 5;

        /// <summary>Every skill a hero can draw from in a stage, before chapter locks are applied.</summary>
        public static readonly IReadOnlyList<SkillId> SKILLDECK_DEFAULT_SEASON_SKILLDECK = new List<SkillId>
        {
            SkillId.SpinBlade, SkillId.Glutton, SkillId.PlasmaDrill, SkillId.DeathTouch, SkillId.RangeUp,
            SkillId.AttackSpeedUp, SkillId.ShockBomb, SkillId.BouncingClaw, SkillId.ShootingStar, SkillId.MoveSpeedUp,
            SkillId.MaxHPUp, SkillId.DamageUp, SkillId.Meteor,
        };

        public static readonly IReadOnlyList<EquipmentSlot> VALID_EQUIPMENT_SLOTS = new List<EquipmentSlot>
        {
            EquipmentSlot.Armor, EquipmentSlot.Gloves, EquipmentSlot.Shoes, EquipmentSlot.Necklace, EquipmentSlot.Ring,
        };

        public static readonly IReadOnlyList<Grade> GRADES_WITH_EFFECT = new List<Grade> { Grade.D, Grade.C, Grade.B, Grade.A, Grade.S, Grade.SS };
        public static readonly Grade IMPLEMENTED_MAX_GRADE = Grade.SS;

        /// <summary>Gem amounts at or above this are displayed in shortened form.</summary>
        public static readonly long GEM_THRESHOLD_TO_SHORTEN = 100000;

        /// <summary>Highest hero level reachable at a grade.</summary>
        public static int MaxLevel(this Grade grade) => grade switch
        {
            Grade.D => 10,
            Grade.C => 20,
            Grade.B => 30,
            Grade.A => 50,
            Grade.A1 => 60,
            Grade.A2 => 70,
            Grade.S => 80,
            Grade.S1 => 90,
            Grade.S2 => 100,
            Grade.S3 => 110,
            Grade.SS => 120,
            Grade.SS1 => 130,
            Grade.SS2 => 140,
            Grade.SS3 => 150,
            _ => throw new ArgumentOutOfRangeException(nameof(grade), grade, null),
        };

        /// <summary>Grade a hero or equipment of this rarity starts at when acquired.</summary>
        public static Grade InitialGrade(this Rarity rarity) => rarity == Rarity.Special ? Grade.A : Grade.D;
    }
}
