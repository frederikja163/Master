using System.Collections.ObjectModel;

namespace Master.Serializing;

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
}

public static class TypeHelper
{
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
            _ => throw new ArgumentOutOfRangeException(nameof(logicalType), logicalType, null)
        };

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
            {typeof(string), LogicalType.String},
            {typeof(byte[]), LogicalType.Blob},
        });

    public static LogicalType ToLogicalType(this Type type) => PhysicalTypes[type];

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
    public static IEnumerable<LogicalType> FloatTypes()
    {
        yield return LogicalType.Float16;
        yield return LogicalType.Float32;
        yield return LogicalType.Float64;
    }

    public static IEnumerable<LogicalType> NumericTypes()
    {
        return IntegerTypes().Concat(FloatTypes());
    }

    public static IEnumerable<LogicalType> BlobTypes()
    {
        yield return LogicalType.Blob;
        yield return LogicalType.String;
    }

    public static IEnumerable<LogicalType> AllTypes()
    {
        return NumericTypes().Concat(BlobTypes());
    }
}