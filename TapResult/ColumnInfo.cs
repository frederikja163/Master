namespace Master;

/// <summary>
/// TODO
/// </summary>
public sealed class ColumnInfo
{
    /// <summary>
    /// TODO
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// TODO
    /// </summary>
    public EncodingInfo Encoding { get; }
    /// <summary>
    /// TODO
    /// </summary>
    public TableInfo TableInfo { get; }
    
    internal ColumnInfo(string name, EncodingInfo encoding, TableInfo tableInfo)
    {
        Name = name;
        Encoding = encoding;
        TableInfo = tableInfo;
    }
}