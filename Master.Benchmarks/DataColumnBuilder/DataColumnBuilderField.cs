using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Master.Benchmarks;
using Master.Serializing;

namespace Master.Benchmarks;

internal interface IMemorySlicer
{
    Memory<byte> Data { get; }
    Span<byte> Slice(int size);

    public int Index { get; }
}

internal struct ResizeableMemorySlicer : IMemorySlicer
{
    public Memory<byte> Data { get; private set; }
    public int Index { get; private set; }

    public ResizeableMemorySlicer(Memory<byte> data)
    {
        Data = data;
    }

    public Span<byte> Slice(int size)
    {
        while ((uint)Index + size > (uint)Data.Length)
        {
            Memory<byte> oldData = Data;
            Data = new byte[oldData.Length * 2];
            oldData.CopyTo(Data);
        }

        Span<byte> slice = Data.Span.Slice(Index, size);
        Index += size;
        return slice;
    }
}

internal struct FixedMemorySlicer : IMemorySlicer
{
    public Memory<byte> Data { get; }
    public int Index { get; private set; }

    public FixedMemorySlicer(Memory<byte> data)
    {
        Data = data;
    }

    public Span<byte> Slice(int size)
    {
        if ((uint)Index + size > (uint)Data.Length)
        {
            throw new IndexOutOfRangeException();
        }

        Span<byte> slice = Data.Span.Slice(Index, size);
        Index += size;
        return slice;
    }
}

public struct DataColumnBuilderField
{
    private readonly LogicalType _type;
    private int _logicalLength = 0;
    private bool _resizeAble = false;
    private IMemorySlicer slicer;
    
    public DataColumnBuilderField(int size, bool resizeAble = false) : this(LogicalType.UInt8, size, resizeAble)
    {
    }

    public DataColumnBuilderField(LogicalType type, int size, bool resizeAble = false)
    {
        _type = type;
        _resizeAble = resizeAble;
        slicer = resizeAble ? new ResizeableMemorySlicer(new byte[size]) : new FixedMemorySlicer(new byte[size]);
    }

    public int PhysicalSize => slicer.Index;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private Span<byte> Slice(int size)
    {
        return slicer.Slice(size);
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
        return new DataColumn(_type,  slicer.Data.Slice(0, slicer.Index), _logicalLength);
    }
}