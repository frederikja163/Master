using Master.Serializing.Encodings;

namespace Master.Serializing.Columns;

public class SplitColumn : IColumn
{
    public readonly DataColumn _lengthColumn;
    public readonly DataColumn _byteColumn;
    public readonly LogicalType _logicalType;

    public SplitColumn(DataColumn lengthColumn, DataColumn byteColumn, LogicalType logicalType)
    {
        _lengthColumn = lengthColumn;
        _byteColumn = byteColumn;
        _logicalType = logicalType;
    }
    
    public int CalculateTotalLength()
    {
        return GetDataColumns().Sum(d => d.CalculateTotalLength());
    }

    public EncodingId Id { get; } = EncodingId.Split;
    public IEnumerable<DataColumn> GetDataColumns()
    {
        yield return _lengthColumn;
        yield return _byteColumn;
    }
    
    // DataColumn.Create<byte>(BitConverter.GetBytes((int)dataColumn.LogicalType))
    //        LogicalType type = (LogicalType)metadataReader.Read<int>();
}