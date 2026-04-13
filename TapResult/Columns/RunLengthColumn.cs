using System.Runtime.CompilerServices;
using TapResult.Encodings;
using TapResult.Readers;

namespace TapResult.Columns;

internal sealed class RunLengthColumn : IColumnParent
{
    public RunLengthColumn(LogicalType logicalType, IColumn byteColumn, IColumn repeatColumn, int length)
    {
        LogicalType = logicalType;
        ByteColumn = byteColumn;
        RepeatColumn = repeatColumn;
        Length = length;
    }

    public EncodingType EncodingType { get; } = EncodingType.RunLength;
    public LogicalType LogicalType { get; }

    public IColumn ByteColumn { get; set; }
    public IColumn RepeatColumn { get; set; }
    public int Length { get; set; }
    internal static readonly int Size = Unsafe.SizeOf<int>();

    public void WriteMetadata(ColumnBuilder blobBuilder)
    {
        blobBuilder.Write(Size);
        blobBuilder.WriteRaw(Length);
    }

    public IColumnReader OpenReader()
    {
        return RunLengthEncoding.CreateReader(LogicalType, ByteColumn.OpenReader(), RepeatColumn.OpenReader<int>(), Length);
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