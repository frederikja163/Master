namespace TapResult.Readers;

internal sealed class NullReader<T> : IColumnReader<T?>
    where T : class
{
    private readonly IColumnReader<byte> _nullReader;
    private readonly IColumnReader<T> _valueReader;

    public NullReader(IColumnReader<byte> nullReader, IColumnReader<T> valueReader, int length)
    {
        _nullReader = nullReader;
        _valueReader = valueReader;
        Length = length;
    }

    public int Length { get; }
    public int Index { get; private set; } = 0;

    
    private int IsNull(int offset)
    {
        int index = Index + offset;
        int byteIndex = index / 8;
        int bitIndex = index % 8;
        byte currentNulls = _nullReader.Peek(_nullReader.Index - byteIndex);

        // We want to advance if the value is 0, so we use xor to flip the bit after shifting.
        return (currentNulls >> bitIndex) ^ 1;
    }
    
    public void Advance(int units)
    {
        int advancedUnits = 0;
        for (int i = 0; i < units; i++)
        {
            advancedUnits += IsNull(0);
            Index += 1;
            if (Index % 8 == 0)
            {
                _nullReader.Advance(1);
            }
        }

        if (advancedUnits > 0)
        {
            _valueReader.Advance(advancedUnits);
        }
    }

    public T? Peek(int offset = 0)
    {
        if (IsNull(offset) != 0)
        {
            return null;
        }
        
        int valueOffset = 0;
        for (int i = 0; i < offset; i++)
        {
            valueOffset += IsNull(i);
        }

        return _valueReader.Peek(valueOffset);
    }

    public IEnumerable<T?> Peek(int offset, int count)
    {
        int valueOffset = 0;
        for (int i = 0; i < offset; i++)
        {
            valueOffset += IsNull(i);
        }

        for (int i = 0; i < count; i++)
        {
            if (IsNull(i) != 0)
            {
                valueOffset += 1;
                yield return null;
            }
            else
            {
                yield return _valueReader.Peek(valueOffset);
            }
        }
    }

    object? IColumnReader.Peek(int offset)
    {
        return Peek(offset);
    }

    IEnumerable<object?> IColumnReader.Peek(int offset, int count)
    {
        return Peek(offset, count);
    }
}