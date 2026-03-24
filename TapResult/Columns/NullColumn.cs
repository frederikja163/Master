using System.Runtime.CompilerServices;
using TapResult.Encodings;

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