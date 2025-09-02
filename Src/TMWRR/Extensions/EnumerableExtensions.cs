using System.Collections.Immutable;

namespace TMWRR.Extensions;

public static class EnumerableExtensions
{
    public static IEnumerable<T>? NullIfEmpty<T>(this IEnumerable<T> source)
    {
        if (source is null || !source.Any())
        {
            return null;
        }

        return source;
    }

    public static ImmutableList<T>? ToNullableImmutableListIfEmpty<T>(this IEnumerable<T> source)
    {
        return NullIfEmpty(source)?.ToImmutableList();
    }
}
