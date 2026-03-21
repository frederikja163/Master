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
public sealed class DataColumn : IColumn, IEquatable<DataColumn>
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
        return new GenericReader(Data);
    }

    void IColumn.WriteMetadata(ColumnBuilder blobBuilder)
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

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is DataColumn other &&
               Equals(other);
    }

    public bool Equals(DataColumn? other)
    {
        return other is not null &&
               other.Data.Equals(Data) &&
               other.EncodingType == EncodingType &&
               other.PhysicalSize == PhysicalSize &&
               other.LogicalLength == LogicalLength &&
               other.LogicalType == LogicalType;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Data, EncodingType, PhysicalSize, (int)LogicalType, LogicalLength, LogicalType);
    }
}