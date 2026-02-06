using System.Runtime.InteropServices;
using System.Text;

namespace Master.Serializing;

public readonly struct DataColumn
{
    public ReadOnlyMemory<byte> Data { get; }
    public readonly LogicalType LogicalType;
    public int PhysicalSize => Data.Length;
    public int LogicalLength { get; }

    public static DataColumn Empty { get; } = new DataColumn(LogicalType.UInt8, ReadOnlyMemory<byte>.Empty, 0);

    public DataColumn(LogicalType logicalType, ReadOnlyMemory<byte> data, int logicalLength)
    {
        Data = data;
        LogicalType = logicalType;
        LogicalLength = logicalLength;
    }

    public static DataColumn Create<T>(ReadOnlySpan<T> data) where T : struct
    {
        if (!BitConverter.IsLittleEndian)
        {
            throw new NotImplementedException();
        }
        
        ReadOnlySpan<byte> reinterpretedData = MemoryMarshal.Cast<T, byte>(data);
        return new DataColumn(typeof(T).ToLogicalType(), new ReadOnlyMemory<byte>(reinterpretedData.ToArray()), data.Length);
    }

    public static DataColumn Create(ReadOnlySpan<string> data)
    {
        int length = 0;
        foreach (string str in data)
        {
            length += Encoding.UTF8.GetByteCount(str);
        }

        byte[] bytes = new byte[length + sizeof(int) * data.Length];
        int index = 0;
        foreach (string str in data)
        {
            BitConverter.TryWriteBytes(bytes.AsSpan().Slice(index, sizeof(int)), str.Length); // TODO: Using str.length here is wrong
            index += 4;
            index += Encoding.UTF8.GetBytes(str, bytes.AsSpan().Slice(index));
        }

        return new DataColumn(LogicalType.String, bytes, data.Length);
    }

    public static DataColumn Create(ReadOnlySpan<ReadOnlyMemory<byte>> data)
    {
        int length = 0;
        foreach (ReadOnlyMemory<byte> blob in data)
        {
            length += blob.Length;
        }

        byte[] bytes = new byte[length + sizeof(int) * data.Length];
        int index = 0;
        foreach (ReadOnlyMemory<byte> blob in data)
        {
            BitConverter.TryWriteBytes(bytes.AsSpan(index, sizeof(int)), blob.Length);
            index += 4;
            blob.Span.CopyTo(bytes.AsSpan(index));
            index += blob.Length;
        }

        return new DataColumn(LogicalType.Blob, bytes, data.Length);
    }

    public static DataColumn Create(Array array)
    {
        return array switch
        {
            sbyte[] values => Create<sbyte>(values),
            short[] values => Create<short>(values),
            int[] values => Create<int>(values),
            long[] values => Create<long>(values),
            byte[] values => Create<byte>(values),
            ushort[] values => Create<ushort>(values),
            uint[] values => Create<uint>(values),
            ulong[] values => Create<ulong>(values),
            Half[] values => Create<Half>(values),
            float[] values => Create<float>(values),
            double[] values => Create<double>(values),
            string[] str => Create(str.AsSpan()),
            // TODO: Handle nullable arrays.
            // sbyte?[] values => Create<sbyte>(values),
            // short?[] values => Create<short>(values),
            // int?[] values => Create<int>(values),
            // long?[] values => Create<long>(values),
            // byte?[] values => Create<byte>(values),
            // ushort?[] values => Create<ushort>(values),
            // uint?[] values => Create<uint>(values),
            // ulong?[] values => Create<ulong>(values),
            // Half?[] values => Create<Half>(values),
            // float?[] values => Create<float>(values),
            // double?[] values => Create<double>(values),
            _ => throw new ArgumentOutOfRangeException(nameof(array))
        };
    }

    public DataColumnReader OpenReader()
    {
        return new DataColumnReader(this);
    }
}