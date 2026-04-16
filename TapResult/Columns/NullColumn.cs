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
        return LogicalType switch
        {
            LogicalType.SInt8 => new NullReaderValType<sbyte>(nullReader, valueReader, LogicalLength, LogicalType),
            LogicalType.SInt16 => new NullReaderValType<short>(nullReader, valueReader, LogicalLength, LogicalType),
            LogicalType.SInt32 => new NullReaderValType<int>(nullReader, valueReader, LogicalLength, LogicalType),
            LogicalType.SInt64 => new NullReaderValType<long>(nullReader, valueReader, LogicalLength, LogicalType),
            LogicalType.UInt8 => new NullReaderValType<byte>(nullReader, valueReader, LogicalLength, LogicalType),
            LogicalType.UInt16 => new NullReaderValType<ushort>(nullReader, valueReader, LogicalLength, LogicalType),
            LogicalType.UInt32 => new NullReaderValType<uint>(nullReader, valueReader, LogicalLength, LogicalType),
            LogicalType.UInt64 => new NullReaderValType<ulong>(nullReader, valueReader, LogicalLength, LogicalType),
            LogicalType.Float16 => new NullReaderValType<Half>(nullReader, valueReader, LogicalLength, LogicalType),
            LogicalType.Float32 => new NullReaderValType<float>(nullReader, valueReader, LogicalLength, LogicalType),
            LogicalType.Float64 => new NullReaderValType<double>(nullReader, valueReader, LogicalLength, LogicalType),
            LogicalType.Blob => new NullReaderRefType<byte[]>(nullReader, valueReader, LogicalLength, LogicalType),
            LogicalType.String => new NullReaderRefType<string>(nullReader, valueReader, LogicalLength, LogicalType),
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