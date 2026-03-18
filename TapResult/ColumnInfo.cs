namespace TapResult;

/// <summary>
/// Info about a column, used for opening a reader for a specific column.
/// </summary>
public sealed class ColumnInfo
{
    /// <summary>
    /// The name of the column.
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// The underlying encoding of this column.
    /// </summary>
    public EncodingInfo Encoding { get; }
    /// <summary>
    /// The table this column belongs to.
    /// </summary>
    public TableInfo TableInfo { get; }
    
    internal ColumnInfo(string name, EncodingInfo encoding, TableInfo tableInfo)
    {
        Name = name;
        Encoding = encoding;
        TableInfo = tableInfo;
    }
}