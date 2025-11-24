namespace Master.Benchmarks.Extensions;

internal static class EnumerableExtensions
{
    public static Dictionary<TKey, TValue> TryToDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> enumerable,
        Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector) where TKey : notnull
    {
        Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();
        foreach (TSource source in enumerable)
        {
            TKey key = keySelector(source);
            TValue value = valueSelector(source);
            dictionary.TryAdd(key, value);
        }

        return dictionary;
    }
}