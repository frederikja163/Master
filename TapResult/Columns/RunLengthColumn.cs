using System.Runtime.CompilerServices;
using TapResult.Encodings;
using TapResult.Readers;

namespace TapResult.Columns;

internal sealed class RunLengthColumn : IColumnParent
{
    public RunLengthColumn(LogicalType logicalType, IColumn byteColumn, IColumn repeatColumn, int logicalLength)
    {
        LogicalType = logicalType;
        ByteColumn = byteColumn;
        RepeatColumn = repeatColumn;
        LogicalLength = logicalLength;
    }

    public EncodingType EncodingType { get; } = EncodingType.RunLength;
    public LogicalType LogicalType { get; }

    public IColumn ByteColumn { get; set; }
    public IColumn RepeatColumn { get; set; }
    public int LogicalLength { get; set; }

    public void WriteMetadata(IBlobBuilder blobBuilder)
    {
    }

    public IColumnReader OpenReader()
    {
        return RunLengthEncoding.CreateReader(LogicalType, ByteColumn.OpenReader(), RepeatColumn.OpenReader<int>(), LogicalLength);
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