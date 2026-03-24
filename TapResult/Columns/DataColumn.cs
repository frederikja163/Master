using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TapResult.Encodings;
using TapResult.Readers;

namespace TapResult.Columns;

/// <summary>
/// DataColumn is the atomic columns written in a table in the file.
/// All other columns consist of DataColumns and their metadata.
/// </summary>
public sealed class DataColumn : IColumn
{
    private long _offset;
    public EncodingType EncodingType => EncodingType.Binary;
    /// <summary>
    /// The underlying data of the DataColumn.
    /// </summary>
    public ReadOnlyMemory<byte> Data { get; }
    /// <summary>
    /// The logical type of the data stored in <see cref="Data"/>
    /// </summary>
    public LogicalType LogicalType { get; }


    /// <summary>
    /// The physical length, or the length of the <see cref="Data"/> memory.
    /// </summary>
    public int PhysicalSize => Data.Length;
    /// <summary>
    /// The logical length. This varies depending on <see cref="LogicalType"/>.
    /// </summary>
    public int LogicalLength { get; }
    private static readonly int BlobSize = Unsafe.SizeOf<int>() + Unsafe.SizeOf<int>() + Unsafe.SizeOf<long>();

    /// <summary>
    /// Gets an empty DataColumn without any data, and with the logical type of uint.
    /// </summary>
    public static DataColumn Empty { get; } = new (LogicalType.UInt8, ReadOnlyMemory<byte>.Empty, 0);

    /// <summary>
    /// Creates a new DataColumn, there are easier ways to create a datacolumn using the helper method DataColumn.Create.
    /// </summary>
    public DataColumn(LogicalType logicalType, ReadOnlyMemory<byte> data, int logicalLength)
    {
        Data = data;
        LogicalType = logicalType;
        LogicalLength = logicalLength;
    }
    
    private static DataColumn Create<T>(ReadOnlySpan<T> data, LogicalType type) where T : unmanaged
    {   
        if (!BitConverter.IsLittleEndian)
        {
            DataColumnBuilder builder = new DataColumnBuilder(type, data.Length * Unsafe.SizeOf<T>());
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

        DataColumnBuilder builder = new DataColumnBuilder(LogicalType.String, length + sizeof(int) * data.Count);
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
        
        DataColumnBuilder valueBuilder = new DataColumnBuilder(typeof(T).ToLogicalType(), valueSize);
        DataColumnBuilder nullBuilder = new DataColumnBuilder(array.Length / 8 + 1);
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

    /// <summary>
    /// Open a typed reader that reads the values of this DataColumn.
    /// Will give an error if the type of T is not the same as <see cref="LogicalType"/>.
    /// </summary>
    public IColumnReader<T> OpenReader<T>()
    {
        if (typeof(T) != LogicalType.ToCsType() || OpenReader() is not IColumnReader<T> reader)
        {
            throw new ArgumentException($"Type {typeof(T).FullName} is not valid for logical type {LogicalType}, expected {LogicalType.ToCsType().FullName}", nameof(T));
        }

        return reader;
    }

    /// <summary>
    /// Open a reader that reads the values of this DataColumn.
    /// The Reader will have the type specified by <see cref="LogicalType"/>.
    /// </summary>
    public IColumnReader OpenReader()
    {
        return LogicalType switch
        {
            LogicalType.SInt8 => new PrimitiveReader<sbyte>(Data),
            LogicalType.SInt16 => new PrimitiveReader<short>(Data),
            LogicalType.SInt32 => new PrimitiveReader<int>(Data),
            LogicalType.SInt64 => new PrimitiveReader<long>(Data),
            LogicalType.UInt8 => new PrimitiveReader<byte>(Data),
            LogicalType.UInt16 => new PrimitiveReader<ushort>(Data),
            LogicalType.UInt32 => new PrimitiveReader<uint>(Data),
            LogicalType.UInt64 => new PrimitiveReader<ulong>(Data),
            LogicalType.Float16 => new PrimitiveReader<Half>(Data),
            LogicalType.Float32 => new PrimitiveReader<float>(Data),
            LogicalType.Float64 => new PrimitiveReader<double>(Data),
            LogicalType.Blob => new VarLengthReader(Data, LogicalLength, LogicalType),
            LogicalType.String => new VarLengthReader(Data, LogicalLength, LogicalType),
            _ => throw new ArgumentOutOfRangeException(nameof(LogicalType), typeof(LogicalType), null)
        };
    }

    /// <summary>
    /// Opens a new generic reader on this DataColumn.
    /// </summary>
    public GenericReader OpenGenericReader()
    {
        return new GenericReader(Data.Span);
    }

    void IColumn.WriteMetadata(ref DataColumnBuilder blobBuilder)
    {
        blobBuilder.Write(BlobSize);
        blobBuilder.WriteRaw(PhysicalSize);
        blobBuilder.WriteRaw(LogicalLength);
        blobBuilder.WriteRaw(_offset);
    }

    internal void Write(Stream stream)
    {
        _offset = stream.Position;
        stream.Write(Data.Span);
    }
}