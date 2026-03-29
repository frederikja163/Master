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
    private byte[]? _nulls = null;
    private byte[] _data;
    private int _byteIndex = 0;
    private int _valuesLength = 0;
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
        if ((uint)_byteIndex + size > (uint)_data.Length)
        {
            Array.Resize(ref _data,  Math.Max(_data.Length * 2, _byteIndex + size));
        }

        Span<byte> slice = _data.AsSpan(_byteIndex, size);
        _byteIndex += size;
        return slice;
    }

    /// <summary>
    /// Writes a null value to the datacolumn.
    /// </summary>
    public void WriteNull()
    {
        if (_nulls is null)
        {
            // Guess how many nulls we based on length.
            int capacity;
            if (_type.TryGetSize(out int size))
            {
                capacity = _data.Length / size * 2;
            }
            else
            {
                capacity = _data.Length / 8;
            }
            capacity = Math.Max(capacity, _logicalLength / 4);
            
            _nulls = new byte[capacity];
        }

        int byteIndex = _logicalLength / 8;
        if ((uint)byteIndex > (uint)_nulls.Length)
        {
            Array.Resize(ref _nulls, Math.Max(_nulls.Length * 2, byteIndex));
        }
        int bitIndex = _logicalLength % 8;
        byte value = _nulls[byteIndex];
        value |= (byte)(1 << bitIndex);
        _nulls[byteIndex] = value;
        _logicalLength += 1;
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
        _valuesLength += Unsafe.SizeOf<T>() / size;
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
        _valuesLength += logicalLength;
        
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
    /// Builds this ColumnBuilder into an IColumn, will automatically determine if the column should be nullable or not.
    /// </summary>
    /// <returns></returns>
    public IColumn Build()
    {
        if (_valuesLength == _logicalLength)
        {
            return BuildDataColumn();
        }
        return new NullColumn(_type, new DataColumn(LogicalType.UInt8, _nulls, _logicalLength / 8 + 1),
            new DataColumn(_type, new Memory<byte>(_data, 0, _byteIndex), _valuesLength),
            _logicalLength);
    }

    /// <summary>
    /// Builds this DataColumnBuilder into a DataColumn and returns it.
    /// </summary>
    /// <remarks>If this ColumnBuilder has any nulls written into it, those values will disappear. Consider using <see cref="Build"/> instead.</remarks>
    public DataColumn BuildDataColumn()
    {
        return new DataColumn(_type,  new Memory<byte>(_data, 0, _byteIndex), _logicalLength);
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
            return builder.BuildDataColumn();
        }
        
        ReadOnlySpan<byte> reinterpretedData = MemoryMarshal.Cast<T, byte>(data);
        return new DataColumn(type, new ReadOnlyMemory<byte>(reinterpretedData.ToArray()), data.Length);
    }

    // TODO: Switch all create methods to return an IColumn instead of DataColumn.
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
    private static DataColumn CreateFromString(ICollection<string> data)
    {
        int length = 0;
        foreach (string str in data)
        {
            length += Encoding.UTF8.GetByteCount(str);
        }

        ColumnBuilder builder = new ColumnBuilder(LogicalType.String, length + sizeof(int) * data.Count);
        builder.WriteStrings(data);

        return builder.BuildDataColumn();
    }

    /// <summary>
    /// Create a new DataColumn from an IEnumerable of blobs.
    /// </summary>
    public static DataColumn Create(IEnumerable<byte[]> data)
    {
        return CreateFromBlobs(data.Select(d => new ReadOnlyMemory<byte>(d)).ToArray());
    }
    
    /// <summary>
    /// Create a new DataColumn from a collection of blobs.
    /// </summary>
    private static DataColumn CreateFromBlobs(ICollection<ReadOnlyMemory<byte>> data)
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
    public static IColumn Create(Array array)
    {
        return array switch
        {
            sbyte[] values => Create<sbyte>(values, array.GetType().GetElementType()! == typeof(sbyte) ? LogicalType.SInt8 : LogicalType.UInt8),
            short[] values => Create<short>(values, array.GetType().GetElementType()! == typeof(short) ? LogicalType.SInt16 : LogicalType.UInt16),
            int[] values => Create<int>(values, array.GetType().GetElementType()! == typeof(int) ? LogicalType.SInt32 : LogicalType.UInt32),
            long[] values => Create<long>(values, array.GetType().GetElementType()! == typeof(long) ? LogicalType.SInt64 : LogicalType.UInt64),
            Half[] values => Create<Half>(values),
            float[] values => Create<float>(values),
            double[] values => Create<double>(values),
            string[] str => CreateFromString(str), // TODO: Split nulls for strings.
            sbyte?[] values => SplitNulls<sbyte>(values),
            short?[] values => SplitNulls<short>(values),
            int?[] values => SplitNulls<int>(values),
            long?[] values => SplitNulls<long>(values),
            byte?[] values => SplitNulls<byte>(values),
            ushort?[] values => SplitNulls<ushort>(values),
            uint?[] values => SplitNulls<uint>(values),
            ulong?[] values => SplitNulls<ulong>(values),
            Half?[] values => SplitNulls<Half>(values),
            float?[] values => SplitNulls<float>(values),
            double?[] values => SplitNulls<double>(values),
            _ => throw new ArgumentOutOfRangeException(nameof(array))
        };
    }

    private static IColumn SplitNulls<T>(T?[] array)
        where T : unmanaged
    {
        LogicalType type = typeof(T).ToLogicalType();
        type.TryGetSize(out int size);
        ColumnBuilder valueBuilder = new ColumnBuilder(type, size * array.Length);
        for (int i = 0; i < array.Length; i++)
        {
            T? value = array[i];
            if (value is { } val)
            {
                valueBuilder.Write(val);
            }
            else
            {
                valueBuilder.WriteNull();
            }
        }
        
        return valueBuilder.Build();
    }
}