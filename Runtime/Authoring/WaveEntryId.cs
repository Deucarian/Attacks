using System;
using Deucarian.GameplayFoundation;

namespace Deucarian.Attacks.Authoring
{
    public readonly struct WaveEntryId : IEquatable<WaveEntryId>, IComparable<WaveEntryId>
    {
        private readonly string _value;

        public WaveEntryId(string value)
        {
            _value = new ContentId(value).Value;
        }

        private WaveEntryId(string value, bool serialized)
        {
            _value = value ?? string.Empty;
        }

        public string Value => _value ?? string.Empty;
        public bool IsEmpty => string.IsNullOrEmpty(Value);

        public bool IsValid
        {
            get
            {
                return TryCreate(Value, out _);
            }
        }

        public static WaveEntryId CreateNew()
        {
            return new WaveEntryId("entry-" + Guid.NewGuid().ToString("N"));
        }

        public static bool TryCreate(string value, out WaveEntryId entryId)
        {
            try
            {
                entryId = new WaveEntryId(value);
                return true;
            }
            catch (ArgumentException)
            {
                entryId = default;
                return false;
            }
        }

        internal static WaveEntryId FromSerialized(string value)
        {
            return new WaveEntryId(value, true);
        }

        public bool Equals(WaveEntryId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is WaveEntryId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public int CompareTo(WaveEntryId other)
        {
            return string.Compare(Value, other.Value, StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(WaveEntryId left, WaveEntryId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(WaveEntryId left, WaveEntryId right)
        {
            return !left.Equals(right);
        }
    }
}
