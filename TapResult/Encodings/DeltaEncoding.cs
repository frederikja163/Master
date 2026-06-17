using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TapResult.Columns;
using TapResult.Readers;

namespace TapResult.Encodings;

public sealed class DeltaEncoding : IEncoding
{
    public EncodingType Type { get; } = EncodingType.Delta;

    public IColumn Encode<T>(IColumnReader<T> reader) where T : notnull
    {
        if (reader.Length == 0)
        {
            ColumnBuilder<T> empty = new(0);
            return new DeltaColumn(reader.Type, empty.Build(), 0, []);
        }

        T firstValue = reader.Read();
        byte[] firstValueBytes = new byte[Unsafe.SizeOf<T>()];
        Unsafe.WriteUnaligned(ref MemoryMarshal.GetArrayDataReference(firstValueBytes), firstValue);

        if (reader.IsAtEnd)
        {
            ColumnBuilder<T> empty = new(0);
            return new DeltaColumn(reader.Type, empty.Build(), 1, firstValueBytes);
        }

        ColumnBuilder<T> builder = new(reader.Length * Unsafe.SizeOf<T>());
        T previous = firstValue;
        while (!reader.IsAtEnd)
        {
            T current = reader.Read();
            builder.WriteValue(Subtract(current, previous));
            previous = current;
        }

        return new DeltaColumn(reader.Type, builder.Build(), reader.Length, firstValueBytes);
    }

    public IColumnReader CreateDecoder(LogicalType type, int length, GenericReader metadataReader, params IEnumerable<IColumnReader> childColumns)
    {
        using IEnumerator<IColumnReader> childColumnEnumerator = childColumns.GetEnumerator();
        if (!childColumnEnumerator.MoveNext() || childColumnEnumerator.Current is not { } deltas ||
            childColumnEnumerator.MoveNext())
            throw new Exception("Child columns not configured correctly for Delta column.");
        return CreateReader(type, deltas, length, metadataReader);
    }

    internal static IColumnReader CreateReader(LogicalType type, IColumnReader deltaReader, int length, GenericReader metadataReader) => type switch
    {
        LogicalType.SInt8 => new DeltaColumnReader<sbyte>(deltaReader, length, type, metadataReader.Read<sbyte>()),
        LogicalType.SInt16 => new DeltaColumnReader<short>(deltaReader, length, type, metadataReader.Read<short>()),
        LogicalType.SInt32 => new DeltaColumnReader<int>(deltaReader, length, type, metadataReader.Read<int>()),
        LogicalType.SInt64 => new DeltaColumnReader<long>(deltaReader, length, type, metadataReader.Read<long>()),
        LogicalType.UInt8 => new DeltaColumnReader<byte>(deltaReader, length, type, metadataReader.Read<byte>()),
        LogicalType.UInt16 => new DeltaColumnReader<ushort>(deltaReader, length, type, metadataReader.Read<ushort>()),
        LogicalType.UInt32 => new DeltaColumnReader<uint>(deltaReader, length, type, metadataReader.Read<uint>()),
        LogicalType.UInt64 => new DeltaColumnReader<ulong>(deltaReader, length, type, metadataReader.Read<ulong>()),
        LogicalType.Float16 => new DeltaColumnReader<Half>(deltaReader, length, type, metadataReader.Read<Half>()),
        LogicalType.Float32 => new DeltaColumnReader<float>(deltaReader, length, type, metadataReader.Read<float>()),
        LogicalType.Float64 => new DeltaColumnReader<double>(deltaReader, length, type, metadataReader.Read<double>()),
        LogicalType.Blob => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        LogicalType.String => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    private static T Subtract<T>(T a, T b)
    {
        if (typeof(T) == typeof(sbyte))
        {
            sbyte result = (sbyte)(Unsafe.As<T, sbyte>(ref a) - Unsafe.As<T, sbyte>(ref b));
            return Unsafe.As<sbyte, T>(ref result);
        }
        if (typeof(T) == typeof(short))
        {
            short result = (short)(Unsafe.As<T, short>(ref a) - Unsafe.As<T, short>(ref b));
            return Unsafe.As<short, T>(ref result);
        }
        if (typeof(T) == typeof(int))
        {
            int result = Unsafe.As<T, int>(ref a) - Unsafe.As<T, int>(ref b);
            return Unsafe.As<int, T>(ref result);
        }
        if (typeof(T) == typeof(long))
        {
            long result = Unsafe.As<T, long>(ref a) - Unsafe.As<T, long>(ref b);
            return Unsafe.As<long, T>(ref result);
        }
        if (typeof(T) == typeof(byte))
        {
            byte result = (byte)(Unsafe.As<T, byte>(ref a) - Unsafe.As<T, byte>(ref b));
            return Unsafe.As<byte, T>(ref result);
        }
        if (typeof(T) == typeof(ushort))
        {
            ushort result = (ushort)(Unsafe.As<T, ushort>(ref a) - Unsafe.As<T, ushort>(ref b));
            return Unsafe.As<ushort, T>(ref result);
        }
        if (typeof(T) == typeof(uint))
        {
            uint result = Unsafe.As<T, uint>(ref a) - Unsafe.As<T, uint>(ref b);
            return Unsafe.As<uint, T>(ref result);
        }
        if (typeof(T) == typeof(ulong))
        {
            ulong result = Unsafe.As<T, ulong>(ref a) - Unsafe.As<T, ulong>(ref b);
            return Unsafe.As<ulong, T>(ref result);
        }
        if (typeof(T) == typeof(Half))
        {
            Half result = Unsafe.As<T, Half>(ref a) - Unsafe.As<T, Half>(ref b);
            return Unsafe.As<Half, T>(ref result);
        }
        if (typeof(T) == typeof(float))
        {
            float result = Unsafe.As<T, float>(ref a) - Unsafe.As<T, float>(ref b);
            return Unsafe.As<float, T>(ref result);
        }
        if (typeof(T) == typeof(double))
        {
            double result = Unsafe.As<T, double>(ref a) - Unsafe.As<T, double>(ref b);
            return Unsafe.As<double, T>(ref result);
        }
        throw new ArgumentOutOfRangeException(nameof(T));
    }

    public IEnumerable<LogicalType> GetSupportedTypes()
    {
        return TypeHelper.NumericTypes();
    }
}
