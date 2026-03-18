using System.Numerics;
using System.Runtime.CompilerServices;

namespace TapResult.Readers;

internal sealed class BitPackingColumnReader<T> : IColumnReader<T>
    where T : unmanaged, INumber<T>, IBinaryInteger<T>, IMinMaxValue<T>
{
    private static readonly int BitSize = Unsafe.SizeOf<T>() * 8;
    public byte PrefixLength { get; }
    public T Prefix { get; }
    public LogicalType Type { get; }
    public IColumnReader<T> ColumnReader { get; }
    public int Length { get; }
    public int Index { get; private set;  } = 0;
    private readonly int _valueSize;
    private readonly T _valueMask;

    public BitPackingColumnReader(IColumnReader reader, int logicalLength, LogicalType type, byte prefixLength, T prefix)
    {
        if (reader is not IColumnReader<T> columnReader)
            throw new ArgumentException(
                $"Expected child column of {nameof(BitPackingColumnReader<T>)} to be of type {nameof(IColumnReader<T>)} but found {reader.GetType().FullName}");
        ColumnReader = columnReader;
        PrefixLength = prefixLength;
        Prefix = prefix << (BitSize - prefixLength);
        Length = logicalLength;
        Type = type;
        _valueSize = BitSize - prefixLength;
        _valueMask = (T.AllBitsSet << prefixLength) >>> prefixLength;
    }
    
    public T Peek(int byteOffset = 0)
    {
        int index = (Index + byteOffset) * _valueSize;
        int valueIndex = index / BitSize - ColumnReader.Index;
        int shiftAmount = BitSize - _valueSize - index % BitSize;
        T value = ColumnReader.Peek(valueIndex);
        if (shiftAmount >= 0)
        {
            value >>>= shiftAmount; // 1000 >> BitSize - valueSize
        }
        else
        {
            T nextValue = ColumnReader.Peek(valueIndex + 1);
            shiftAmount = int.Abs(shiftAmount);
            nextValue >>>= BitSize - shiftAmount; // 1000 = 0010
            value <<= shiftAmount;
            value |= nextValue;
        }
        value = (value & _valueMask) | Prefix;
        return value;
    }

    IEnumerable<object> IColumnReader.Peek(int offset, int count)
    {
        return Peek(offset, count).OfType<object>();
    }

    object IColumnReader.Peek(int offset)
    {
        return Peek(offset);
    }

    public IEnumerable<T> Peek(int offset, int count)
    {
        // TODO: Implement faster SIMD version of peekn here.
        for (int i = 0; i < count; i++)
        {
            yield return Peek(i + offset);
        }
    }

    public void Advance(int units)
    {
        Index += units;
        int index = Index * _valueSize / BitSize - ColumnReader.Index;
        if (index > 0)
        {
            ColumnReader.Advance(index);
        }
    }
}