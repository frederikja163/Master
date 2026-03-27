using TapResult.Encodings;
using TapResult.Readers;

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


    public void WriteMetadata(ColumnBuilder blobBuilder)
    {
        blobBuilder.WriteBlob(ReadOnlySpan<byte>.Empty);
    }

    public IColumnReader OpenReader()
    {
        return new SplitColumnReader(LengthColumn.OpenReader<int>(), ByteColumn.OpenReader<byte>(), LogicalType);
    }

    public IEnumerable<IColumn> GetChildColumns()
    {
        yield return LengthColumn;
        yield return ByteColumn;
    }

    public bool Swap(IColumn existingColumn, IColumn newColumn)
    {
        if (existingColumn.Equals(LengthColumn))
        {
            LengthColumn = newColumn;
            return true;
        }
        if (existingColumn.Equals(ByteColumn))
        {
            ByteColumn = newColumn;
            return true;
        }

        return false;
    }
}