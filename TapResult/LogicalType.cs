using System.Collections.ObjectModel;

namespace TapResult;

/// <summary>
/// Logical types supported by the file format.
/// These represent the logical type, as the physical type will always just be an array of bytes,
/// that store data according to some conventions.
/// Primitive types are stored as little endian,
/// and variable length types (blob and string) are stored as length prefixed.
/// The length is an integer, and strings are stored with UTF8.
/// </summary>
public enum LogicalType : byte
{
    SInt8,
    SInt16,
    SInt32,
    SInt64,
    UInt8,
    UInt16,
    UInt32,
    UInt64,
    Float16,
    Float32,
    Float64,
    Blob,
    String,
    NullableSInt8,
    NullableSInt16,
    NullableSInt32,
    NullableSInt64,
    NullableUInt8,
    NullableUInt16,
    NullableUInt32,
    NullableUInt64,
    NullableFloat16,
    NullableFloat32,
    NullableFloat64,
    NullableBlob,
    NullableString,
}

/// <summary>
/// Helper methods for <see cref="LogicalType"/>, either conversions, groups, or size related methods.
/// </summary>
public static class TypeHelper
{
    /// <summary>
    /// Converts a <see cref="LogicalType"/> to a <see cref="System.Type"/>.
    /// </summary>
    public static Type ToCsType(this LogicalType logicalType)
        => logicalType switch
        {
            LogicalType.SInt8 => typeof(sbyte),
            LogicalType.SInt16 => typeof(short),
            LogicalType.SInt32 => typeof(int),
            LogicalType.SInt64 => typeof(long),
            LogicalType.UInt8 => typeof(byte),
            LogicalType.UInt16 => typeof(ushort),
            LogicalType.UInt32 => typeof(uint),
            LogicalType.UInt64 => typeof(ulong),
            LogicalType.Float16 => typeof(Half),
            LogicalType.Float32 => typeof(float),
            LogicalType.Float64 => typeof(double),
            LogicalType.String => typeof(string),
            LogicalType.Blob => typeof(byte[]),
            LogicalType.NullableSInt8 => typeof(sbyte?),
            LogicalType.NullableSInt16 => typeof(short?),
            LogicalType.NullableSInt32 => typeof(int?),
            LogicalType.NullableSInt64 => typeof(long?),
            LogicalType.NullableUInt8 => typeof(byte?),
            LogicalType.NullableUInt16 => typeof(ushort?),
            LogicalType.NullableUInt32 => typeof(uint?),
            LogicalType.NullableUInt64 => typeof(ulong?),
            LogicalType.NullableFloat16 => typeof(Half?),
            LogicalType.NullableFloat32 => typeof(float?),
            LogicalType.NullableFloat64 => typeof(double?),
            LogicalType.NullableBlob => typeof(string),
            LogicalType.NullableString => typeof(byte[]),
            _ => throw new ArgumentOutOfRangeException(nameof(logicalType), logicalType, null)
        };

    /// <summary>
    /// Tries to get the size of this logical type.
    /// If it is a variable length type it returns false otherwise it returns true,
    /// with the size in the size parameter.
    /// </summary>
    /// <remarks> For nullable types this function cannot find the length, as their size might be 0.</remarks>
    public static unsafe bool TryGetSize(this LogicalType logicalType, out int size)
    {
        (size, bool ret) = logicalType switch
        {
            LogicalType.SInt8 => (sizeof(sbyte), true),
            LogicalType.SInt16 => (sizeof(short), true),
            LogicalType.SInt32 => (sizeof(int), true),
            LogicalType.SInt64 => (sizeof(long), true),
            LogicalType.UInt8 => (sizeof(byte), true),
            LogicalType.UInt16 => (sizeof(ushort), true),
            LogicalType.UInt32 => (sizeof(uint), true),
            LogicalType.UInt64 => (sizeof(ulong), true),
            LogicalType.Float16 => (sizeof(Half), true),
            LogicalType.Float32 => (sizeof(float), true),
            LogicalType.Float64 => (sizeof(double), true),
            LogicalType.Blob => (sizeof(byte), false),
            LogicalType.String => (sizeof(char), false),
            LogicalType.NullableSInt8 => (sizeof(sbyte), false),
            LogicalType.NullableSInt16 => (sizeof(short), false),
            LogicalType.NullableSInt32 => (sizeof(int), false),
            LogicalType.NullableSInt64 => (sizeof(long), false),
            LogicalType.NullableUInt8 => (sizeof(byte), false),
            LogicalType.NullableUInt16 => (sizeof(ushort), false),
            LogicalType.NullableUInt32 => (sizeof(uint), false),
            LogicalType.NullableUInt64 => (sizeof(ulong), false),
            LogicalType.NullableFloat16 => (sizeof(Half), false),
            LogicalType.NullableFloat32 => (sizeof(float), false),
            LogicalType.NullableFloat64 => (sizeof(double), false),
            LogicalType.NullableBlob => (sizeof(byte), false),
            LogicalType.NullableString => (sizeof(char), false),
            _ => throw new ArgumentOutOfRangeException(nameof(logicalType), logicalType, null)
        };
        return ret;
    }

    private static readonly ReadOnlyDictionary<Type, LogicalType> PhysicalTypes = new ReadOnlyDictionary<Type, LogicalType>(
        new Dictionary<Type, LogicalType>()
        {
            {typeof(sbyte), LogicalType.SInt8},
            {typeof(short), LogicalType.SInt16},
            {typeof(int), LogicalType.SInt32},
            {typeof(long), LogicalType.SInt64},
            {typeof(byte), LogicalType.UInt8},
            {typeof(ushort), LogicalType.UInt16},
            {typeof(uint), LogicalType.UInt32},
            {typeof(ulong), LogicalType.UInt64},
            {typeof(Half), LogicalType.Float16},
            {typeof(float), LogicalType.Float32},
            {typeof(double), LogicalType.Float64},
            {typeof(string), LogicalType.NullableString},
            {typeof(byte[]), LogicalType.NullableBlob},
            {typeof(sbyte?), LogicalType.NullableSInt8},
            {typeof(short?), LogicalType.NullableSInt16},
            {typeof(int?), LogicalType.NullableSInt32},
            {typeof(long?), LogicalType.NullableSInt64},
            {typeof(byte?), LogicalType.NullableUInt8},
            {typeof(ushort?), LogicalType.NullableUInt16},
            {typeof(uint?), LogicalType.NullableUInt32},
            {typeof(ulong?), LogicalType.NullableUInt64},
            {typeof(Half?), LogicalType.NullableFloat16},
            {typeof(float?), LogicalType.NullableFloat32},
            {typeof(double?), LogicalType.NullableFloat64},
        });

    /// <summary>
    /// Converts a <see cref="System.Type"/> to a <see cref="LogicalType"/>.
    /// </summary>
    public static LogicalType ToLogicalType(this Type type) => PhysicalTypes[type];

    /// <summary>
    /// Reverses the nullable of a logical type.
    /// For nullable types, converts them to their non-nullable counterparts.
    /// For non-nullable types, converts them to their nullable counterparts.
    /// </summary>
    public static LogicalType ReverseNullability(this LogicalType type) => type switch
    {
        LogicalType.SInt8 => LogicalType.NullableSInt8,
        LogicalType.SInt16 => LogicalType.NullableSInt16,
        LogicalType.SInt32 => LogicalType.NullableSInt32,
        LogicalType.SInt64 => LogicalType.NullableSInt64,
        LogicalType.UInt8 => LogicalType.NullableUInt8,
        LogicalType.UInt16 => LogicalType.NullableUInt16,
        LogicalType.UInt32 => LogicalType.NullableUInt32,
        LogicalType.UInt64 => LogicalType.NullableUInt64,
        LogicalType.Float16 => LogicalType.NullableFloat16,
        LogicalType.Float32 => LogicalType.NullableFloat32,
        LogicalType.Float64 => LogicalType.NullableFloat64,
        LogicalType.Blob => LogicalType.NullableBlob,
        LogicalType.String => LogicalType.NullableString,
        LogicalType.NullableSInt8 => LogicalType.SInt8,
        LogicalType.NullableSInt16 => LogicalType.SInt16,
        LogicalType.NullableSInt32 => LogicalType.SInt32,
        LogicalType.NullableSInt64 => LogicalType.SInt64,
        LogicalType.NullableUInt8 => LogicalType.UInt8,
        LogicalType.NullableUInt16 => LogicalType.UInt16,
        LogicalType.NullableUInt32 => LogicalType.UInt32,
        LogicalType.NullableUInt64 => LogicalType.UInt64,
        LogicalType.NullableFloat16 => LogicalType.Float16,
        LogicalType.NullableFloat32 => LogicalType.Float32,
        LogicalType.NullableFloat64 => LogicalType.Float64,
        LogicalType.NullableBlob => LogicalType.Blob,
        LogicalType.NullableString => LogicalType.String,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    /// <summary>
    /// Checks if the type is nullable.
    /// </summary>
    public static bool IsNullable(this LogicalType type) => type switch
    {
        LogicalType.SInt8 => false,
        LogicalType.SInt16 => false,
        LogicalType.SInt32 => false,
        LogicalType.SInt64 => false,
        LogicalType.UInt8 => false,
        LogicalType.UInt16 => false,
        LogicalType.UInt32 => false,
        LogicalType.UInt64 => false,
        LogicalType.Float16 => false,
        LogicalType.Float32 => false,
        LogicalType.Float64 => false,
        LogicalType.Blob => false,
        LogicalType.String => false,
        LogicalType.NullableSInt8 => true,
        LogicalType.NullableSInt16 => true,
        LogicalType.NullableSInt32 => true,
        LogicalType.NullableSInt64 => true,
        LogicalType.NullableUInt8 => true,
        LogicalType.NullableUInt16 => true,
        LogicalType.NullableUInt32 => true,
        LogicalType.NullableUInt64 => true,
        LogicalType.NullableFloat16 => true,
        LogicalType.NullableFloat32 => true,
        LogicalType.NullableFloat64 => true,
        LogicalType.NullableBlob => true,
        LogicalType.NullableString => true,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    /// <summary>
    /// Converts a type to its nullable counterpart, or returns the type itself if it is already null.
    /// </summary>
    public static LogicalType ToNullable(this LogicalType type) => IsNullable(type) ? type : ReverseNullability(type);

    /// <summary>
    /// Converts a type to its non-nullable counterpart, or returns the type itself if it is already null.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static LogicalType ToNonNullable(this LogicalType type) =>
        IsNullable(type) ? ReverseNullability(type) : type;
    
    /// <summary>
    /// Gets all integer types as logical types.
    /// SInt8-SInt64 and UInt8-UInt64.
    /// </summary>
    public static IEnumerable<LogicalType> IntegerTypes()
    {
        yield return LogicalType.SInt8;
        yield return LogicalType.SInt16;
        yield return LogicalType.SInt32;
        yield return LogicalType.SInt64;
        yield return LogicalType.UInt8;
        yield return LogicalType.UInt16;
        yield return LogicalType.UInt32;
        yield return LogicalType.UInt64;
    }
    
    /// <summary>
    /// Gets all float types as logical types.
    /// Float16-Float64.
    /// </summary>
    public static IEnumerable<LogicalType> FloatTypes()
    {
        yield return LogicalType.Float16;
        yield return LogicalType.Float32;
        yield return LogicalType.Float64;
    }
    
    /// <summary>
    /// Gets all numeric types.
    /// Union of <see cref="IntegerTypes"/> and <see cref="FloatTypes"/>.
    /// </summary>
    public static IEnumerable<LogicalType> NumericTypes()
    {
        return IntegerTypes().Concat(FloatTypes());
    }
    
    /// <summary>
    /// Gets all variable length types.
    /// Blob and String.
    /// </summary>
    public static IEnumerable<LogicalType> VariableLengthTypes()
    {
        yield return LogicalType.Blob;
        yield return LogicalType.String;
    }
}