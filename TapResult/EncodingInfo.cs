using Master.Encodings;

namespace Master;

/// <summary>
/// TODO
/// </summary>
public sealed class EncodingInfo
{
    private readonly List<EncodingInfo> _subEncodings = new();
    /// <summary>
    /// TODO
    /// </summary>
    public int Id { get; }
    /// <summary>
    /// TODO
    /// </summary>
    public int ParentId { get; }
    /// <summary>
    /// TODO
    /// </summary>
    public EncodingId Encoding { get; }
    /// <summary>
    /// TODO
    /// </summary>
    public LogicalType Type { get; }
    /// <summary>
    /// TODO
    /// </summary>
    public ReadOnlyMemory<byte> Blob { get; }
    /// <summary>
    /// TODO
    /// </summary>
    public EncodingInfo? ParentEncoding { get; private set; } = null;

    internal EncodingInfo(int id, int parentId, EncodingId encoding, LogicalType type, ReadOnlyMemory<byte> blob)
    {
        Id = id;
        ParentId = parentId;
        Encoding = encoding;
        Type = type;
        Blob = blob;
    }

    /// <summary>
    /// TODO
    /// </summary>
    public IEnumerable<EncodingInfo> GetSubEncodings()
    {
        return _subEncodings;
    }

    /// <summary>
    /// TODO
    /// </summary>
    internal void AddSubEncoding(EncodingInfo subEncoding)
    {
        _subEncodings.Add(subEncoding);
        subEncoding.ParentEncoding = this;
    }
}