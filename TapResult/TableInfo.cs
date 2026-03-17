using System.Diagnostics.CodeAnalysis;
using Master.Readers;

namespace Master;

/// <summary>
/// TODO
/// </summary>
public sealed class TableInfo
{
    private readonly Dictionary<string, ColumnInfo> _columns;
    /// <summary>
    /// TODO
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// TODO
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
    /// TODO
    /// </summary>
    public IEnumerable<ColumnInfo> GetColumns()
    {
        return _columns.Values;
    }

    /// <summary>
    /// TODO
    /// </summary>
    public bool TryGetColumn(string name, [NotNullWhen(true)] out ColumnInfo? columnInfo)
    {
        return _columns.TryGetValue(name, out columnInfo);
    }
}