using System.Numerics;

namespace TapResult.Readers;

public class DictionaryColumnReader<T> : IColumnReader<T>
{
    private readonly IColumnReader<T> _dictionaryColumn;
    private readonly IColumnReader<int> _indexColumn;

    public DictionaryColumnReader(IColumnReader dictColumn, IColumnReader<int> indexColumn, int length, LogicalType type)
    {
        if (dictColumn is not IColumnReader<T> columnReader)
            throw new ArgumentException($"{nameof(dictColumn)} not a {nameof(IColumnReader<T>)}");
        _dictionaryColumn = columnReader;
        _indexColumn = indexColumn;
        Length = length;
        Type = type;
    }

    public int Length { get; }
    public int Index { get; private set; }
    public LogicalType Type { get; }

    public void Advance(int units)
    {
        _indexColumn.Advance(units);
        Index += units;
    }

    object? IColumnReader.Peek(int offset)
    {
        return Peek(offset);
    }

    IEnumerable<object?> IColumnReader.Peek(int offset, int count)
    {
        return Peek(offset, count).OfType<object?>();
    }

    public IColumnReader<T> Clone()
    {
        return new DictionaryColumnReader<T>(_dictionaryColumn.Clone(), _indexColumn.Clone(), Length, Type)
        {
            Index = Index,
        };
    }

    IColumnReader IColumnReader.Clone()
    {
        return Clone();
    }

    public T Peek(int offset = 0)
    {
        int idx = _indexColumn.Peek(offset);
        return _dictionaryColumn.Peek(idx);
    }

    public IEnumerable<T> Peek(int offset, int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return Peek(offset + i);
        }
    }
}
