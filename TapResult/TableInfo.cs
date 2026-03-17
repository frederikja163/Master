using System.Diagnostics.CodeAnalysis;
using TapResult.Readers;

namespace TapResult;

/// <summary>
/// Contains information about a table that can be read from the metadata.
/// </summary>
public sealed class TableInfo
{
    private readonly Dictionary<string, ColumnInfo> _columns;
    /// <summary>
    /// The name of this table.
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// The encoding of this table.
    /// Mostly used internally to calculate sub encodings etc.
    /// </summary>
    public EncodingInfo Encoding { get; }

    internal TableInfo(EncodingInfo encoding)
    {
        Encoding = encoding;
        GenericReader reader = new GenericReader(encoding.Blob.Span);
        Name = reader.ReadString();
        _columns = new Dictionary<string, ColumnInfo>();
        foreach (EncodingInfo subEncoding in encoding.GetSubEncodings())
        {
            string name = reader.ReadString();
            ColumnInfo columnInfo = new ColumnInfo(name, subEncoding, this);
            _columns.Add(name, columnInfo);
        }
    }

    /// <summary>
    /// Gets all columns that are part of this table.
    /// </summary>
    public IEnumerable<ColumnInfo> GetColumns()
    {
        return _columns.Values;
    }

    /// <summary>
    /// Tries to get a column by name from this table.
    /// Returns true and found column if there is a column. Otherwise, returns false and null.
    /// </summary>
    public bool TryGetColumn(string name, [NotNullWhen(true)] out ColumnInfo? columnInfo)
    {
        return _columns.TryGetValue(name, out columnInfo);
    }
}