#nullable enable
using System;
using System.ComponentModel;
using System.Globalization;

namespace Shared.GameDataTypes
{
    /// <summary>Identity of one hero instance in a user's inventory. Serializes as a GUID string (also as a dictionary key).</summary>
    [TypeConverter(typeof(HeroInstanceIdConverter))]
    public readonly struct HeroInstanceId : IEquatable<HeroInstanceId>
    {
        public static readonly HeroInstanceId Invalid = new HeroInstanceId(Guid.Empty);

        public Guid RawValue { get; }

        public HeroInstanceId(Guid rawValue) => RawValue = rawValue;
        public HeroInstanceId(string guidString) => RawValue = Guid.Parse(guidString);

        public static HeroInstanceId CreateNew() => new HeroInstanceId(Guid.NewGuid());

        public bool Equals(HeroInstanceId other) => RawValue == other.RawValue;
        public override bool Equals(object? obj) => obj is HeroInstanceId other && Equals(other);
        public override int GetHashCode() => RawValue.GetHashCode();
        public static bool operator ==(HeroInstanceId lhs, HeroInstanceId rhs) => lhs.RawValue == rhs.RawValue;
        public static bool operator !=(HeroInstanceId lhs, HeroInstanceId rhs) => lhs.RawValue != rhs.RawValue;
        public override string ToString() => RawValue.ToString();
    }

    /// <summary>Identity of one equipment instance in a user's inventory. Serializes as a GUID string (also as a dictionary key).</summary>
    [TypeConverter(typeof(EquipmentInstanceIdConverter))]
    public readonly struct EquipmentInstanceId : IEquatable<EquipmentInstanceId>
    {
        public static readonly EquipmentInstanceId Invalid = new EquipmentInstanceId(Guid.Empty);

        public Guid RawValue { get; }

        public EquipmentInstanceId(Guid rawValue) => RawValue = rawValue;
        public EquipmentInstanceId(string guidString) => RawValue = Guid.Parse(guidString);

        public static EquipmentInstanceId CreateNew() => new EquipmentInstanceId(Guid.NewGuid());

        public bool Equals(EquipmentInstanceId other) => RawValue == other.RawValue;
        public override bool Equals(object? obj) => obj is EquipmentInstanceId other && Equals(other);
        public override int GetHashCode() => RawValue.GetHashCode();
        public static bool operator ==(EquipmentInstanceId lhs, EquipmentInstanceId rhs) => lhs.RawValue == rhs.RawValue;
        public static bool operator !=(EquipmentInstanceId lhs, EquipmentInstanceId rhs) => lhs.RawValue != rhs.RawValue;
        public override string ToString() => RawValue.ToString();
    }

    /// <summary>Lets Newtonsoft.Json use the ids as dictionary keys (string form).</summary>
    public class HeroInstanceIdConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string);
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => new HeroInstanceId((string)value);
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) => value.ToString()!;
    }

    public class EquipmentInstanceIdConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string);
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => new EquipmentInstanceId((string)value);
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) => value.ToString()!;
    }
}
