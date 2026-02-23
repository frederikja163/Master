using Master.Serializing.Encodings;

namespace Master.Serializing.Columns;

internal sealed class SplitColumn : IColumnParent
{
    public IColumn LengthColumn;
    public IColumn ByteColumn;
    public readonly LogicalType LogicalType;

    public SplitColumn(IColumn lengthColumn, IColumn byteColumn, LogicalType logicalType)
    {
        LengthColumn = lengthColumn;
        ByteColumn = byteColumn;
        LogicalType = logicalType;
    }
    
    public int CalculateTotalLength()
    {
        return GetDataColumns().Sum(d => d.CalculateTotalLength());
    }

    public EncodingId Id => EncodingId.Split;
    public IEnumerable<DataColumn> GetDataColumns() => LengthColumn.GetDataColumns().Concat(ByteColumn.GetDataColumns());

    void IColumn.WriteMetadata(DataColumnBuilder builder)
    {
        throw new NotImplementedException();
    }

    IEnumerable<IColumn> IColumnParent.GetChildColumns()
    {
        yield return LengthColumn;
        yield return ByteColumn;
    }

    void IColumnParent.Swap(IColumn existingColumn, IColumn newColumn)
    {
        if (existingColumn == LengthColumn)
        {
            LengthColumn = newColumn;
        }
        if (existingColumn == ByteColumn)
        {
            ByteColumn = newColumn;
        }
    }
}