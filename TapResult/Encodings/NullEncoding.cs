using TapResult.Columns;
using TapResult.Readers;

namespace TapResult.Encodings;

/// <summary>
/// Encoding of null values.
/// </summary>
public sealed class NullEncoding : IEncoding
{
    public EncodingType Type { get; } = EncodingType.Null;
    public IColumn? Encode<T>(IColumnReader<T> reader) where T : notnull
    {
        return null;
    }

    public IColumnReader CreateDecoder(LogicalType type, int length, GenericReader metadataReader,
        params IEnumerable<IColumnReader> childColumns)
    {
        using IEnumerator<IColumnReader> childColumnEnumerator = childColumns.GetEnumerator();
        if (!childColumnEnumerator.MoveNext() || childColumnEnumerator.Current is not IColumnReader<byte> nulls ||
            !childColumnEnumerator.MoveNext() || childColumnEnumerator.Current is not {} values ||
            childColumnEnumerator.MoveNext())
            throw new Exception("Child columns not configured correctly for null column.");
        return NullColumn.ColumnReader(type, length, nulls, values);
    }

    public IEnumerable<LogicalType> GetSupportedTypes()
    {
        yield break;
    }
}