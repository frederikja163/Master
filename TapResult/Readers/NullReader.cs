namespace TapResult.Readers;

internal abstract class NullReaderBase : IColumnReader
{
    private readonly IColumnReader<byte> _nullReader;
    private readonly IColumnReader _valueReader;

    protected NullReaderBase(IColumnReader<byte> nullReader, IColumnReader valueReader, int length)
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
        return (currentNulls >> bitIndex) & 1;
    }
    
    public void Advance(int units)
    {
        int advancedUnits = 0;
        for (int i = 0; i < units; i++)
        {
            advancedUnits += IsNull(0) ^ 1;
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

    public object? Peek(int offset = 0)
    {
        if (IsNull(offset) != 0)
        {
            return null;
        }
        
        int valueOffset = 0;
        for (int i = 0; i < offset; i++)
        {
            valueOffset += IsNull(i) ^ 1;
        }

        return _valueReader.Peek(valueOffset);
    }

    public IEnumerable<object?> Peek(int offset, int count)
    {
        int valueOffset = 0;
        for (int i = 0; i < offset; i++)
        {
            valueOffset += IsNull(i) ^ 1;
        }

        for (int i = 0; i < count; i++)
        {
            if (IsNull(i) != 0)
            {
                yield return null;
            }
            else
            {
                valueOffset += 1;
                yield return _valueReader.Peek(valueOffset);
            }
        }
    }
}

internal sealed class NullReaderValType<T> : NullReaderBase, IColumnReader<T?>
    where T : struct
{
    public NullReaderValType(IColumnReader<byte> nullReader, IColumnReader valueReader, int length) : base(nullReader, valueReader, length)
    {
    }

    // TODO: This causes unnecessary boxing, that we would like to avoid.
    public new T? Peek(int offset = 0)
    {
        return (T?)base.Peek(offset);
    }

    public new IEnumerable<T?> Peek(int offset, int count)
    {
        return base.Peek(offset, count).Cast<T?>();
    }
}

internal sealed class NullReaderRefType<T> : NullReaderBase, IColumnReader<T?>
    where T : class
{
    public NullReaderRefType(IColumnReader<byte> nullReader, IColumnReader valueReader, int length) : base(nullReader, valueReader, length)
    {
    }

    public new T? Peek(int offset = 0)
    {
        return (T?)base.Peek(offset);
    }

    public new IEnumerable<T?> Peek(int offset, int count)
    {
        return base.Peek(offset, count).Cast<T?>();
    }
}