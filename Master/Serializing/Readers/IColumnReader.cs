namespace Master.Serializing.Readers;

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
    public IEnumerable<T> Peek(int count, int offset);

    public T Read()
    {
        T value = Peek();
        Advance(1);
        return value;
    }

    public IEnumerable<T> Read(int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return Read();
        }
    }
}