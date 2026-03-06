using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Master.Serializing.Columns;
using Master.Serializing.Readers;

namespace Master.Serializing.Encodings;

internal sealed class BitPacking : IEncoding
{
    public EncodingId Id { get; } = EncodingId.BitPacking;
    
    public IColumn Encode(in DataColumn dataColumn)
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

    public IColumnReader CreateDecoder(LogicalType type, ref GenericReader metadataReader, IEnumerable<IColumnReader> childReader)
    {
        IColumnReader? reader = childReader.FirstOrDefault();
        if (reader is null)
            throw new Exception("Expected a child column to a bitpack encoded column, but found none.");
        if (metadataReader.Read<int>() != BitPackingColumn.Size)
            throw new Exception("BitPacking metadata was malformed.");
        byte prefixLength = metadataReader.Read<byte>();
        ulong prefix = metadataReader.Read<ulong>();
        int logicalLength = metadataReader.Read<int>();
        return type switch
        {
            LogicalType.SInt8 => new BitPackingColumnReader<sbyte>(reader, logicalLength, type, prefixLength, (sbyte)prefix),
            LogicalType.SInt16 => new BitPackingColumnReader<short>(reader, logicalLength, type, prefixLength, (short)prefix),
            LogicalType.SInt32 => new BitPackingColumnReader<int>(reader, logicalLength, type, prefixLength, (int)prefix),
            LogicalType.SInt64 => new BitPackingColumnReader<long>(reader, logicalLength, type, prefixLength, (long)prefix),
            LogicalType.UInt8 => new BitPackingColumnReader<byte>(reader, logicalLength, type, prefixLength, (byte)prefix),
            LogicalType.UInt16 => new BitPackingColumnReader<ushort>(reader, logicalLength, type, prefixLength, (ushort)prefix),
            LogicalType.UInt32 => new BitPackingColumnReader<uint>(reader, logicalLength, type, prefixLength, (uint)prefix),
            LogicalType.UInt64 => new BitPackingColumnReader<ulong>(reader, logicalLength, type, prefixLength, (ulong)prefix),
            
            // Explicitly throw argument out of range exception so we can get warnings if LogicalType adds new types.
            LogicalType.Float16 => throw new ArgumentOutOfRangeException(nameof(type), type, null),
            LogicalType.Float32 => throw new ArgumentOutOfRangeException(nameof(type), type, null),
            LogicalType.Float64 => throw new ArgumentOutOfRangeException(nameof(type), type, null),
            LogicalType.Blob => throw new ArgumentOutOfRangeException(nameof(type), type, null),
            LogicalType.String => throw new ArgumentOutOfRangeException(nameof(type), type, null),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    private static IColumn Encode<T>(in DataColumn dataColumn)
        where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
    {
        BitPackingColumn metadata = GetMetadata<T>(dataColumn);
        EncodeData<T>(dataColumn, metadata);
        return metadata;
    }

    private static void EncodeData<T>(in DataColumn dataColumn, BitPackingColumn metadata)
        where T : unmanaged, INumber<T>, IBinaryInteger<T>, IMinMaxValue<T>
    {
        IColumnReader<T> reader = new PrimitiveReader<T>(dataColumn.Data);
        int size = Unsafe.SizeOf<T>() * 8;
        int packedSize = size - metadata.PrefixLength;
        int length = (int)double.Ceiling(dataColumn.PhysicalSize * (packedSize / (double)size)) + 1;
        DataColumnBuilder builder = new DataColumnBuilder(dataColumn.LogicalType, length * Unsafe.SizeOf<T>());
        T flag = (~T.Zero << metadata.PrefixLength) >>> metadata.PrefixLength;
        T currentValue = default;
        int shift = 0;
        while (!reader.IsAtEnd)
        {
             T value = reader.Read() & flag;
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

    internal static BitPackingColumn GetMetadata<T>(in DataColumn data) where T : unmanaged, IBinaryInteger<T>
    {
        Span<int> bitCounts = stackalloc int[Unsafe.SizeOf<ulong>() * 8];
        GetBitCounts<T>(data, bitCounts);
        int count = data.LogicalLength;
        
        byte prefixLength = 0;
        ulong prefix = 0;

        for (int i = 0; i < Unsafe.SizeOf<T>() * 8 - 1; i++)
        {
            int bitCount = bitCounts[i];
            if (bitCount == count)
            {
                prefixLength += 1;
                prefix = (prefix << 1) | 1;
            }
            else if (bitCount == 0)
            {
                prefixLength += 1;
                prefix <<= 1;
            }
            else
            {
                break;
            }
        }
        return new BitPackingColumn(data, prefixLength, prefix);
    }

    internal static void GetBitCounts<T>(in DataColumn column, Span<int> bitCounts)
        where T : unmanaged, INumber<T>, IBinaryInteger<T>
    {
        // TODO: Optimize using SIMD and PopCount.
        IColumnReader<T> reader = new PrimitiveReader<T>(column.Data);
        int size = Unsafe.SizeOf<T>() * 8;
        for (int i = 0; i < column.LogicalLength; i++)
        {
            T value = reader.Read();
            // TODO: Skip leading zeros.
            for (int j = 1; j <= size; j++)
            {
                int bit = (value << (size - j)) >> (size - 1) != default ? 1 : 0;
                bitCounts[size - j] += bit;
            }
        }
    }

    public IEnumerable<LogicalType> GetSupportedTypes()
    {
        return TypeHelper.IntegerTypes();
    }
}