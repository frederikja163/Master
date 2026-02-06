using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Master.Serializing.Encodings;

internal sealed class BitPacking : IEncoding
{
    internal struct Metadata
    {
        public static readonly int Size = Unsafe.SizeOf<byte>() +
                                          Unsafe.SizeOf<ulong>() +
                                          Unsafe.SizeOf<int>() +
                                          Unsafe.SizeOf<byte>();
        public byte PrefixLength { get; set; }
        public ulong Prefix { get; set; }
        public int LogicalLength { get; set; }
        public LogicalType Type { get; set; }

        public Metadata(DataColumn column)
        {
            Debug.Assert(column.PhysicalSize == Size);
            DataColumnReader reader = column.OpenReader();
            PrefixLength = reader.Read<byte>();
            Prefix = reader.Read<ulong>();
            LogicalLength = reader.Read<int>();
            Type = (LogicalType)reader.Read<byte>();
        }
        
        public DataColumn ToDataColumn()
        {
            DataColumnBuilder builder = new DataColumnBuilder(Size);
            builder.Write(PrefixLength);
            builder.Write(Prefix);
            builder.Write(LogicalLength);
            builder.Write((byte)Type);
            return builder.Build();
        }
    }
    
    public EncodingId Id { get; } = EncodingId.BitPacking;
    
    public void Encode(DataColumn dataColumn, ref DataColumn metadataCol, out DataColumn[] outColumns)
    {
        if (!dataColumn.LogicalType.TryGetSize(out int size))
        {
            throw new Exception("Type must be a primitive.");
        }
        
        DataColumn column = size switch
        {
            1 => Encode<byte>(dataColumn, ref metadataCol),
            2 => Encode<ushort>(dataColumn, ref metadataCol),
            4 => Encode<uint>(dataColumn, ref metadataCol),
            8 => Encode<ulong>(dataColumn, ref metadataCol),
            _ => throw new Exception("Logical type size must be either 1, 2, 4 or 8."),
        };

        outColumns = [column];
    }

    private static DataColumn Encode<T>(DataColumn dataColumn, ref DataColumn metadataCol)
        where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
    {
        Metadata metadata = GetMetadata<T>(dataColumn, ref metadataCol);
        return EncodeData<T>(dataColumn, metadata);
    }

    private static DataColumn EncodeData<T>(DataColumn dataColumn, Metadata metadata)
        where T : unmanaged, INumber<T>, IBinaryInteger<T>, IMinMaxValue<T>
    {
        DataColumnReader reader = dataColumn.OpenReader();
        int size = Unsafe.SizeOf<T>() * 8;
        int packedSize = size - metadata.PrefixLength;
        int length = (int)float.Ceiling(reader.PhysicalSize * packedSize / (float)size);
        DataColumnBuilder builder = new DataColumnBuilder(LogicalType.SInt32, length * size / 8);

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

        return builder.Build();
    }

    internal static Metadata GetMetadata<T>(DataColumn data, ref DataColumn metadataCol) where T : unmanaged, IBinaryInteger<T>
    {
        if (metadataCol.LogicalLength != 0)
        {
            return new Metadata(metadataCol);
        }

        Span<int> bitCounts = stackalloc int[Unsafe.SizeOf<ulong>() * 8];
        GetBitCounts<T>(data, bitCounts);
        int count = data.LogicalLength;
        Metadata metadata = new Metadata();
        metadata.Type = data.LogicalType;
        metadata.LogicalLength = data.LogicalLength;
        
        foreach (int bitCount in bitCounts)
        {
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
        metadataCol = metadata.ToDataColumn();
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

    public DataColumn Decode(DataColumn[] data, DataColumn metadataCol)
    {
        if (data.Length != 1)
            throw new Exception($"Length of {nameof(data)} must be equal to 1");
        DataColumn dataColumn = data[0];
        Metadata metadata = new Metadata(metadataCol);
        if (!metadata.Type.TryGetSize(out int size))
        {
            throw new Exception("Type must be a primitive");
        }
        
        DataColumn column = size switch
        {
            1 => Decode<byte>(dataColumn, metadata),
            2 => Decode<ushort>(dataColumn, metadata),
            4 => Decode<uint>(dataColumn, metadata),
            8 => Decode<ulong>(dataColumn, metadata),
            _ => throw new Exception("Logical type size must be either 1, 2, 4 or 8."),
        };
        return column;
    }

    private DataColumn Decode<T>(DataColumn dataColumn, Metadata metadata)
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