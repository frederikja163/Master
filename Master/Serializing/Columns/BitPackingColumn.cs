using System.Diagnostics;
using System.Runtime.CompilerServices;
using Master.Serializing.Encodings;

namespace Master.Serializing.Columns;

public sealed class BitPackingColumn : IColumn, IColumnParent
{
    public EncodingId Id => EncodingId.Split;

    public BitPackingColumn() {}
    public BitPackingColumn(DataColumn blob)
    {
        Debug.Assert(blob.PhysicalSize == Size);
        DataColumnReader reader = blob.OpenReader();
        PrefixLength = reader.Read<byte>();
        Prefix = reader.Read<ulong>();
        LogicalLength = reader.Read<int>();
        Type = (LogicalType)reader.Read<byte>();
    }


    public IColumn[] Columns { get; set; } = [];

    public int CalculateTotalLength()
    {
        return Columns.Sum(column => column.CalculateTotalLength());
    }

    public IEnumerable<DataColumn> GetDataColumns()
    {
        foreach (IColumn column in Columns)
        {
            foreach (DataColumn data in column.GetDataColumns())
            {
                yield return data;
            }
        }
    }
    
    public static readonly int Size = Unsafe.SizeOf<byte>() +
                                      Unsafe.SizeOf<ulong>() +
                                      Unsafe.SizeOf<int>() +
                                      Unsafe.SizeOf<byte>();
    public byte PrefixLength { get; set; }
    public ulong Prefix { get; set; }
    public int LogicalLength { get; set; }
    public LogicalType Type { get; set; }
        
    public DataColumn ToDataColumn()
    {
        DataColumnBuilder builder = new DataColumnBuilder(Size);
        builder.Write(PrefixLength);
        builder.Write(Prefix);
        builder.Write(LogicalLength);
        builder.Write((byte)Type);
        return builder.Build();
    }
}