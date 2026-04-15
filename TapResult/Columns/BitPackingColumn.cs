using System.Diagnostics;
using System.Runtime.CompilerServices;
using TapResult.Encodings;
using TapResult.Readers;

namespace TapResult.Columns;

internal sealed class BitPackingColumn : IColumnParent
{
    public byte PrefixLength { get; }
    public ulong Prefix { get; }
    public int LogicalLength { get; }
    public LogicalType LogicalType { get; }
    public EncodingType EncodingType => EncodingType.BitPacking;
    public IColumn Column { get; private set; }

    public BitPackingColumn(IColumn column, byte prefixLength, ulong prefix, int logicalLength)
    {
        Column = column;
        PrefixLength = prefixLength;
        Prefix = prefix;
        LogicalLength = logicalLength;
        LogicalType = column.LogicalType;
    }

    public IEnumerable<IColumn> GetChildColumns()
    {
        yield return Column;
    }

    public bool Swap(IColumn existingColumn, IColumn newColumn)
    {
        if (!existingColumn.Equals(Column))
            return false;
        Column = newColumn;
        return true;
    }

    public void WriteMetadata(IBlobBuilder blobBuilder)
    {
        blobBuilder.WriteValue(PrefixLength);
        blobBuilder.WriteValue(Prefix);
        blobBuilder.WriteValue(LogicalLength);
    }

    public IColumnReader OpenReader()
        => BitPacking.OpenReader(Column.OpenReader(), LogicalLength, LogicalType, PrefixLength, Prefix);
}