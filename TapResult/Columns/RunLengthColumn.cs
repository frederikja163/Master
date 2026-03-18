using System.Runtime.CompilerServices;
using TapResult.Encodings;

namespace TapResult.Columns;

public struct RunLengthColumn(LogicalType logicalType, IColumn byteColumn, IColumn repeatColumn, int byteLength, int length) : IColumnParent
{
    public EncodingId EncodingId { get; } = EncodingId.RunLength;
    public LogicalType LogicalType { get; } = logicalType;

    public IColumn ByteColumn { get; set; } = byteColumn;
    public IColumn RepeatColumn { get; set; } = repeatColumn;
    public int Length { get; set; } = length;
    public int ByteLength { get; set; } = byteLength;
    internal static readonly int Size = Unsafe.SizeOf<int>() + Unsafe.SizeOf<int>();

    public int CalculateTotalLength()
    {
        return GetDataColumns().Sum(d => d.LogicalLength);
    }
    
    public IEnumerable<DataColumn> GetDataColumns() => ByteColumn.GetDataColumns().Concat(RepeatColumn.GetDataColumns());


    void IColumn.WriteMetadata(ref DataColumnBuilder blobBuilder)
    {
        blobBuilder.Write(Size);
        blobBuilder.WriteRaw(ByteLength);
        blobBuilder.WriteRaw(Length);
    }

    IEnumerable<IColumn> IColumnParent.GetChildColumns(bool recursive)
    {
        if (recursive)
        {
            if (ByteColumn is IColumnParent columnParent)
            {
                foreach (IColumn childColumn in columnParent.GetChildColumns(true))
                {
                    yield return childColumn;
                }
            }
        
            if (RepeatColumn is IColumnParent columnParent2)
            {
                foreach (IColumn childColumn in columnParent2.GetChildColumns(true))
                {
                    yield return childColumn;
                }
            }
        }
        yield return ByteColumn;
        yield return RepeatColumn;
    }

    void IColumnParent.Swap(in IColumn existingColumn, in IColumn newColumn)
    {
        if (existingColumn.Equals(ByteColumn))
        {
            ByteColumn = newColumn;
        }
        if (existingColumn.Equals(RepeatColumn))
        {
            RepeatColumn = newColumn;
        }
    }
}