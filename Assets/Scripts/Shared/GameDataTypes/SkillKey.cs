#nullable enable
using System;

namespace Shared.GameDataTypes
{
    /// <summary>Identifies one level of one skill inside the skill table.</summary>
    public readonly struct SkillKey : IEquatable<SkillKey>
    {
        public readonly SkillId Id;
        public readonly int Level; // 1-based

        public SkillKey(SkillId skillId, int level)
        {
            Id = skillId;
            Level = level;
        }

        public bool Equals(SkillKey other) => Id == other.Id && Level == other.Level;
        public override bool Equals(object? obj) => obj is SkillKey other && Equals(other);
        public override int GetHashCode() => ((int)Id * 397) ^ Level;
        public static bool operator ==(SkillKey lhs, SkillKey rhs) => lhs.Equals(rhs);
        public static bool operator !=(SkillKey lhs, SkillKey rhs) => !lhs.Equals(rhs);
        public override string ToString() => $"{Id}:{Level}";
    }
}
