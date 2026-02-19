using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Master.Serializing;

namespace Master.Benchmarks;

internal abstract class DataColumnBuilderBase
{
    protected readonly LogicalType _type;
    protected Memory<byte> _data = Memory<byte>.Empty;
    protected int _index = 0;
    protected int _logicalLength = 0;
    
    public DataColumnBuilderBase(int size) : this(LogicalType.UInt8, size)
    {
    }

    public DataColumnBuilderBase(LogicalType type, int size)
    {
        _type = type;
        _data = new byte[size];
    }

    public int PhysicalSize => _data.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    protected abstract Span<byte> Slice(int size);

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

        if (!_type.TryGetSize(out int size))
            size = Unsafe.SizeOf<T>();
        _logicalLength += Unsafe.SizeOf<T>() / size;
    }

    public void Write<T>(ReadOnlySpan<T> values)
        where T : unmanaged
    {
        if (BitConverter.IsLittleEndian)
        {
            Span<byte> slice = Slice(values.Length * Unsafe.SizeOf<T>());
            ReadOnlySpan<byte> bytes = MemoryMarshal.Cast<T, byte>(values);
            bytes.CopyTo(slice);
            _logicalLength += values.Length;
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
        WriteRaw(blob, 0);
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

    public void WriteRaw(ReadOnlySpan<byte> values, int logicalLength)
    {
        Write(values);
        // We add values.length in Write(values), but for writeRaw we want to override this with the provided length.
        _logicalLength -= values.Length;
        _logicalLength += logicalLength;
    }

    public DataColumn Build()
    {
        return new DataColumn(_type,  _data.Slice(0, _index), _logicalLength);
    }
}