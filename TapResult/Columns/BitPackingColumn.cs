using System.Diagnostics;
using System.Runtime.CompilerServices;
using TapResult.Encodings;

namespace TapResult.Columns;

internal sealed class BitPackingColumn : IColumnParent
{
    public byte PrefixLength { get; }
    public ulong Prefix { get; }
    public int LogicalLength { get; }
    public LogicalType LogicalType { get; }
    public EncodingType EncodingType => EncodingType.BitPacking;
    public IColumn Column { get; set; }
    internal static readonly int Size = Unsafe.SizeOf<byte>() +
                                       Unsafe.SizeOf<ulong>() +
                                       Unsafe.SizeOf<int>();

    public BitPackingColumn(IColumn column, byte prefixLength, ulong prefix, int logicalLength)
    {
        Column = column;
        PrefixLength = prefixLength;
        Prefix = prefix;
        LogicalLength = logicalLength;
        LogicalType = column.LogicalType;
        
    }
    
    public BitPackingColumn(DataColumn column, byte prefixLength, ulong prefix) : this(column, prefixLength, prefix, column.LogicalLength)
    { }

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

    public void WriteMetadata(ref DataColumnBuilder blobBuilder)
    {
        blobBuilder.Write(Size);
        blobBuilder.WriteRaw(PrefixLength);
        blobBuilder.WriteRaw(Prefix);
        blobBuilder.WriteRaw(LogicalLength);
    }
}