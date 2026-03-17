using TapResult.Columns;
using TapResult.Readers;

namespace TapResult.Encodings;

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

/// <summary>
/// TODO
/// </summary>
public interface IEncoding
{
    /// <summary>
    /// TODO
    /// </summary>
    public EncodingId Id { get; }
    /// <summary>
    /// TODO
    /// </summary>
    IColumn Encode(in DataColumn dataColumn);

    /// <summary>
    /// TODO
    /// </summary>
    IColumnReader CreateDecoder(LogicalType type, ref GenericReader metadataReader, params IEnumerable<IColumnReader> childColumns);

    /// <summary>
    /// TODO
    /// </summary>
    IEnumerable<LogicalType> GetSupportedTypes();
}