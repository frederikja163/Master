using Master.Encodings;

namespace Master.Columns;

/// <summary>
/// TODO
/// </summary>
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
    /// <summary>
    /// TODO
    /// </summary>
    public LogicalType LogicalType { get; }
    /// <summary>
    /// TODO
    /// </summary>
    public IEnumerable<DataColumn> GetDataColumns();
    internal void WriteMetadata(ref DataColumnBuilder blobBuilder);
}