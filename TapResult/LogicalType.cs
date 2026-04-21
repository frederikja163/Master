using System.Collections.ObjectModel;
using System.Globalization;

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
            LogicalType.Blob => typeof(byte[]),
            LogicalType.String => typeof(string),
            _ => throw new ArgumentOutOfRangeException(nameof(logicalType), logicalType, null)
        };

    /// <summary>
    /// Tries to get the size of this logical type.
    /// If it is a variable length type it returns false otherwise it returns true,
    /// with the size in the size parameter.
    /// </summary>
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

    /// <summary>
    /// Converts a <see cref="System.Type"/> to a <see cref="LogicalType"/>.
    /// </summary>
    public static LogicalType ToLogicalType(this Type type) => PhysicalTypes[type];
    
        /// <summary>
        /// Tries to guess the type and promotes if higher than previousGuess.
        /// String > UInt64 > UInt32 > UInt16 > UInt8 > Blob
        /// String > SInt64 > SInt32 > SInt16 > SInt8 > Blob
        /// String > Float64 > Float32 > Float16 > Blob
        /// Float > SInt (no decimal) > UInt (no minus, no decimal)
        /// </summary>
        /// <param name="input"></param>
        /// <param name="previousGuess"></param>
        /// <returns></returns>
        public static LogicalType GuessTypeFromSpan(this ReadOnlySpan<char> input, LogicalType previousGuess = LogicalType.Blob)
        {
            if (input.IsEmpty)
                return previousGuess;

            if (previousGuess == LogicalType.String)
                return LogicalType.String;
            
            // Float-like values first: decimal point or exponent.
            if (input.IndexOfAny(['.', 'e', 'E']) >= 0)
            {
                if (!double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                    return LogicalType.String;

                return previousGuess switch
                {
                    LogicalType.Blob => LogicalType.Float16,
                    LogicalType.Float16 => LogicalType.Float16,
                    LogicalType.Float32 => LogicalType.Float32,
                    LogicalType.Float64 => LogicalType.Float64,
                    _ when IsFloat(previousGuess) => previousGuess,
                    _ => LogicalType.String
                };
            }

            // Signed Integers
            if (input[0] == '-')
            {
                if (!long.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
                    return LogicalType.String;
                var promoteSigned = PromoteSigned(integer, previousGuess);
                return IsSigned(promoteSigned) ? promoteSigned : LogicalType.String;
            }

            // Try signed first for consistency, then unsigned for large values.
            if (long.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out var signedValue))
            {
                if (signedValue < 0)
                {
                    var promoteSigned = PromoteSigned(signedValue, previousGuess);
                    return IsSigned(promoteSigned) ? promoteSigned : LogicalType.String;
                }

                var promoteUnsigned = PromoteUnsigned((ulong)signedValue, previousGuess);
                return IsUnsigned(promoteUnsigned) ? promoteUnsigned : LogicalType.String;
            }

            if (!ulong.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unsignedValue))
                return LogicalType.String;
            var promoted = PromoteUnsigned(unsignedValue, previousGuess);
            return IsUnsigned(promoted) ? promoted : LogicalType.String;
            

            static bool IsSigned(LogicalType t) => t is LogicalType.SInt8 or LogicalType.SInt16 or LogicalType.SInt32 or LogicalType.SInt64;
            static bool IsUnsigned(LogicalType t) => t is LogicalType.UInt8 or LogicalType.UInt16 or LogicalType.UInt32 or LogicalType.UInt64;
            static bool IsFloat(LogicalType t) => t is LogicalType.Float16 or LogicalType.Float32 or LogicalType.Float64;

            static LogicalType PromoteSigned(long value, LogicalType previous)
            {
                return value switch
                {
                    >= sbyte.MinValue and <= sbyte.MaxValue => 
                        previous is LogicalType.Blob or LogicalType.SInt8 
                            ? LogicalType.SInt8 : previous,
                    >= short.MinValue and <= short.MaxValue => 
                        previous is LogicalType.Blob or LogicalType.SInt8 or LogicalType.SInt16 
                            ? LogicalType.SInt16 : previous,
                    >= int.MinValue and <= int.MaxValue => 
                        previous is LogicalType.Blob or LogicalType.SInt8 or LogicalType.SInt16 or LogicalType.SInt32 
                            ? LogicalType.SInt32 : previous,
                    _ => previous is LogicalType.Blob or LogicalType.SInt8 or LogicalType.SInt16 or LogicalType.SInt32 or LogicalType.SInt64
                        ? LogicalType.SInt64 : previous
                };
            }

            static LogicalType PromoteUnsigned(ulong value, LogicalType previous)
            {
                return value switch
                {
                    <= byte.MaxValue => 
                        previous is LogicalType.Blob or LogicalType.UInt8 
                            ? LogicalType.UInt8 : previous,
                    <= ushort.MaxValue => 
                        previous is LogicalType.Blob or LogicalType.UInt8 or LogicalType.UInt16 
                            ? LogicalType.UInt16 : previous,
                    <= uint.MaxValue => 
                        previous is LogicalType.Blob or LogicalType.UInt8 or LogicalType.UInt16 or LogicalType.UInt32 
                            ? LogicalType.UInt32 : previous,
                    _ => previous is LogicalType.Blob or LogicalType.UInt8 or LogicalType.UInt16 or LogicalType.UInt32 or LogicalType.UInt64 
                        ? LogicalType.UInt64 : previous
                };
            }
        }

    private static readonly Dictionary<LogicalType, HashSet<LogicalType>> CompatibilityGroups = GetCompatibilityLookup();

    private static Dictionary<LogicalType, HashSet<LogicalType>> GetCompatibilityLookup()
    {
        Dictionary<LogicalType, HashSet<LogicalType>> compatibilityLookup = new Dictionary<LogicalType, HashSet<LogicalType>>();
        foreach (HashSet<LogicalType> compatibilityGroup in CompatibilityGroups().Select(c => c.ToHashSet()))
        {
            foreach (LogicalType logicalType in compatibilityGroup)
            {
                compatibilityLookup.Add(logicalType, compatibilityGroup);
            }
        }

        return compatibilityLookup;
        
        IEnumerable<IEnumerable<LogicalType>> CompatibilityGroups()
        {
            yield return VariableLengthTypes();
            yield return [LogicalType.SInt8, LogicalType.UInt8];
            yield return [LogicalType.SInt16, LogicalType.UInt16];
            yield return [LogicalType.SInt32, LogicalType.UInt32];
            yield return [LogicalType.SInt64, LogicalType.UInt64];
        }
    }
    
    /// <summary>
    /// Checks whether two logical types are compatible with each other.
    /// Compatibility means they have the same length and it makes sense to read one value as the other.
    /// For example a string is the same as a blob, and a uint32 is the same as a sint32.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="type2"></param>
    /// <returns></returns>
    public static bool IsCompatible(this LogicalType type, LogicalType type2)
    {
        return CompatibilityGroups.TryGetValue(type, out HashSet<LogicalType>? group) && group.Contains(type2);
    }
    
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
    /// <summary>
    /// Gets all types.
    /// Union of <see cref="NumericTypes"/> and <see cref="VariableLengthTypes"/>.
    /// </summary>
    public static IEnumerable<LogicalType> AllTypes()
    {
        return NumericTypes().Concat(VariableLengthTypes());
    }
}