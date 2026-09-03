using System;

namespace SamMul.GameClients.Stages.AreaIndicators
{
    public readonly struct AreaIndicatorInstanceID : IEquatable<AreaIndicatorInstanceID>
    {
        public readonly long RawValue;

        public static readonly AreaIndicatorInstanceID Invalid = new AreaIndicatorInstanceID(-1);

        public AreaIndicatorInstanceID(long rawValue)
        {
            this.RawValue = rawValue;
        }

        public override bool Equals(object obj) => obj is AreaIndicatorInstanceID other && this.Equals(other);

        public bool Equals(AreaIndicatorInstanceID other) => this.RawValue == other.RawValue;

        public override int GetHashCode() => this.RawValue.GetHashCode();

        public static bool operator ==(AreaIndicatorInstanceID lhs, AreaIndicatorInstanceID rhs) => lhs.Equals(rhs);

        public static bool operator !=(AreaIndicatorInstanceID lhs, AreaIndicatorInstanceID rhs) => !(lhs == rhs);
    }

}
