using System.Numerics;

namespace TapResult.Readers;

internal sealed class DeltaColumnReader<T> : IColumnReader<T>
    where T : INumber<T>
{
    private readonly IColumnReader<T> _deltaReader;
    private readonly T _baseValue;

    public DeltaColumnReader(IColumnReader deltaReader, int length, LogicalType type, T baseValue)
    {
        if (deltaReader is not IColumnReader<T> columnReader)
            throw new ArgumentException($"{nameof(deltaReader)} not a {nameof(IColumnReader<T>)}");
        _deltaReader = columnReader;
        _baseValue = baseValue;
        Length = length;
        Type = type;
        _currentValue = _baseValue;
    }

    public int Length { get; }
    public int Index { get; private set; }
    public LogicalType Type { get; }

    private T _currentValue;
    private int _deltaIndex;

    public void Advance(int units)
    {
        for (int i = 0; i < units; i++)
        {
            if (_deltaIndex < _deltaReader.Length)
            {
                _currentValue += _deltaReader.Peek(_deltaIndex);
                _deltaIndex++;
            }
        }
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
        return new DeltaColumnReader<T>(_deltaReader.Clone(), Length, Type, _baseValue)
        {
            Index = Index,
            _currentValue = _currentValue,
            _deltaIndex = _deltaIndex,
        };
    }

    IColumnReader IColumnReader.Clone()
    {
        return Clone();
    }

    public T Peek(int offset = 0)
    {
        if (offset == 0)
            return _currentValue;

        T value = _currentValue;
        for (int i = 0; i < offset; i++)
            value += _deltaReader.Peek(_deltaIndex + i);
        return value;
    }

    public IEnumerable<T> Peek(int offset, int count)
    {
        T value = _currentValue;
        for (int i = 0; i < offset; i++)
            value += _deltaReader.Peek(_deltaIndex + i);

        for (int i = 0; i < count; i++)
        {
            yield return value;
            if (_deltaIndex + offset + i < _deltaReader.Length)
                value += _deltaReader.Peek(_deltaIndex + offset + i);
        }
    }
}
