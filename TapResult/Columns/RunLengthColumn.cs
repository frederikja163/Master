using System.Runtime.CompilerServices;
using TapResult.Encodings;
using TapResult.Readers;

namespace TapResult.Columns;

internal sealed class RunLengthColumn(LogicalType logicalType, IColumn byteColumn, IColumn repeatColumn, int byteLength, int length) : IColumnParent
{
    public EncodingType EncodingType { get; } = EncodingType.RunLength;
    public LogicalType LogicalType { get; } = logicalType;

    public IColumn ByteColumn { get; set; } = byteColumn;
    public IColumn RepeatColumn { get; set; } = repeatColumn;
    public int Length { get; set; } = length;
    public int ByteLength { get; set; } = byteLength;
    internal static readonly int Size = Unsafe.SizeOf<int>() + Unsafe.SizeOf<int>();

    public void WriteMetadata(ColumnBuilder blobBuilder)
    {
        blobBuilder.Write(Size);
        blobBuilder.WriteRaw(ByteLength);
        blobBuilder.WriteRaw(Length);
    }

    public IColumnReader OpenReader()
    {
        throw new NotImplementedException();
    }

    public IEnumerable<IColumn> GetChildColumns()
    {
        yield return ByteColumn;
        yield return RepeatColumn;
    }

    public bool Swap(IColumn existingColumn, IColumn newColumn)
    {
        if (existingColumn.Equals(ByteColumn))
        {
            ByteColumn = newColumn;
            return true;
        }
        if (existingColumn.Equals(RepeatColumn))
        {
            RepeatColumn = newColumn;
            return true;
        }

        return false;
    }
}