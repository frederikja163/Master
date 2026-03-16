using Master.Serializing.Encodings;

namespace Master.Serializing.Columns;

public interface IColumn
{
    /// <summary>
    /// Calculates length of all data columns contained in this column
    /// </summary>
    /// <returns></returns>
    public int CalculateTotalLength();
    /// <summary>
    /// Describes What encoding has been used
    /// </summary>
    public EncodingId EncodingId { get; }
    public LogicalType LogicalType { get; }
    public IEnumerable<DataColumn> GetDataColumns();
    internal void WriteMetadata(ref DataColumnBuilder blobBuilder);
}