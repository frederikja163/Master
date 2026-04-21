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
    Null = 4,
    RunLength = 5,
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
    IColumn? Encode<T>(IColumnReader<T> reader) where T : notnull;

    /// <summary>
    /// Create a decoder for this type of encoding.
    /// The decoder must be an IColumnReader,
    /// and the IColumnReader should ideally have a more specific type that depends on the type of LogicalType.
    /// </summary>
    IColumnReader CreateDecoder(LogicalType type, int length, GenericReader metadataReader, params IEnumerable<IColumnReader> childColumns);

    /// <summary>
    /// Gets the supported LogicalTypes of this Encoding.
    /// See <see cref="TypeHelper"/> for methods to get common groups of types.
    /// </summary>
    IEnumerable<LogicalType> GetSupportedTypes();
}

/// <summary>
/// Extensions all <see cref="IEncoding"/> will have in common.
/// </summary>
public static class EncodingExtensions
{
    /// <summary>
    /// Encode a column on an encoding.
    /// Will call the underlying <see cref="IEncoding.Encode"/> method with the correct type of reader.
    /// </summary>
    public static IColumn? Encode(this IEncoding encoding, IColumn column)
    {
        return column.LogicalType switch
        {
            LogicalType.SInt8 => encoding.Encode(column.OpenReader<sbyte>()),
            LogicalType.SInt16 => encoding.Encode(column.OpenReader<short>()),
            LogicalType.SInt32 => encoding.Encode(column.OpenReader<int>()),
            LogicalType.SInt64 => encoding.Encode(column.OpenReader<long>()),
            LogicalType.UInt8 => encoding.Encode(column.OpenReader<byte>()),
            LogicalType.UInt16 => encoding.Encode(column.OpenReader<ushort>()),
            LogicalType.UInt32 => encoding.Encode(column.OpenReader<uint>()),
            LogicalType.UInt64 => encoding.Encode(column.OpenReader<ulong>()),
            LogicalType.Float16 => encoding.Encode(column.OpenReader<Half>()),
            LogicalType.Float32 => encoding.Encode(column.OpenReader<float>()),
            LogicalType.Float64 => encoding.Encode(column.OpenReader<double>()),
            LogicalType.Blob => encoding.Encode(column.OpenReader<byte[]>()),
            LogicalType.String => encoding.Encode(column.OpenReader<string>()),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}