using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using TapResult.Columns;
using TapResult.Readers;

namespace TapResult.Encodings;

/// <summary>
/// Split encoding takes variable length columns and splits the length and the data into separate columns.
/// </summary>
public sealed class SplitEncoding : IEncoding
{
    public EncodingType Type { get; } = EncodingType.Split;
    public IColumn? Encode<T>(IColumnReader<T> reader) where T : notnull
    {
        return null;
    }

    public IColumnReader CreateDecoder(LogicalType type, int length,
        GenericReader metadataReader, IEnumerable<IColumnReader> childColumns)
    {
        using IEnumerator<IColumnReader> childColumnEnumerator = childColumns.GetEnumerator();
        if (!childColumnEnumerator.MoveNext() || childColumnEnumerator.Current is not IColumnReader<int> lengths ||
            !childColumnEnumerator.MoveNext() || childColumnEnumerator.Current is not IColumnReader<byte> bytes ||
            childColumnEnumerator.MoveNext())
            throw new Exception("Child columns not configured correctly for split column.");
        return new SplitColumnReader(lengths, bytes, type);
    }

    public IEnumerable<LogicalType> GetSupportedTypes()
    {
        yield break;
    }
}