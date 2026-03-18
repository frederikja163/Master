using TapResult.Encodings;

namespace TapResult;

/// <summary>
/// Provides information about the encoding of a <see cref="ColumnInfo"/>
/// </summary>
public sealed class EncodingInfo
{
    private readonly List<EncodingInfo> _subEncodings = new();
    /// <summary>
    /// The ID of this encoding. Usually this doesn't contain anything useful.
    /// </summary>
    public int Id { get; }
    /// <summary>
    /// The ID of the parent encoding. Usually this doesn't contain anything useful.
    /// </summary>
    public int ParentId { get; }
    /// <summary>
    /// The type of encoding used.
    /// </summary>
    public EncodingType Encoding { get; }
    /// <summary>
    /// The logical type of this column.
    /// </summary>
    public LogicalType Type { get; }
    /// <summary>
    /// A blob containing metadata for this encoding.
    /// </summary>
    public ReadOnlyMemory<byte> Blob { get; }
    /// <summary>
    /// The parent of this encoding if any.
    /// </summary>
    public EncodingInfo? ParentEncoding { get; private set; } = null;

    internal EncodingInfo(int id, int parentId, EncodingType encoding, LogicalType type, ReadOnlyMemory<byte> blob)
    {
        Id = id;
        ParentId = parentId;
        Encoding = encoding;
        Type = type;
        Blob = blob;
    }

    /// <summary>
    /// Get the sub encodings of this encoding.
    /// </summary>
    public IEnumerable<EncodingInfo> GetSubEncodings()
    {
        return _subEncodings;
    }
    
    internal void AddSubEncoding(EncodingInfo subEncoding)
    {
        _subEncodings.Add(subEncoding);
        subEncoding.ParentEncoding = this;
    }
}