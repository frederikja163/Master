namespace Master.Serializing;

public enum EncodingId
{
    Binary,
    Split,
}

internal interface IEncoding
{
    public EncodingId Id { get; }
    Column Encode(PhysicalColumn physicalColumn, ReadOnlyMemory<byte>? suggestedParameters = null);

    PhysicalColumn Decode(ReadOnlyMemory<byte>[] data, ReadOnlyMemory<byte> parameters);

    IEnumerable<LogicalType> GetSupportedTypes();
}