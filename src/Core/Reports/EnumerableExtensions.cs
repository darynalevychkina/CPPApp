using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Reports;

public static class EnumerableExtensions
{
    // top-N by a key, largest first; the optional tie-breaker keeps the order stable when keys are equal
    public static IEnumerable<T> TopBy<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector,
        int n,
        Func<T, string>? tieBreaker = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentOutOfRangeException.ThrowIfNegative(n);

        IOrderedEnumerable<T> ordered = source.OrderByDescending(keySelector);
        if (tieBreaker is not null)
            ordered = ordered.ThenBy(tieBreaker, StringComparer.Ordinal);

        return ordered.Take(n);
    }
}