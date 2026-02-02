namespace Master.Serializing;

public enum EncodingId
{
    Binary,
    Split,
    BitPacking
}

internal interface IEncoding
{
    public EncodingId Id { get; }
    void Encode(DataColumn dataColumn, ref DataColumn metadataCol, out DataColumn[] outColumns);

    DataColumn Decode(DataColumn[] data, DataColumn metadata);

    IEnumerable<LogicalType> GetSupportedTypes();
}