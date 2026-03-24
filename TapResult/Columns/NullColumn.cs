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
    public void WriteMetadata(ColumnBuilder blobBuilder)
    {
        blobBuilder.Write(Unsafe.SizeOf<int>());
        blobBuilder.Write(LogicalLength);
    }

    public IColumnReader OpenReader() => LogicalType switch
    {
        LogicalType.SInt8 => new NullReaderValType<sbyte>(_nullColumn.OpenReader<byte>(), _valueColumn.OpenReader(),
            LogicalLength),
        LogicalType.SInt16 => new NullReaderValType<short>(_nullColumn.OpenReader<byte>(), _valueColumn.OpenReader(),
            LogicalLength),
        LogicalType.SInt32 => new NullReaderValType<int>(_nullColumn.OpenReader<byte>(), _valueColumn.OpenReader(),
            LogicalLength),
        LogicalType.SInt64 => new NullReaderValType<long>(_nullColumn.OpenReader<byte>(), _valueColumn.OpenReader(),
            LogicalLength),
        LogicalType.UInt8 => new NullReaderValType<byte>(_nullColumn.OpenReader<byte>(), _valueColumn.OpenReader(),
            LogicalLength),
        LogicalType.UInt16 => new NullReaderValType<ushort>(_nullColumn.OpenReader<byte>(), _valueColumn.OpenReader(),
            LogicalLength),
        LogicalType.UInt32 => new NullReaderValType<uint>(_nullColumn.OpenReader<byte>(), _valueColumn.OpenReader(),
            LogicalLength),
        LogicalType.UInt64 => new NullReaderValType<ulong>(_nullColumn.OpenReader<byte>(), _valueColumn.OpenReader(),
            LogicalLength),
        LogicalType.Float16 => new NullReaderValType<Half>(_nullColumn.OpenReader<byte>(), _valueColumn.OpenReader(),
            LogicalLength),
        LogicalType.Float32 => new NullReaderValType<float>(_nullColumn.OpenReader<byte>(), _valueColumn.OpenReader(),
            LogicalLength),
        LogicalType.Float64 => new NullReaderValType<double>(_nullColumn.OpenReader<byte>(), _valueColumn.OpenReader(),
            LogicalLength),
        LogicalType.Blob => new NullReaderRefType<byte[]>(_nullColumn.OpenReader<byte>(), _valueColumn.OpenReader(),
            LogicalLength),
        LogicalType.String => new NullReaderRefType<string>(_nullColumn.OpenReader<byte>(), _valueColumn.OpenReader(),
            LogicalLength),
        _ => throw new ArgumentOutOfRangeException()
    };

    public IEnumerable<IColumn> GetChildColumns()
    {
        yield return _nullColumn;
        yield return _valueColumn;
    }

    public void Swap(IColumn existingColumn, IColumn newColumn)
    {
        if (existingColumn.Equals(_nullColumn))
        {
            _nullColumn = newColumn;
        }
        else if (existingColumn.Equals(_valueColumn))
        {
            _valueColumn = newColumn;
        }
    }
}