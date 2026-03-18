using TapResult.Encodings;

namespace TapResult.Columns;

/// <summary>
/// The base of a column of a specific type. If you are making your own custom column you probably want a <see cref="IColumnParent"/>.
/// </summary>
public interface IColumn
{
    /// <summary>
    /// Calculates length of all data columns contained in this column
    /// </summary>
    public int CalculateTotalLength();
    /// <summary>
    /// Describes what encoding has been used for this column.
    /// </summary>
    public EncodingType EncodingType { get; }
    /// <summary>
    /// The logical type of this column.
    /// </summary>
    public LogicalType LogicalType { get; }
    /// <summary>
    /// Get the underlying data columns of this column.
    /// </summary>
    public IEnumerable<DataColumn> GetDataColumns(); // TODO: Maybe this should lie in IColumnParent.
    internal void WriteMetadata(ref DataColumnBuilder blobBuilder);
}