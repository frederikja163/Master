using System.Numerics;
using System.Runtime.CompilerServices;
using TapResult.Columns;
using TapResult.Readers;

namespace TapResult.Encodings;

/// <summary>
/// Bitpack encoding, packs together integer types by removing common prefixes and storing said prefix as metadata.
/// For example turning 1a 1b 1c 1d into ab cd with 1 as the prefix.
/// </summary>
public sealed class BitPacking : IEncoding
{
    public EncodingType Type { get; } = EncodingType.BitPacking;
    
    public IColumn Encode<T>(IColumnReader<T> dataColumn) where T : notnull
    {
        return dataColumn switch
        {
            IColumnReader<sbyte> reader => EncodeData(reader),
            IColumnReader<short> reader => EncodeData(reader),
            IColumnReader<int> reader => EncodeData(reader),
            IColumnReader<long> reader => EncodeData(reader),
            IColumnReader<byte> reader => EncodeData(reader),
            IColumnReader<ushort> reader => EncodeData(reader),
            IColumnReader<uint> reader => EncodeData(reader),
            IColumnReader<ulong> reader => EncodeData(reader),
            _ => throw new ArgumentOutOfRangeException(nameof(dataColumn)),
        };
    }

    public IColumnReader CreateDecoder(LogicalType type, int length, GenericReader metadataReader, IEnumerable<IColumnReader> childReader)
    {
        IColumnReader? reader = childReader.FirstOrDefault();
        if (reader is null)
            throw new Exception("Expected a child column to a bitpack encoded column, but found none.");
        byte prefixLength = metadataReader.Read<byte>();
        ulong prefix = metadataReader.Read<ulong>();
        return OpenReader(reader, length, type, prefixLength, prefix);
    }

    private static IColumn EncodeData<T>(IColumnReader<T> reader)
        where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
    {
        GetMetadata<T>(reader.Clone(), out byte prefixLength, out ulong prefix);
        int size = Unsafe.SizeOf<T>() * 8;
        int packedSize = size - prefixLength;
        int length = (int)double.Ceiling(reader.Length / (double)packedSize) + 1;
        ColumnBuilder<T> builder = new (length * Unsafe.SizeOf<T>());
        T flag = (T.AllBitsSet << prefixLength) >>> prefixLength;
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
                builder.WriteValue(currentValue);

                currentValue = value;
                shift = packedSize - shift;
            }
        }

        currentValue <<= size - shift;
        builder.WriteValue(currentValue);
        return new BitPackingColumn(builder.Build(), prefixLength, prefix, reader.Length);
    }

    internal static void GetMetadata<T>(IColumnReader<T> data, out byte prefixLength, out ulong prefix) where T : unmanaged, IBinaryInteger<T>
    {
        Span<int> bitCounts = stackalloc int[Unsafe.SizeOf<ulong>() * 8];
        GetBitCounts<T>(data, bitCounts);

        prefixLength = 0;
        prefix = 0;
        
        for (int i = 0; i < Unsafe.SizeOf<T>() * 8 - 1; i++)
        {
            int bitCount = bitCounts[i];
            if (bitCount == data.Length)
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
    }

    internal static void GetBitCounts<T>(IColumnReader<T> reader, Span<int> bitCounts)
        where T : unmanaged, INumber<T>, IBinaryInteger<T>
    {
        // TODO: Optimize using SIMD and PopCount.
        int size = Unsafe.SizeOf<T>() * 8;
        for (int i = 0; i < reader.Length; i++)
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

    internal static IColumnReader OpenReader(IColumnReader reader, int logicalLength, LogicalType type, byte prefixLength,
        ulong prefix) => type switch
    {
        LogicalType.SInt8 => new BitPackingColumnReader<sbyte>(reader, logicalLength, type, prefixLength, (sbyte)prefix),
        LogicalType.SInt16 => new BitPackingColumnReader<short>(reader, logicalLength, type, prefixLength, (short)prefix),
        LogicalType.SInt32 => new BitPackingColumnReader<int>(reader, logicalLength, type, prefixLength, (int)prefix),
        LogicalType.SInt64 => new BitPackingColumnReader<long>(reader, logicalLength, type, prefixLength, (long)prefix),
        LogicalType.UInt8 => new BitPackingColumnReader<byte>(reader, logicalLength, type, prefixLength, (byte)prefix),
        LogicalType.UInt16 => new BitPackingColumnReader<ushort>(reader, logicalLength, type, prefixLength, (ushort)prefix),
        LogicalType.UInt32 => new BitPackingColumnReader<uint>(reader, logicalLength, type, prefixLength, (uint)prefix),
        LogicalType.UInt64 => new BitPackingColumnReader<ulong>(reader, logicalLength, type, prefixLength, (ulong)prefix),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}