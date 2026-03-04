using System.Diagnostics;
using System.Runtime.CompilerServices;
using Master.Serializing.Encodings;

namespace Master.Serializing.Columns;

internal sealed class BitPackingColumn : IColumnParent
{
    public byte PrefixLength { get; set; }
    public ulong Prefix { get; set; }
    public int LogicalLength { get; set; }
    public LogicalType LogicalType { get; set; }
    public EncodingId EncodingId => EncodingId.BitPacking;
    public IColumn Column { get; set; }
    private static readonly int Size = Unsafe.SizeOf<byte>() +
                                       Unsafe.SizeOf<ulong>() +
                                       Unsafe.SizeOf<int>();

    public BitPackingColumn(IColumn placeholder)
    {
        Column = placeholder;
    }
    
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

    public void Swap(IColumn existingColumn, IColumn newColumn)
    {
        Debug.Assert(existingColumn == Column);
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