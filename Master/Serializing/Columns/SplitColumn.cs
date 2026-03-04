using Master.Serializing.Encodings;

namespace Master.Serializing.Columns;

internal sealed class SplitColumn : IColumnParent
{
    public IColumn LengthColumn { get; set; }
    public IColumn ByteColumn { get; set; }
    public LogicalType LogicalType { get; }
    public EncodingId EncodingId => EncodingId.Split;

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

    public IEnumerable<DataColumn> GetDataColumns() => LengthColumn.GetDataColumns().Concat(ByteColumn.GetDataColumns());

    void IColumn.WriteMetadata(ref DataColumnBuilder blobBuilder)
    {
        blobBuilder.WriteBlob(ReadOnlySpan<byte>.Empty);
    }

    IEnumerable<IColumn> IColumnParent.GetChildColumns(bool recursive)
    {
        if (recursive)
        {
            if (LengthColumn is IColumnParent columnParent)
            {
                foreach (IColumn childColumn in columnParent.GetChildColumns(true))
                {
                    yield return childColumn;
                }
            }
        
            if (ByteColumn is IColumnParent columnParent2)
            {
                foreach (IColumn childColumn in columnParent2.GetChildColumns(true))
                {
                    yield return childColumn;
                }
            }
        }
        yield return LengthColumn;
        yield return ByteColumn;
    }

    void IColumnParent.Swap(in IColumn existingColumn, in IColumn newColumn)
    {
        if (existingColumn.Equals(LengthColumn))
        {
            LengthColumn = newColumn;
        }
        if (existingColumn.Equals(ByteColumn))
        {
            ByteColumn = newColumn;
        }
    }
}