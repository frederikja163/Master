using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Master.Serializing.Columns;

namespace Master.Serializing.Encodings;

internal sealed class BitPacking : IEncoding
{
    public EncodingId Id { get; } = EncodingId.BitPacking;
    
    public IColumn Encode(DataColumn dataColumn)
    {
        if (!dataColumn.LogicalType.TryGetSize(out int size))
        {
            throw new Exception("Type must be a primitive.");
        }
        
        IColumn column = size switch
        {
            1 => Encode<byte>(dataColumn),
            2 => Encode<ushort>(dataColumn),
            4 => Encode<uint>(dataColumn),
            8 => Encode<ulong>(dataColumn),
            _ => throw new Exception("Logical type size must be either 1, 2, 4 or 8."),
        };

        return column;
    }

    private static IColumn Encode<T>(DataColumn dataColumn)
        where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
    {
        BitPackingColumn metadata = GetMetadata<T>(dataColumn);
        EncodeData<T>(dataColumn, metadata);
        return metadata;
    }

    private static void EncodeData<T>(DataColumn dataColumn, BitPackingColumn metadata)
        where T : unmanaged, INumber<T>, IBinaryInteger<T>, IMinMaxValue<T>
    {
        DataColumnReader reader = dataColumn.OpenReader();
        int size = Unsafe.SizeOf<T>() * 8;
        int packedSize = size - metadata.PrefixLength;
        int length = (int)double.Ceiling(reader.PhysicalSize * (packedSize / (double)size)) + 1;
        DataColumnBuilder builder = new DataColumnBuilder(dataColumn.LogicalType, length * Unsafe.SizeOf<T>());
        T flag = (T.MaxValue << metadata.PrefixLength) >> metadata.PrefixLength;
        T currentValue = default;
        int shift = 0;
        while (!reader.AtEnd)
        {
             T value = reader.Read<T>() & flag;
             if (shift + packedSize < size)
             {
                 currentValue = (currentValue << packedSize) | value;
                 shift += packedSize;
             }
             else
             {
                 shift = size - shift;
                 T value1 = value >> (packedSize - shift);
                 currentValue = (currentValue << shift) | value1;
                 builder.Write(currentValue);

                 currentValue = value;
                 shift = packedSize - shift;
             }
        }

        currentValue <<= size - shift;
        builder.Write(currentValue);

        metadata.Column = builder.Build();
    }

    internal static BitPackingColumn GetMetadata<T>(DataColumn data) where T : unmanaged, IBinaryInteger<T>
    {
        Span<int> bitCounts = stackalloc int[Unsafe.SizeOf<ulong>() * 8];
        GetBitCounts<T>(data, bitCounts);
        int count = data.LogicalLength;
        BitPackingColumn metadata = new (data)
        {
            Type = data.LogicalType,
            LogicalLength = data.LogicalLength
        };

        for (int i = 0; i < Unsafe.SizeOf<T>() * 8 - 1; i++)
        {
            int bitCount = bitCounts[i];
            if (bitCount == count)
            {
                metadata.PrefixLength += 1;
                metadata.Prefix = (metadata.Prefix << 1) | 1;
            }
            else if (bitCount == 0)
            {
                metadata.PrefixLength += 1;
                metadata.Prefix <<= 1;
            }
            else
            {
                break;
            }
        }
        return metadata;
    }

    internal static void GetBitCounts<T>(DataColumn column, Span<int> bitCounts)
        where T : unmanaged, INumber<T>, IBinaryInteger<T>
    {
        // TODO: Optimize using SIMD and PopCount.
        DataColumnReader reader = column.OpenReader();
        int size = Unsafe.SizeOf<T>() * 8;
        for (int i = 0; i < column.LogicalLength; i++)
        {
            T value = reader.Read<T>();
            // TODO: Skip leading zeros.
            for (int j = 1; j <= size; j++)
            {
                int bit = (value << (size - j)) >> (size - 1) != default ? 1 : 0;
                bitCounts[size - j] += bit;
            }
        }
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

    public IEnumerable<LogicalType> GetSupportedTypes()
    {
        return TypeHelper.IntegerTypes();
    }
}