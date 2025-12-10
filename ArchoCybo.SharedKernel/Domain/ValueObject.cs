using System.Collections.Generic;
using System.Linq;

namespace ArchoCybo.SharedKernel.Domain;

/// <summary>
/// Base class for value objects
/// </summary>
public abstract class ValueObject
{
    protected abstract IEnumerable<object> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != GetType()) return false;

        var other = (ValueObject)obj;

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            foreach (var obj in GetEqualityComponents())
            {
                hash = hash * 23 + (obj?.GetHashCode() ?? 0);
            }
            return hash;
        }
    }

    public static bool operator ==(ValueObject? a, ValueObject? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        return a.Equals(b);
    }

    public static bool operator !=(ValueObject? a, ValueObject? b) => !(a == b);
}
