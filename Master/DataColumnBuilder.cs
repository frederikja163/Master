using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Master.Serializing;

namespace Master;

internal ref struct DataColumnBuilder
{
    private readonly LogicalType _type;
    private readonly byte[] _data = [];
    private int _index = 0;
    private readonly int _logicalLength = 0;

    public DataColumnBuilder(LogicalType type, int size, int logicalLength)
    {
        _type = type;
        _data = new byte[size];
        _logicalLength = logicalLength;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private Span<byte> Slice(int size)
    {
        if ((uint)_index + size >= (uint)_data.Length)
            throw new IndexOutOfRangeException();

        Span<byte> slice = _data.AsSpan(_index, size);
        _index += size;
        return slice;
    }

    public void Write<T>(T value)
        where T : unmanaged
    {
        Span<byte> slice = Slice(Unsafe.SizeOf<T>());
        switch (value)
        {
            case sbyte sInt8: slice[0] = (byte)sInt8; break;
            case short sInt16: BinaryPrimitives.WriteInt16LittleEndian(slice, sInt16); break;
            case int sInt32: BinaryPrimitives.WriteInt32LittleEndian(slice, sInt32); break;
            case long sInt64: BinaryPrimitives.WriteInt64LittleEndian(slice, sInt64); break;
            case byte uInt8: slice[0] = uInt8; break;
            case ushort uInt16: BinaryPrimitives.WriteUInt16LittleEndian(slice, uInt16); break;
            case uint uInt32: BinaryPrimitives.WriteUInt32LittleEndian(slice, uInt32); break;
            case ulong uInt64: BinaryPrimitives.WriteUInt64LittleEndian(slice, uInt64); break;
            case Half float16: BinaryPrimitives.WriteHalfLittleEndian(slice, float16); break;
            case float float32: BinaryPrimitives.WriteSingleLittleEndian(slice, float32); break;
            case double float64: BinaryPrimitives.WriteDoubleLittleEndian(slice, float64); break;
            default: throw new ArgumentOutOfRangeException(nameof(T), typeof(T), null);
        }
    }

    public void Write<T>(ReadOnlySpan<T> values)
        where T : unmanaged
    {
        if (BitConverter.IsLittleEndian)
        {
            Span<byte> slice = Slice(values.Length * Unsafe.SizeOf<T>());
            ReadOnlySpan<byte> bytes = MemoryMarshal.Cast<T, byte>(values);
            bytes.CopyTo(slice);
            return;
        }

        for (int i = 0; i < values.Length; i++)
        {
            Write(values[i]);
        }
    }

    public void WriteBlob(ReadOnlySpan<byte> blob)
    {
        Write(blob.Length);
        Write(blob);
    }

    public void WriteBlobs(ReadOnlySpan<ReadOnlyMemory<byte>> blobs)
    {
        for (int i = 0; i < blobs.Length; i++)
        {
            WriteBlob(blobs[i].Span);
        }
    }
    
    public void WriteString(string str)
    {
        WriteBlob(Encoding.UTF8.GetBytes(str));
    }
    
    public void WriteStrings(ReadOnlySpan<string> strs)
    {
        foreach (string str in strs)
        {
            WriteString(str);
        }
    }

    public DataColumn Build()
    {
        return new DataColumn(_type, _data, _logicalLength);
    }
}