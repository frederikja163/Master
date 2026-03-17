namespace Master.Readers;

public interface IColumnReader
{
    public bool IsAtEnd => Index >= Length;
    public int Length { get; }
    public int Index { get; }
    public void Advance(int units);
}

public interface IColumnReader<T> : IColumnReader
{
    public T Peek(int offset = 0);
    public IEnumerable<T> Peek(int offset, int count);
}

public static class ColumnReaderExtensions {
    public static T Read<T>(this IColumnReader<T> reader)
    {
        T value = reader.Peek();
        reader.Advance(1);
        return value;
    }

    public static IEnumerable<T> Read<T>(this IColumnReader<T> reader, int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return reader.Read();
        }
    }
}