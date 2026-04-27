using System.Buffers.Binary;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using TapResult.Columns;

namespace TapResult;

internal interface IRawWriter
{
    public void WriteRaw<T>(ReadOnlySpan<T> bytes, int count = 0) where T : unmanaged;
    public void WriteRaw<T>(T bytes);
    public void CloseBlob();
}


/// <summary>
/// Helper type for making <see cref="DataColumn"/>.
/// </summary>
public sealed class ColumnBuilder<T> : IRawWriter
{
    private BlobBuilder? _blobBuilder = null;
    private byte[]? _nulls = null;
    private int[]? _lengths = null;
    private byte[] _data;
    private int _byteIndex = 0;
    private int _valuesLength = 0;
    private int _logicalLength = 0;
    
    /// <summary>
    /// Create a new DataColumnBuilder.
    /// Size is specified in bytes.
    /// </summary>
    public ColumnBuilder(int size)
    {
        _data = new byte[size];
    }

    /// <summary>
    /// The physical size so far of this DataColumnBuilder.
    /// Returns the size currently written, not the total capacity.
    /// </summary>
    public int PhysicalSize => _byteIndex;

    /// <summary>
    /// The current logical length of this <see cref="ColumnBuilder"/>.
    /// </summary>
    public int LogicalLength => _logicalLength;

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
            int capacity = _data.Length / 8;
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

    private void WriteLength(int length)
    {
        if (_lengths is null)
        {
            _lengths = new int[16];
        }

        int index = _valuesLength;
        if (index >= _lengths.Length)
        {
            Array.Resize(ref _lengths, Math.Max(index + 1, _lengths.Length * 2));
        }
        _lengths[index] = length;
    }

    /// <summary>
    /// Write a value of type T to the DataColumn. This increases the length by 1 as opposed to <see cref="WriteRaw{T}(T)"/>
    /// </summary>
    public void WriteValue(T value)
    {
        WriteRaw(value);
        
        _logicalLength += 1;
        _valuesLength += 1;
    }

    /// <summary>
    /// Write values of type T to the DataColumn.
    /// </summary>
    [OverloadResolutionPriority(1)]
    public void WriteValues(ReadOnlySpan<T> values)
    {
        switch (typeof(T).ToLogicalType())
        {
            case LogicalType.SInt8:  WriteRaw(Unsafe.BitCast<ReadOnlySpan<T>, ReadOnlySpan<sbyte>>(values)); break;
            case LogicalType.SInt16:  WriteRaw(Unsafe.BitCast<ReadOnlySpan<T>, ReadOnlySpan<short>>(values)); break;
            case LogicalType.SInt32:  WriteRaw(Unsafe.BitCast<ReadOnlySpan<T>, ReadOnlySpan<int>>(values)); break;
            case LogicalType.SInt64:  WriteRaw(Unsafe.BitCast<ReadOnlySpan<T>, ReadOnlySpan<long>>(values)); break;
            case LogicalType.UInt8:  WriteRaw(Unsafe.BitCast<ReadOnlySpan<T>, ReadOnlySpan<byte>>(values)); break;
            case LogicalType.UInt16:  WriteRaw(Unsafe.BitCast<ReadOnlySpan<T>, ReadOnlySpan<ushort>>(values)); break;
            case LogicalType.UInt32:  WriteRaw(Unsafe.BitCast<ReadOnlySpan<T>, ReadOnlySpan<uint>>(values)); break;
            case LogicalType.UInt64:  WriteRaw(Unsafe.BitCast<ReadOnlySpan<T>, ReadOnlySpan<ulong>>(values)); break;
            case LogicalType.Float16:  WriteRaw(Unsafe.BitCast<ReadOnlySpan<T>, ReadOnlySpan<Half>>(values)); break;
            case LogicalType.Float32:  WriteRaw(Unsafe.BitCast<ReadOnlySpan<T>, ReadOnlySpan<float>>(values)); break;
            case LogicalType.Float64:  WriteRaw(Unsafe.BitCast<ReadOnlySpan<T>, ReadOnlySpan<double>>(values)); break;
            default:
                foreach (T value in values)
                {
                    WriteValue(value);
                }

                return;
        }

        _logicalLength += values.Length;
        _valuesLength += values.Length;
    }

    /// <summary>
    /// Write values of type T to the DataColumn.
    /// </summary>
    public void WriteValues(IEnumerable<T> values)
    {
        foreach (T value in values)
        {
            WriteValue(value);
        }
    }

    void IRawWriter.WriteRaw<TValue>(ReadOnlySpan<TValue> values, int logicalLength) =>
        WriteRaw(values, logicalLength);
    
    /// <summary>
    /// Writes multiple values to the DataColumn, this only increases LogicalLength by the provided value.
    /// Generally use <see cref="WriteValue"/> unless you have a good reason to override the added length.
    /// </summary>
    private void WriteRaw<TValue>(ReadOnlySpan<TValue> values, int logicalLength = 0)
        where TValue : unmanaged
    {
        _logicalLength += logicalLength;
        _valuesLength += logicalLength;
        
        if (BitConverter.IsLittleEndian)
        {
            Span<byte> slice = Slice(values.Length * Unsafe.SizeOf<TValue>());
            ReadOnlySpan<byte> bytes = MemoryMarshal.Cast<TValue, byte>(values);
            bytes.CopyTo(slice);
            return;
        }

        for (int i = 0; i < values.Length; i++)
        {
            WriteRaw(values[i]);
        }
    }

    void IRawWriter.WriteRaw<TValue>(TValue value) => WriteRaw(value);
    
    private void WriteRaw<TValue>(TValue value)
    {
        switch (value)
        {
            case sbyte sInt8: Slice(Unsafe.SizeOf<TValue>())[0] = (byte)sInt8; break;
            case short sInt16: BinaryPrimitives.WriteInt16LittleEndian(Slice(Unsafe.SizeOf<TValue>()), sInt16); break;
            case int sInt32: BinaryPrimitives.WriteInt32LittleEndian(Slice(Unsafe.SizeOf<TValue>()), sInt32); break;
            case long sInt64: BinaryPrimitives.WriteInt64LittleEndian(Slice(Unsafe.SizeOf<TValue>()), sInt64); break;
            case byte uInt8: Slice(Unsafe.SizeOf<TValue>())[0] = uInt8; break;
            case ushort uInt16: BinaryPrimitives.WriteUInt16LittleEndian(Slice(Unsafe.SizeOf<TValue>()), uInt16); break;
            case uint uInt32: BinaryPrimitives.WriteUInt32LittleEndian(Slice(Unsafe.SizeOf<TValue>()), uInt32); break;
            case ulong uInt64: BinaryPrimitives.WriteUInt64LittleEndian(Slice(Unsafe.SizeOf<TValue>()), uInt64); break;
            case Half float16: BinaryPrimitives.WriteHalfLittleEndian(Slice(Unsafe.SizeOf<TValue>()), float16); break;
            case float float32: BinaryPrimitives.WriteSingleLittleEndian(Slice(Unsafe.SizeOf<TValue>()), float32); break;
            case double float64: BinaryPrimitives.WriteDoubleLittleEndian(Slice(Unsafe.SizeOf<TValue>()), float64); break;
            case string str: WriteRaw(Encoding.UTF8.GetBytes(str)); break;
            case byte[] blob:
                WriteLength(blob.Length);
                WriteRaw((ReadOnlySpan<byte>)blob);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(TValue), typeof(TValue), null);
        }
    }

    /// <summary>
    /// Builds this ColumnBuilder into an IColumn.
    /// </summary>
    /// <returns></returns>
    public IColumn Build()
    {
        return Build(typeof(T).ToLogicalType());
    }

    /// <summary>
    /// Builds this <see cref="ColumnBuilder"/>, but overrides the <see cref="LogicalType"/> to a compatible type.
    /// </summary>
    public IColumn Build(LogicalType overrideType)
    {
        if (!overrideType.IsCompatible(typeof(T).ToLogicalType()))
        {
            throw new Exception($"{overrideType} is not compatible with {typeof(T).ToLogicalType()}");
        }

        IColumn column;
        if (_lengths is not null)
        {
            IColumn length = ColumnBuilder.Create<int>(_lengths.AsSpan(0, _logicalLength));
            IColumn data = ColumnBuilder.Create<byte>(_data.AsSpan(0, _byteIndex));
            column = new SplitColumn(length, data, overrideType);
        }
        else
        {
            column = new DataColumn(overrideType, new Memory<byte>(_data, 0, _byteIndex), _valuesLength);
        }

        if (_valuesLength == _logicalLength)
        {
            return column;
        }
        return new NullColumn(overrideType, ColumnBuilder.Create<byte>(_nulls.AsSpan(0, _logicalLength / 8 + 1)), column, _logicalLength);
    }
    
    internal DataColumn BuildDataColumn()
    {
        return new DataColumn(typeof(T).ToLogicalType(), new Memory<byte>(_data, 0, _byteIndex), _logicalLength);
    }

    /// <summary>
    /// Opens a new <see cref="BlobBuilder"/> on this <see cref="ColumnBuilder"/>.
    /// </summary>
    public BlobBuilder OpenBlob()
    {
        if (_blobBuilder is not null)
        {
            throw new Exception($"Cannot open more than one blob on a {nameof(ColumnBuilder)} at a time.");
        }
        
        _blobBuilder = new BlobBuilder(this)
        {
            StartIndex = _byteIndex
        };
        // Make sure there is space for an integer later.
        return _blobBuilder;
    }

    /// <summary>
    /// Closes the currently open <see cref="BlobBuilder"/>, if one exists, on this <see cref="ColumnBuilder"/>.
    /// </summary>
    void IRawWriter.CloseBlob()
    {
        if (_blobBuilder is null)
        {
            throw new Exception(
                $"This {nameof(ColumnBuilder)} does not have an open {nameof(BlobBuilder)}");
        }

        int length = _byteIndex - _blobBuilder.StartIndex;
        WriteLength(length);
        _blobBuilder = null;
        _logicalLength += 1;
        _valuesLength += 1;
    }
}

public static class ColumnBuilder
{
    private static IColumn Create<T>(ReadOnlySpan<T> data, LogicalType type) where T : unmanaged
    {
        if (!BitConverter.IsLittleEndian)
        {
            ColumnBuilder<T> builder = new ColumnBuilder<T>(data.Length * Unsafe.SizeOf<T>());
            foreach (T var in data)
            {
                builder.WriteValue(var);
            }
            return builder.Build();
        }
        
        ReadOnlySpan<byte> reinterpretedData = MemoryMarshal.Cast<T, byte>(data);
        return new DataColumn(typeof(T).ToLogicalType(), new ReadOnlyMemory<byte>(reinterpretedData.ToArray()), data.Length);
    }
    
    /// <summary>
    /// Create a new DataColumn from a span of data.
    /// </summary>
    [OverloadResolutionPriority(1)]
    public static IColumn Create<T>(ReadOnlySpan<T> data) where T : unmanaged
    {
        return Create(data, typeof(T).ToLogicalType());
    }

    public static IColumn Create<T>(IEnumerable<T> values)
    {
        if (!values.TryGetNonEnumeratedCount(out int count))
        {
            count = 16;
        }

        int size = count * Unsafe.SizeOf<T>();
        ColumnBuilder<T> builder = new ColumnBuilder<T>(size);
        foreach (T value in values)
        {
            if (value is null)
            {
                builder.WriteNull();
            }
            else
            {
                builder.WriteValue(value);
            }
        }
        return builder.Build();
    }

    /// <summary>
    /// Create a new DataColumn based on an array.
    /// The array can either contain primitive types from <see cref="LogicalType"/>, or strings.
    /// A separate nulls DataColumn is created if the underlying type is nullable.
    /// </summary>
    [OverloadResolutionPriority(1)]
    public static IColumn Create(Array array)
    {
        return array switch
        {
            sbyte[] values => array.GetType().GetElementType()! == typeof(sbyte) ? Create<sbyte>(values) : Create<byte>(Unsafe.As<byte[]>(values)),
            short[] values => array.GetType().GetElementType()! == typeof(short) ? Create<short>(values) : Create<ushort>(Unsafe.As<ushort[]>(values)),
            int[] values => array.GetType().GetElementType()! == typeof(int) ? Create<int>(values) : Create<uint>(Unsafe.As<uint[]>(values)),
            long[] values => array.GetType().GetElementType()! == typeof(long) ? Create<long>(values) : Create<ulong>(Unsafe.As<ulong[]>(values)),
            Half[] values => Create<Half>(values.AsSpan()),
            float[] values => Create<float>(values.AsSpan()),
            double[] values => Create<double>(values.AsSpan()),
            sbyte?[] values => SplitNulls(values),
            short?[] values => SplitNulls(values),
            int?[] values => SplitNulls(values),
            long?[] values => SplitNulls(values),
            byte?[] values => SplitNulls(values),
            ushort?[] values => SplitNulls(values),
            uint?[] values => SplitNulls(values),
            ulong?[] values => SplitNulls(values),
            Half?[] values => SplitNulls(values),
            float?[] values => SplitNulls(values),
            double?[] values => SplitNulls(values),
            string[] str => Create<string>((IEnumerable<string>)str),
            byte[][] blobs => Create<byte[]>((IEnumerable<byte[]>)blobs),
            _ => throw new ArgumentOutOfRangeException(nameof(array))
        };
    }

    private static IColumn SplitNulls<T>(T?[] array)
        where T : unmanaged
    {
        LogicalType type = typeof(T).ToLogicalType();
        type.TryGetSize(out int size);
        ColumnBuilder<T> valueBuilder = new ColumnBuilder<T>(size * array.Length);
        for (int i = 0; i < array.Length; i++)
        {
            T? value = array[i];
            if (value is { } val)
            {
                valueBuilder.WriteValue(val);
            }
            else
            {
                valueBuilder.WriteNull();
            }
        }
        
        return valueBuilder.Build();
    }
}