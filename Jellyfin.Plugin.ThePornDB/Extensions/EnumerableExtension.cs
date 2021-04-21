using System.Collections.Generic;
using System.Linq;

internal static class EnumerableExtension
{
    public static IEnumerable<(int index, T item)> WithIndex<T>(this IEnumerable<T> source)
        => source.Select((item, index) => (index, item));
}
