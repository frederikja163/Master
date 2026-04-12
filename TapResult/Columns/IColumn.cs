using TapResult.Encodings;
using TapResult.Readers;

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
    internal void WriteMetadata(ColumnBuilder blobBuilder);

    /// <summary>
    /// Opens a reader that reads the values of this Column.
    /// </summary>
    public IColumnReader OpenReader();
}

/// <summary>
/// Extensions for IColumn.
/// </summary>
public static class ColumnExtensions
{
    /// <summary>
    /// Open a typed reader that reads the values of this Column.
    /// Will give an error if the type of T is not the same as <see cref="LogicalType"/>.
    /// </summary>
    public static IColumnReader<T> OpenReader<T>(this IColumn column)
    {
        if ((typeof(T) != column.LogicalType.ToCsType() && Nullable.GetUnderlyingType(typeof(T)) != column.LogicalType.ToCsType())
            || column.OpenReader() is not IColumnReader<T> reader)
        {
            throw new ArgumentException($"Type {typeof(T).Name} is not valid for logical type {column.LogicalType}, expected {column.LogicalType.ToCsType().Name}", nameof(T));
        }

        return reader;
    }
}