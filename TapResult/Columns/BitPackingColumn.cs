using System.Diagnostics;
using System.Runtime.CompilerServices;
using Master.Serializing.Encodings;

namespace Master.Serializing.Columns;

internal sealed class BitPackingColumn : IColumnParent
{
    public byte PrefixLength { get; }
    public ulong Prefix { get; }
    public int LogicalLength { get; }
    public LogicalType LogicalType { get; }
    public EncodingId EncodingId => EncodingId.BitPacking;
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
    
    IEnumerable<IColumn> IColumnParent.GetChildColumns(bool recursive)
    {
        if (recursive && Column is IColumnParent columnParent)
        {
            foreach (IColumn childColumn in columnParent.GetChildColumns(true))
            {
                yield return childColumn;
            }
        }
        yield return Column;
    }

    public void Swap(in IColumn existingColumn, in IColumn newColumn)
    {
        Debug.Assert(existingColumn.Equals(Column));
        Column = newColumn;
    }

    public int CalculateTotalLength()
    {
        return GetDataColumns().Sum(column => column.CalculateTotalLength());
    }

    public IEnumerable<DataColumn> GetDataColumns()
    {
        return Column.GetDataColumns();
    }
    
    void IColumn.WriteMetadata(ref DataColumnBuilder blobBuilder)
    {
        blobBuilder.Write(Size);
        blobBuilder.WriteRaw(PrefixLength);
        blobBuilder.WriteRaw(Prefix);
        blobBuilder.WriteRaw(LogicalLength);
    }
}