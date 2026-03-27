using TapResult.Columns;
using TapResult.Readers;

namespace TapResult.Encodings;

/// <summary>
/// Describes what <b>IEncoding</b> to use. The encoding does not determine the type of IColumn as IColumns can be reused in different encodings.
/// </summary>
public enum EncodingType : byte
{
    Table = 0,
    Binary = 1,
    Split = 2,
    BitPacking = 3,
}

/// <summary>
/// The base of all encodings.
/// </summary>
public interface IEncoding
{
    /// <summary>
    /// The type of this encoding. If making custom encodings, make sure it does not collide with any values used in <see cref="EncodingType"/>.
    /// </summary>
    public EncodingType Type { get; }
    /// <summary>
    /// Encode a DataColumn using the encoding specified by <see cref="Type"/>.
    /// </summary>
    IColumn Encode(in DataColumn dataColumn);

    /// <summary>
    /// Create a decoder for this type of encoding.
    /// The decoder must be an IColumnReader,
    /// and the IColumnReader should ideally have a more specific type that depends on the type of LogicalType.
    /// </summary>
    IColumnReader CreateDecoder(LogicalType type, GenericReader metadataReader, params IEnumerable<IColumnReader> childColumns);

    /// <summary>
    /// Gets the supported LogicalTypes of this Encoding.
    /// See <see cref="TypeHelper"/> for methods to get common groups of types.
    /// </summary>
    IEnumerable<LogicalType> GetSupportedTypes();
}