using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TapResult.Columns;

namespace TapResult;

/// <summary>
/// TODO
/// </summary>
public struct DataColumnBuilder
{
    private readonly LogicalType _type;
    private Memory<byte> _data = Memory<byte>.Empty;
    private int _index = 0;
    private int _logicalLength = 0;
    private readonly bool _isConstSize = false;
    
    /// <summary>
    /// TODO
    /// </summary>
    public DataColumnBuilder(int size, bool isConstSize = true) : this(LogicalType.UInt8, size, isConstSize)
    {
    }

    /// <summary>
    /// TODO
    /// </summary>
    public DataColumnBuilder(LogicalType type, int size, bool isConstSize = true)
    {
        _type = type;
        _data = new byte[size];
        _isConstSize = isConstSize;
    }

    /// <summary>
    /// TODO
    /// </summary>
    public int PhysicalSize => _index;
    /// <summary>
    /// TODO
    /// </summary>
    public bool IsAtEnd => _index >= _data.Length; // TODO: Consider if we should include '&& !_isConstSize' here

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private Span<byte> Slice(int size)
    {
        while ((uint)_index + size > (uint)_data.Length)
        {
            if (_isConstSize)
            {
                throw new IndexOutOfRangeException();
            }

            Memory<byte> oldData = _data;
            _data = new byte[oldData.Length * 2];
            oldData.CopyTo(_data);
        }

        Span<byte> slice = _data.Span.Slice(_index, size);
        _index += size;
        return slice;
    }

    /// <summary>
    /// TODO
    /// </summary>
    public void Write<T>(T value)
        where T : unmanaged
    {
        WriteRaw(value);
        
        if (!_type.TryGetSize(out int size))
            size = Unsafe.SizeOf<T>();
        _logicalLength += Unsafe.SizeOf<T>() / size;
    }

    /// <summary>
    /// TODO
    /// </summary>
    public void Write<T>(ReadOnlySpan<T> values)
        where T : unmanaged
    {
        WriteRaw(values, values.Length);
    }

    /// <summary>
    /// TODO
    /// </summary>
    public void WriteBlob(ReadOnlySpan<byte> blob)
    {
        Write(blob.Length);
        WriteRaw(blob, 0);
    }

    /// <summary>
    /// TODO
    /// </summary>
    public void WriteBlobs(IEnumerable<ReadOnlyMemory<byte>> blobs)
    {
        foreach (ReadOnlyMemory<byte> blob in blobs)
        {
            WriteBlob(blob.Span);
        }
    }

    /// <summary>
    /// TODO
    /// </summary>
    public void WriteBlobs(IEnumerable<byte[]> blobs)
    {
        foreach (ReadOnlyMemory<byte> blob in blobs)
        {
            WriteBlob(blob.Span);
        }
    }
    
    /// <summary>
    /// TODO
    /// </summary>
    public void WriteString(string str)
    {
        WriteBlob(Encoding.UTF8.GetBytes(str));
    }
    
    /// <summary>
    /// TODO
    /// </summary>
    public void WriteStrings(IEnumerable<string> strs)
    {
        foreach (string str in strs)
        {
            WriteString(str);
        }
    }

    /// <summary>
    /// TODO
    /// </summary>
    public void WriteRaw<T>(ReadOnlySpan<T> values, int logicalLength)
        where T : unmanaged
    {
        _logicalLength += logicalLength;
        
        if (BitConverter.IsLittleEndian)
        {
            Span<byte> slice = Slice(values.Length * Unsafe.SizeOf<T>());
            ReadOnlySpan<byte> bytes = MemoryMarshal.Cast<T, byte>(values);
            bytes.CopyTo(slice);
            return;
        }

        for (int i = 0; i < values.Length; i++)
        {
            WriteRaw(values[i]);
        }
    }
    
    /// <summary>
    /// TODO
    /// </summary>
    public void WriteRaw<T>(T value)
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

    /// <summary>
    /// TODO
    /// </summary>
    public DataColumn Build()
    {
        return new DataColumn(_type,  _data.Slice(0, _index), _logicalLength);
    }
}