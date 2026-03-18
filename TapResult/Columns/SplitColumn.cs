using TapResult.Encodings;

namespace TapResult.Columns;

internal sealed class SplitColumn : IColumnParent
{
    public IColumn LengthColumn { get; set; }
    public IColumn ByteColumn { get; set; }
    public LogicalType LogicalType { get; }
    public EncodingType EncodingType => EncodingType.Split;

    public SplitColumn(IColumn lengthColumn, IColumn byteColumn, LogicalType logicalType)
    {
        LengthColumn = lengthColumn;
        ByteColumn = byteColumn;
        LogicalType = logicalType;
    }


    public void WriteMetadata(ref DataColumnBuilder blobBuilder)
    {
        blobBuilder.WriteBlob(ReadOnlySpan<byte>.Empty);
    }

    public IEnumerable<IColumn> GetChildColumns()
    {
        yield return LengthColumn;
        yield return ByteColumn;
    }

    public void Swap(in IColumn existingColumn, in IColumn newColumn)
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