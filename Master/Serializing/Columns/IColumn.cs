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
    public EncodingId Id { get; }
    public IEnumerable<DataColumn> GetDataColumns();
    internal void WriteMetadata(DataColumnBuilder builder);
}