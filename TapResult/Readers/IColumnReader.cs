namespace TapResult.Readers;

/// <summary>
/// TODO
/// </summary>
public interface IColumnReader
{
    /// <summary>
    /// TODO
    /// </summary>
    public bool IsAtEnd => Index >= Length;
    /// <summary>
    /// TODO
    /// </summary>
    public int Length { get; }
    /// <summary>
    /// TODO
    /// </summary>
    public int Index { get; }
    /// <summary>
    /// TODO
    /// </summary>
    public void Advance(int units);
    
    // TODO: Create peek and read returning obj instead of T
}

/// <summary>
/// TODO
/// </summary>
public interface IColumnReader<T> : IColumnReader
{
    /// <summary>
    /// TODO
    /// </summary>
    public T Peek(int offset = 0);
    /// <summary>
    /// TODO
    /// </summary>
    public IEnumerable<T> Peek(int offset, int count);
}

/// <summary>
/// TODO
/// </summary>
public static class ColumnReaderExtensions {
    /// <summary>
    /// TODO
    /// </summary>
    public static T Read<T>(this IColumnReader<T> reader)
    {
        T value = reader.Peek();
        reader.Advance(1);
        return value;
    }

    /// <summary>
    /// TODO
    /// </summary>
    public static IEnumerable<T> Read<T>(this IColumnReader<T> reader, int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return reader.Read();
        }
    }
}