using System.Runtime.InteropServices;
using System.Text;

namespace Master.Serializing;

internal readonly struct PhysicalColumn
{
    public ReadOnlyMemory<byte> Data { get; }
    public readonly LogicalType LogicalType;
    public int PhysicalSize => Data.Length;
    public int LogicalLength { get; }

    public PhysicalColumn(LogicalType logicalType, ReadOnlyMemory<byte> data, int logicalLength)
    {
        Data = data;
        LogicalType = logicalType;
        LogicalLength = logicalLength;
    }

    public static PhysicalColumn Create<T>(ReadOnlySpan<T> data) where T : struct
    {
        ReadOnlySpan<byte> reinterpretedData = MemoryMarshal.Cast<T, byte>(data);
        return new PhysicalColumn(typeof(T).ToLogicalType(), new ReadOnlyMemory<byte>(reinterpretedData.ToArray()), data.Length);
    }

    public static PhysicalColumn Create(ReadOnlySpan<string> data)
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
            BitConverter.TryWriteBytes(bytes.AsSpan().Slice(index, sizeof(int)), str.Length);
            index += 4;
            index += Encoding.UTF8.GetBytes(str, bytes.AsSpan().Slice(index));
        }

        return new PhysicalColumn(LogicalType.String, bytes, data.Length);
    }

    public static PhysicalColumn Create(ReadOnlySpan<ReadOnlyMemory<byte>> data)
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

        return new PhysicalColumn(LogicalType.Blob, bytes, data.Length);
    }

    public ReadOnlySpan<T> Interpret<T>()
        where T : struct
    {
        return MemoryMarshal.Cast<byte, T>(Data.Span);
    }
}