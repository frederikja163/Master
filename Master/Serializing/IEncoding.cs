namespace Master.Serializing;

public enum EncodingId
{
    Binary,
    Split,
}

internal interface IEncoding
{
    public EncodingId Id { get; }
    void Encode(DataColumn dataColumn, ref DataColumn metadata, out DataColumn[] outColumns);

    DataColumn Decode(DataColumn[] data, DataColumn metadata);

    IEnumerable<LogicalType> GetSupportedTypes();
}