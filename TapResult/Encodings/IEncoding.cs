using Master.Columns;
using Master.Readers;

namespace Master.Encodings;

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
    IColumn Encode(in DataColumn dataColumn);

    IColumnReader CreateDecoder(LogicalType type, ref GenericReader metadataReader, params IEnumerable<IColumnReader> childColumns);

    IEnumerable<LogicalType> GetSupportedTypes();
}