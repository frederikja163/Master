namespace TapResult.Extensions;

public static class EnumerableExtensions
{
    public static T PickRandom<T>(this IEnumerable<T> enumerable)
    {
        if (enumerable.TryGetNonEnumeratedCount(out int count))
        {
            return enumerable.Skip(Random.Shared.Next(0, count)).First();
        }

        return enumerable.ToArray().PickRandom();
    }
}