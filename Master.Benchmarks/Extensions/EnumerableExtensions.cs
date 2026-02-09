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

    public static IEnumerable<T> ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
    {
        foreach (T value in enumerable)
        {
            action(value);
            yield return value;
        }
    }

    public static IEnumerable<T?> WithNullsClass<T>(this IEnumerable<T> enumerable, float sparsity)
        where T : class
    {
        foreach (T value in enumerable)
        {
            yield return Random.Shared.NextSingle() < sparsity ? value : null;
        }
    }
    public static IEnumerable<T?> WithNullsStruct<T>(this IEnumerable<T> enumerable, float sparsity)
        where T : struct
    {
        foreach (T value in enumerable)
        {
            yield return Random.Shared.NextSingle() < sparsity ? value : null;
        }
    }
}