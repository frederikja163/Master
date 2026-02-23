using System.Numerics;
using System.Runtime.CompilerServices;
using Master.Serializing.Columns;

namespace Master.Serializing.Readers;

internal sealed class BitPackingColumnReader<T> : IColumnReader<T>
    where T : unmanaged, INumber<T>, IBinaryInteger<T>, IMinMaxValue<T>
{
    public byte PrefixLength { get; }
    public ulong Prefix { get; }
    public LogicalType Type { get; }
    public IColumnReader<T> ColumnReader { get; }
    public int Length { get; }
    public int Index { get; } = 0;
    private readonly int _valueBitSize;

    public BitPackingColumnReader(byte prefixLength, ulong prefix, int logicalLength, LogicalType type, IColumnReader<T> reader)
    {
        PrefixLength = prefixLength;
        Prefix = prefix;
        Length = logicalLength;
        Type = type;
        ColumnReader = reader;
    }
    
    public T Peek(int offset = 0)
    {
    }

    public IEnumerable<T> Peek(int count, int offset)
    {
        throw new NotImplementedException();
    }

    public void Advance(int units)
    {
        throw new NotImplementedException();
    }
    
    
    public DataColumn Decode(IColumn data)
    {
        if (data is not BitPackingColumn bitPackingColumn)
            throw new Exception($"Data({nameof(data)}) is not a BitPackingColumn");
        DataColumn dataColumn = (DataColumn) bitPackingColumn.Column; // TODO: needs to not be casted here
        if (!bitPackingColumn.Type.TryGetSize(out int size))
        {
            throw new Exception("Type must be a primitive");
        }
        
        DataColumn column = size switch
        {
            1 => Decode<byte>(dataColumn, bitPackingColumn),
            2 => Decode<ushort>(dataColumn, bitPackingColumn),
            4 => Decode<uint>(dataColumn, bitPackingColumn),
            8 => Decode<ulong>(dataColumn, bitPackingColumn),
            _ => throw new Exception("Logical type size must be either 1, 2, 4 or 8."),
        };
        return column;
    }

    private DataColumn Decode<T>(DataColumn dataColumn, BitPackingColumn metadata)
        where T : unmanaged, INumber<T>, IBinaryInteger<T>, IMinMaxValue<T>
    {
        int size = Unsafe.SizeOf<T>() * 8;
        int packedSize = size - metadata.PrefixLength;
        int length = metadata.LogicalLength;
        DataColumnReader reader = dataColumn.OpenReader();
        DataColumnBuilder builder =
            new DataColumnBuilder(metadata.Type, length * size / 8);

        T flag = (T.MaxValue << metadata.PrefixLength) >> metadata.PrefixLength;
        ulong p = metadata.Prefix << (size - metadata.PrefixLength);
        T prefix = Unsafe.As<ulong, T>(ref p);
        T currentValue = reader.Read<T>();
        int shift = size - packedSize;
        for (int i = 0; i < length; i++)
        {
            T value;
            if (shift >= 0)
            {
                value = (currentValue >> shift);
            }
            else
            {
                int shift1 = int.Abs(shift);
                value = currentValue << shift1;
                currentValue = reader.Read<T>();
                shift = size - shift1;
                value |= (currentValue >> shift);
            }

            value &= flag;
            value |= prefix;
            builder.Write(value);
            shift -= packedSize;
        }
        
        return builder.Build();
    }
}