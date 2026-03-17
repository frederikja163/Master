namespace TapResult.Benchmarks;

internal sealed class Disposable<T> : IDisposable
{
    private readonly Action<T> _action;
    
    public Disposable(T value, Action<T> action)
    {
        Value = value;
        _action = action;
    }

    public static implicit operator T(Disposable<T> disposable)
    {
        return disposable.Value;
    }
    
    public T Value { get; set; }

    public void Dispose()
    {
        _action.Invoke(Value);
    }
}

internal sealed class DisposableList<T> : List<Disposable<T>>, IDisposable
{
    public DisposableList(IEnumerable<T> elements, Action<T> action)
    {
        AddRange(elements.Select(e => new Disposable<T>(e, action)));
    }
    
    public void Dispose()
    {
        foreach (Disposable<T> disposable in this)
        {
            disposable.Dispose();
        }
    }
}

internal static class DisposableListExtensions
{
    public static DisposableList<T> ToDisposableList<T>(this IEnumerable<T> enumerable, Action<T> action)
    {
        return new DisposableList<T>(enumerable, action);
    }
}