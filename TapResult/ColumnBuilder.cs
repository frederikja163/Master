using System.Buffers.Binary;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TapResult.Columns;

namespace TapResult;

/// <summary>
/// Helper type for making <see cref="DataColumn"/>.
/// </summary>
public sealed class ColumnBuilder
{
    private readonly LogicalType _type;
    private Memory<byte> _data;
    private int _byteIndex = 0;
    private int _logicalLength = 0;
    
    /// <summary>
    /// Create a new DataColumnBuilder with type <see cref="LogicalType.UInt8"/>.
    /// Size is specified in bytes.
    /// </summary>
    public ColumnBuilder(int size) : this(LogicalType.UInt8, size)
    {
    }

    /// <summary>
    /// Create a new DataColumnBuilder.
    /// Size is specified in bytes.
    /// </summary>
    public ColumnBuilder(LogicalType type, int size)
    {
        _type = type;
        _data = new byte[size];
    }

    /// <summary>
    /// The physical size so far of this DataColumnBuilder.
    /// Returns the size currently written, not the total capacity.
    /// </summary>
    public int PhysicalSize => _byteIndex;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private Span<byte> Slice(int size)
    {
        while ((uint)_byteIndex + size > (uint)_data.Length)
        {
            Memory<byte> oldData = _data;
            _data = new byte[oldData.Length * 2];
            oldData.CopyTo(_data);
        }

        Span<byte> slice = _data.Span.Slice(_byteIndex, size);
        _byteIndex += size;
        return slice;
    }

    /// <summary>
    /// Write a value of type T to the DataColumn. This increases the length by 1 as opposed to <see cref="WriteRaw{T}(T)"/>
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
    /// Write a value of type T to the DataColumn. This increases the length by values.Length as opposed to <see cref="WriteRaw{T}(System.ReadOnlySpan{T},int)"/>
    /// </summary>
    public void Write<T>(ReadOnlySpan<T> values)
        where T : unmanaged
    {
        WriteRaw(values, values.Length);
    }

    /// <summary>
    /// Writes a single blob to the DataColumn.
    /// </summary>
    public void WriteBlob(ReadOnlySpan<byte> blob)
    {
        Write(blob.Length);
        WriteRaw(blob, 0);
    }

    /// <summary>
    /// Writes blobs to the DataColumn.
    /// </summary>
    public void WriteBlobs(IEnumerable<ReadOnlyMemory<byte>> blobs)
    {
        foreach (ReadOnlyMemory<byte> blob in blobs)
        {
            WriteBlob(blob.Span);
        }
    }

    /// <summary>
    /// Writes multiple blobs to the DataColumn.
    /// </summary>
    public void WriteBlobs(IEnumerable<byte[]> blobs)
    {
        foreach (ReadOnlyMemory<byte> blob in blobs)
        {
            WriteBlob(blob.Span);
        }
    }
    
    /// <summary>
    /// Writes a string to the DataColumn.
    /// </summary>
    public void WriteString(string str)
    {
        WriteBlob(Encoding.UTF8.GetBytes(str));
    }
    
    /// <summary>
    /// Writes multiple strings to the DataColumn.
    /// </summary>
    public void WriteStrings(IEnumerable<string> strs)
    {
        foreach (string str in strs)
        {
            WriteString(str);
        }
    }

    /// <summary>
    /// Writes multiple values to the DataColumn, this only increases LogicalLength by the provided value.
    /// Generally use <see cref="Write{T}(ReadOnlySpan{T})"/> unless you have a good reason to override the added length.
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
    /// Writes a single value to the DataColumn, this does not increase the logical length.
    /// Generally use <see cref="Write{T}(T)"/> unless you have a good reason to not increase the logical length.
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
    /// Builds this DataColumnBuilder into a DataColumn and returns it.
    /// </summary>
    public DataColumn Build()
    {
        return new DataColumn(_type,  _data.Slice(0, _byteIndex), _logicalLength);
    }
    
    
    
    private static DataColumn Create<T>(ReadOnlySpan<T> data, LogicalType type) where T : unmanaged
    {   
        if (!BitConverter.IsLittleEndian)
        {
            ColumnBuilder builder = new ColumnBuilder(type, data.Length * Unsafe.SizeOf<T>());
            foreach (T var in data)
            {
                builder.Write(var);
            }
            return builder.Build();
        }
        
        ReadOnlySpan<byte> reinterpretedData = MemoryMarshal.Cast<T, byte>(data);
        return new DataColumn(type, new ReadOnlyMemory<byte>(reinterpretedData.ToArray()), data.Length);
    }

    /// <summary>
    /// Create a new DataColumn from a span of data.
    /// </summary>
    public static DataColumn Create<T>(ReadOnlySpan<T> data) where T : unmanaged
    {
        return Create(data, typeof(T).ToLogicalType());
    }

    /// <summary>
    /// Create a new DataColumn from any collection of strings.
    /// </summary>
    public static DataColumn Create(ICollection<string> data)
    {
        int length = 0;
        foreach (string str in data)
        {
            length += Encoding.UTF8.GetByteCount(str);
        }

        ColumnBuilder builder = new ColumnBuilder(LogicalType.String, length + sizeof(int) * data.Count);
        builder.WriteStrings(data);

        return builder.Build();
    }

    /// <summary>
    /// Create a new DataColumn from an IEnumerable of blobs.
    /// </summary>
    public static DataColumn Create(IEnumerable<byte[]> data)
    {
        return Create(data.Select(d => new ReadOnlyMemory<byte>(d)).ToArray());
    }
    
    /// <summary>
    /// Create a new DataColumn from a collection of blobs.
    /// </summary>
    public static DataColumn Create(ICollection<ReadOnlyMemory<byte>> data)
    {
        int length = 0;
        foreach (ReadOnlyMemory<byte> blob in data)
        {
            length += blob.Length;
        }

        byte[] bytes = new byte[length + sizeof(int) * data.Count];
        int index = 0;
        foreach (ReadOnlyMemory<byte> blob in data)
        {
            BitConverter.TryWriteBytes(bytes.AsSpan(index, sizeof(int)), blob.Length);
            index += 4;
            blob.Span.CopyTo(bytes.AsSpan(index));
            index += blob.Length;
        }

        return new DataColumn(LogicalType.Blob, bytes, data.Count);
    }

    /// <summary>
    /// Create a new DataColumn based on an array.
    /// The array can either contain primitive types from <see cref="LogicalType"/>, or strings.
    /// A separate nulls DataColumn is created if the underlying type is nullable.
    /// </summary>
    public static DataColumn Create(Array array, out DataColumn? nulls)
    {
        nulls = null;
        return array switch
        {
            sbyte[] values => Create<sbyte>(values, array.GetType().GetElementType()! == typeof(sbyte) ? LogicalType.SInt8 : LogicalType.UInt8),
            short[] values => Create<short>(values, array.GetType().GetElementType()! == typeof(short) ? LogicalType.SInt16 : LogicalType.UInt16),
            int[] values => Create<int>(values, array.GetType().GetElementType()! == typeof(int) ? LogicalType.SInt32 : LogicalType.UInt32),
            long[] values => Create<long>(values, array.GetType().GetElementType()! == typeof(long) ? LogicalType.SInt64 : LogicalType.UInt64),
            Half[] values => Create<Half>(values),
            float[] values => Create<float>(values),
            double[] values => Create<double>(values),
            string[] str => Create(str), // TODO: Split nulls for strings.
            sbyte?[] values => SplitNulls<sbyte>(values, out nulls),
            short?[] values => SplitNulls<short>(values, out nulls),
            int?[] values => SplitNulls<int>(values, out nulls),
            long?[] values => SplitNulls<long>(values, out nulls),
            byte?[] values => SplitNulls<byte>(values, out nulls),
            ushort?[] values => SplitNulls<ushort>(values, out nulls),
            uint?[] values => SplitNulls<uint>(values, out nulls),
            ulong?[] values => SplitNulls<ulong>(values, out nulls),
            Half?[] values => SplitNulls<Half>(values, out nulls),
            float?[] values => SplitNulls<float>(values, out nulls),
            double?[] values => SplitNulls<double>(values, out nulls),
            _ => throw new ArgumentOutOfRangeException(nameof(array))
        };
    }

    private static DataColumn SplitNulls<T>(T?[] array, out DataColumn? nulls)
        where T : unmanaged
    {
        int valueSize = 0;
        foreach (var value in array)
        {
            if (value is null)
            {
                continue;
            }

            valueSize += Unsafe.SizeOf<T>();
        }
        
        ColumnBuilder valueBuilder = new ColumnBuilder(typeof(T).ToLogicalType(), valueSize);
        ColumnBuilder nullBuilder = new ColumnBuilder(array.Length / 8 + 1);
        byte nullByte = 0;
        // TODO: Benchmark using a bitarray and loop unrolling here versus the current implementation.
        for (int i = 0; i < array.Length; i++)
        {
            T? value = array[i];
            if (value is { } val)
            {
                nullByte = (byte)((nullByte << 1) | 0);
                valueBuilder.Write(val);
            }
            else
            {
                nullByte = (byte)((nullByte << 1) | 1);
            }

            if (i % 8 == 0)
            {
                nullBuilder.Write(nullByte);
                nullByte = 0;
            }
        }
        nullBuilder.Write(nullByte);
        
        nulls = nullBuilder.Build();
        return valueBuilder.Build();
    }
}