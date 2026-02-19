using Master.Serializing.Columns;

namespace Master.Serializing.Encodings;

/// <summary>
/// Describes what <b>IEncoding</b> to use. The encoding does not determine the type of IColumn as IColumns can be reused in different encodings.
/// </summary>
public enum EncodingId
{
    Binary,
    Split,
    BitPacking
}

public interface IEncoding
{
    public EncodingId Id { get; }
    IColumn Encode(DataColumn dataColumn);

    DataColumn Decode(IColumn data);
    //DataColumn Decode(ReadOnlySpan<byte> metadata, EncodingId id, int logicalLength, int physicalLength);

    IEnumerable<LogicalType> GetSupportedTypes();
}