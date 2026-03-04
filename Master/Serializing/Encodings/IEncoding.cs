using Master.Serializing.Columns;
using Master.Serializing.Readers;

namespace Master.Serializing.Encodings;

/// <summary>
/// Describes what <b>IEncoding</b> to use. The encoding does not determine the type of IColumn as IColumns can be reused in different encodings.
/// </summary>
public enum EncodingId : byte
{
    Table = 0,
    Binary = 1,
    Split = 2,
    BitPacking = 3,
}

public interface IEncoding
{
    public EncodingId Id { get; }
    IColumn Encode(ref DataColumn dataColumn);

    IColumnReader CreateDecoder(LogicalType type, GenericReader metadataReader, params IEnumerable<IColumnReader> childColumns);

    IEnumerable<LogicalType> GetSupportedTypes();
}