using System.Numerics;
using System.Runtime.CompilerServices;
using TapResult.Columns;
using TapResult.Readers;

namespace TapResult.Encodings;

/// <summary>
/// RLE https://en.wikipedia.org/wiki/Run-length_encoding
/// </summary>
public sealed class RunLengthEncoding : IEncoding
{
    public EncodingType Type { get; } = EncodingType.RunLength;
    
    public IColumn Encode<T>(IColumnReader<T> reader) where T : notnull
    {
        ColumnBuilder byteBuilder = new ColumnBuilder(typeof(T).ToLogicalType(), reader.Length * 4);
        ColumnBuilder repeatBuilder = new ColumnBuilder(LogicalType.SInt32, reader.Length * Unsafe.SizeOf<int>());
        T previous = reader.Read();
        int repeats = 1;
        for (int i = 1; i < reader.Length; i++)
        {
            T current = reader.Read();
            if (current.Equals(previous))
            {
                repeats++;
                continue;
            }

            byteBuilder.WriteValue(previous);
            repeatBuilder.WriteValue(repeats);
            previous = current;
            repeats = 1;
        }
        byteBuilder.WriteValue(previous);
        repeatBuilder.WriteValue(repeats);

        return new RunLengthColumn(typeof(T).ToLogicalType(), byteBuilder.Build(), repeatBuilder.Build(), reader.Length);
    }


    public IColumnReader CreateDecoder(LogicalType type, int length, GenericReader metadataReader, params IEnumerable<IColumnReader> childColumns)
    {
        using IEnumerator<IColumnReader> childColumnEnumerator = childColumns.GetEnumerator();
        if (!childColumnEnumerator.MoveNext() || childColumnEnumerator.Current is not { } bytes ||
            !childColumnEnumerator.MoveNext() || childColumnEnumerator.Current is not IColumnReader<int> repeats ||
            childColumnEnumerator.MoveNext())
            throw new Exception("Child columns not configured correctly for RunLength column.");
        return CreateReader(type, bytes, repeats, length);
    }

    internal static IColumnReader CreateReader(LogicalType type, IColumnReader byteReader, IColumnReader<int> repeatReader, int length)
    {
        return type switch
        {
            LogicalType.SInt8 => new RunLengthReader<sbyte>(byteReader, repeatReader, length, type),
            LogicalType.SInt16 => new RunLengthReader<short>(byteReader, repeatReader, length, type),
            LogicalType.SInt32 => new RunLengthReader<int>(byteReader, repeatReader, length, type),
            LogicalType.SInt64 => new RunLengthReader<long>(byteReader, repeatReader, length, type),
            LogicalType.UInt8 => new RunLengthReader<byte>(byteReader, repeatReader, length, type),
            LogicalType.UInt16 => new RunLengthReader<ushort>(byteReader, repeatReader, length, type),
            LogicalType.UInt32 => new RunLengthReader<uint>(byteReader, repeatReader, length, type),
            LogicalType.UInt64 => new RunLengthReader<ulong>(byteReader, repeatReader, length, type),
            LogicalType.Float16 => new RunLengthReader<Half>(byteReader, repeatReader, length, type),
            LogicalType.Float32 => new RunLengthReader<float>(byteReader, repeatReader, length, type),
            LogicalType.Float64 => new RunLengthReader<double>(byteReader, repeatReader, length, type),

            // Explicitly throw argument out of range exception so we can get warnings if LogicalType adds new types.
            LogicalType.Blob => throw new ArgumentOutOfRangeException(nameof(type), type, null),
            LogicalType.String => throw new ArgumentOutOfRangeException(nameof(type), type, null),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public IEnumerable<LogicalType> GetSupportedTypes()
    {
        return TypeHelper.AllTypes();
    }
}