using System.Diagnostics;
using System.Runtime.CompilerServices;
using Master.Serializing.Encodings;

namespace Master.Serializing.Columns;

internal sealed class BitPackingColumn : IColumnParent
{
    public EncodingId Id => EncodingId.Split;

    public BitPackingColumn(DataColumn column, byte prefixLength, ulong prefix)
    {
        Column = column;
        PrefixLength = prefixLength;
        Prefix = prefix;
        LogicalLength = column.LogicalLength;
        LogicalType = column.LogicalType;
    }

    public IColumn Column;


    IEnumerable<IColumn> IColumnParent.GetChildColumns()
    {
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

    public static readonly int Size = Unsafe.SizeOf<byte>() +
                                       Unsafe.SizeOf<ulong>() +
                                       Unsafe.SizeOf<int>() +
                                       Unsafe.SizeOf<byte>();
    public byte PrefixLength { get; }
    public ulong Prefix { get; }
    public int LogicalLength { get; }
    public LogicalType LogicalType { get; }
        
    public void WriteMetadata(DataColumnBuilder builder)
    {
        builder.Write(PrefixLength);
        builder.Write(Prefix);
        builder.Write(LogicalLength);
    }
}