using System.Runtime.CompilerServices;
using TapResult.Encodings;
using TapResult.Readers;

namespace TapResult.Columns;

internal sealed class NullColumn : IColumnParent
{
    private IColumn _nullColumn;
    private IColumn _valueColumn;
    
    public NullColumn(LogicalType logicalType, IColumn nullColumn, IColumn valueColumn, int logicalLength)
    {
        LogicalType = logicalType;
        _nullColumn = nullColumn;
        _valueColumn = valueColumn;
        LogicalLength = logicalLength;
    }
    public int LogicalLength { get; }
    public EncodingType EncodingType { get; } = EncodingType.Null;
    public LogicalType LogicalType { get; }
    public void WriteMetadata(IBlobBuilder blobBuilder)
    {
        blobBuilder.WriteValue(LogicalLength);
    }

    public IColumnReader OpenReader()
    {
        IColumnReader<byte> nullReader = _nullColumn.OpenReader<byte>();
        IColumnReader valueReader = _valueColumn.OpenReader();
        return ColumnReader(LogicalType, LogicalLength, nullReader, valueReader);
    }

    internal static IColumnReader ColumnReader(LogicalType type, int length, IColumnReader<byte> nullReader, IColumnReader valueReader)
    {
        return type switch
        {
            LogicalType.SInt8 => new NullReaderValType<sbyte>(nullReader, valueReader, length, type),
            LogicalType.SInt16 => new NullReaderValType<short>(nullReader, valueReader, length, type),
            LogicalType.SInt32 => new NullReaderValType<int>(nullReader, valueReader, length, type),
            LogicalType.SInt64 => new NullReaderValType<long>(nullReader, valueReader, length, type),
            LogicalType.UInt8 => new NullReaderValType<byte>(nullReader, valueReader, length, type),
            LogicalType.UInt16 => new NullReaderValType<ushort>(nullReader, valueReader, length, type),
            LogicalType.UInt32 => new NullReaderValType<uint>(nullReader, valueReader, length, type),
            LogicalType.UInt64 => new NullReaderValType<ulong>(nullReader, valueReader, length, type),
            LogicalType.Float16 => new NullReaderValType<Half>(nullReader, valueReader, length, type),
            LogicalType.Float32 => new NullReaderValType<float>(nullReader, valueReader, length, type),
            LogicalType.Float64 => new NullReaderValType<double>(nullReader, valueReader, length, type),
            LogicalType.Blob => new NullReaderRefType<byte[]>(nullReader, valueReader, length, type),
            LogicalType.String => new NullReaderRefType<string>(nullReader, valueReader, length, type),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public IEnumerable<IColumn> GetChildColumns()
    {
        yield return _nullColumn;
        yield return _valueColumn;
    }

    public bool Swap(IColumn existingColumn, IColumn newColumn)
    {
        if (existingColumn.Equals(_nullColumn))
        {
            _nullColumn = newColumn;
            return true;
        }
        else if (existingColumn.Equals(_valueColumn))
        {
            _valueColumn = newColumn;
            return true;
        }

        return false;
    }
}