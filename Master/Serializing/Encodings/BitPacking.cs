using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Master.Serializing.Encodings;

internal sealed class BitPacking : IEncoding
{
    private struct Metadata
    {
        public byte PrefixLength { get; set; }
        public ulong Prefix { get; set; }
        public LogicalType Type { get; set; }

        public Metadata(DataColumn column)
        {
            Debug.Assert(column.PhysicalSize == Unsafe.SizeOf<Metadata>());
            DataColumnReader reader = column.OpenReader();
            PrefixLength = reader.Read<byte>();
            Prefix = reader.Read<ulong>();
            Type = (LogicalType)reader.Read<byte>();
        }
        
        public DataColumn ToDataColumn()
        {
            DataColumnBuilder builder = new DataColumnBuilder(Unsafe.SizeOf<Metadata>());
            builder.Write(PrefixLength);
            builder.Write(Prefix);
            builder.Write((byte)Type);
            return builder.Build();
        }
    }
    
    public EncodingId Id { get; } = EncodingId.BitPacking;
    
    public void Encode(DataColumn dataColumn, ref DataColumn metadataCol, out DataColumn[] outColumns)
    {
        Metadata metadata = GetMetadata(dataColumn, ref metadataCol);
        
        DataColumnReader reader = dataColumn.OpenReader();
        DataColumnBuilder builder = new DataColumnBuilder((int)MathF.Ceiling(reader.PhysicalSize * 8f / metadata.PrefixLength));
        
    }

    private static DataColumn EncodeData<T>(DataColumn dataColumn, Metadata metadata)
        where T : unmanaged, INumber<T>, IBinaryInteger<T>
    {
        
    }

    private static Metadata GetMetadata(DataColumn data, ref DataColumn metadataCol)
    {
        if (metadataCol.LogicalLength != 0)
        {
            return new Metadata(metadataCol);
        }
        Debug.Assert(metadataCol.LogicalType.TryGetSize(out int size));

        Span<int> bitCounts = stackalloc int[Unsafe.SizeOf<ulong>()];
        switch (size)
        {
            case 1:
                GetBitCounts<byte>(data, bitCounts, 1);
                break;
            case 2:
                GetBitCounts<ushort>(data, bitCounts, 1);
                break;
            case 4:
                GetBitCounts<uint>(data, bitCounts, 1);
                break;
            case 8:
                GetBitCounts<ulong>(data, bitCounts, 1);
                break;
        }
        int count = data.LogicalLength;
        Metadata metadata = new Metadata();
        
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

    internal static void GetBitCounts<T>(DataColumn column, Span<int> bitCounts, T one)
        where T : unmanaged, INumber<T>, IBinaryInteger<T>
    {
        // TODO: Optimize using SIMD and PopCount.
        DataColumnReader reader = column.OpenReader();
        int size = Unsafe.SizeOf<T>();
        T maxValue = reader.Read<T>();
        for (int i = 0; i < column.LogicalLength; i++)
        {
            T value = reader.Read<T>();
            // TODO: Skip leading zeros.
            for (int j = 0; j < size * 8; j++)
            {
                bitCounts[j] += (value << (size - j)) >> (size - 1) == one ? 1 : 0;
            }
        }
    }

    public DataColumn Decode(DataColumn[] data, DataColumn metadata)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<LogicalType> GetSupportedTypes()
    {
        return TypeHelper.IntegerTypes();
    }
}