using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Master.Serializing.Encodings;
using Master.Serializing.Readers;

namespace Master.Serializing.Columns;

/// <summary>
/// DataColumn is the atomic columns written in a table in the file. All other columns consist of DataColumns and their metadata.
/// </summary>
public struct DataColumn : IColumn
{
    public EncodingId Id => EncodingId.Binary;
    public LogicalType LogicalType { get; }
    public ReadOnlyMemory<byte> Data { get; }
    public int PhysicalSize => Data.Length;
    public int LogicalLength { get; }

    public static DataColumn Empty { get; } = new (LogicalType.UInt8, ReadOnlyMemory<byte>.Empty, 0);

    public DataColumn(LogicalType logicalType, ReadOnlyMemory<byte> data, int logicalLength)
    {
        Data = data;
        LogicalType = logicalType;
        LogicalLength = logicalLength;
    }
    
    private static DataColumn Create<T>(ReadOnlySpan<T> data, LogicalType type) where T : struct
    {   
        if (!BitConverter.IsLittleEndian)
        {
            throw new NotImplementedException();
        }
        
        ReadOnlySpan<byte> reinterpretedData = MemoryMarshal.Cast<T, byte>(data);
        return new DataColumn(type, new ReadOnlyMemory<byte>(reinterpretedData.ToArray()), data.Length);
    }

    public static DataColumn Create<T>(ReadOnlySpan<T> data) where T : struct
    {
        return Create(data, typeof(T).ToLogicalType());
    }

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

    public static DataColumn Create(IEnumerable<byte[]> data)
    {
        return Create(data.Select(d => new ReadOnlyMemory<byte>(d)).ToArray());
    }
    
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

    internal static DataColumn SplitNulls<T>(T?[] array, out DataColumn? nulls)
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

    public IColumnReader<T> OpenReader<T>()
    {
        if (typeof(T) != LogicalType.ToCsType())
        {
            throw new ArgumentException($"Type {typeof(T).FullName} is not valid for logical type {LogicalType}, expected {LogicalType.ToCsType().FullName}", nameof(T));
        }
        
        return
            typeof(T) == typeof(sbyte) ? (new PrimitiveReader<sbyte>(Data) as IColumnReader<T>)! :
            typeof(T) == typeof(short) ? (new PrimitiveReader<short>(Data)  as IColumnReader<T>)! :
            typeof(T) == typeof(int) ? (new PrimitiveReader<int>(Data)  as IColumnReader<T>)! :
            typeof(T) == typeof(long) ? (new PrimitiveReader<long>(Data)  as IColumnReader<T>)! :
            typeof(T) == typeof(byte) ? (new PrimitiveReader<byte>(Data)  as IColumnReader<T>)! :
            typeof(T) == typeof(ushort) ? (new PrimitiveReader<ushort>(Data)  as IColumnReader<T>)! :
            typeof(T) == typeof(uint) ? (new PrimitiveReader<uint>(Data)  as IColumnReader<T>)! :
            typeof(T) == typeof(ulong) ? (new PrimitiveReader<ulong>(Data)  as IColumnReader<T>)! :
            typeof(T) == typeof(Half) ? (new PrimitiveReader<Half>(Data)  as IColumnReader<T>)! :
            typeof(T) == typeof(float) ? (new PrimitiveReader<float>(Data)  as IColumnReader<T>)! :
            typeof(T) == typeof(double) ? (new PrimitiveReader<double>(Data)  as IColumnReader<T>)! :
            typeof(T) == typeof(string) ? (new VarLengthReader(Data, LogicalLength)  as IColumnReader<T>)! :
            typeof(T) == typeof(byte[]) ? (new VarLengthReader(Data, LogicalLength)  as IColumnReader<T>)! :
            throw new ArgumentOutOfRangeException(nameof(T), typeof(T), null);
    }

    internal GenericReader OpenGenericReader()
    {
        return new GenericReader(this);
    }

    public int CalculateTotalLength()
    {
        return LogicalLength;
    }

    public IEnumerable<DataColumn> GetDataColumns()
    {
        yield return this;
    }

    void IColumn.WriteMetadata(ref DataColumnBuilder builder)
    {
        throw new NotImplementedException();
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is DataColumn other &&
               other.Data.Equals(Data) &&
               other.Id == Id &&
               other.PhysicalSize == PhysicalSize &&
               other.LogicalLength == LogicalLength &&
               other.LogicalType == LogicalType;
    }
}