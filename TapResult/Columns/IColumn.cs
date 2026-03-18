using TapResult.Encodings;

namespace TapResult.Columns;

/// <summary>
/// The base of a column of a specific type. If you are making your own custom column you probably want a <see cref="IColumnParent"/>.
/// </summary>
public interface IColumn
{
    /// <summary>
    /// Describes what encoding has been used for this column.
    /// </summary>
    public EncodingType EncodingType { get; }
    /// <summary>
    /// The logical type of this column.
    /// </summary>
    public LogicalType LogicalType { get; }
    internal void WriteMetadata(ref DataColumnBuilder blobBuilder);
}