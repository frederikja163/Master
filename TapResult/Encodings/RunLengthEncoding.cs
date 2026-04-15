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
    
    public IColumn Encode(DataColumn dataColumn)
    {
        IColumn column = dataColumn.LogicalType switch
        {
            LogicalType.SInt8 => Encode<sbyte>(dataColumn),
            LogicalType.SInt16 => Encode<short>(dataColumn),
            LogicalType.SInt32 => Encode<int>(dataColumn),
            LogicalType.SInt64 => Encode<long>(dataColumn),
            LogicalType.UInt8 => Encode<byte>(dataColumn),
            LogicalType.UInt16 => Encode<ushort>(dataColumn),
            LogicalType.UInt32 => Encode<uint>(dataColumn),
            LogicalType.UInt64 => Encode<ulong>(dataColumn),
            LogicalType.Float16 => Encode<Half>(dataColumn),
            LogicalType.Float32 => Encode<float>(dataColumn),
            LogicalType.Float64 => Encode<double>(dataColumn),
            LogicalType.Blob => Encode<byte[]>(dataColumn),
            LogicalType.String => Encode<string>(dataColumn),
            _ => throw new Exception("Logical type size must be either 1, 2, 4 or 8."),
        };

        return column;
    }

    public IColumn Encode<T>(DataColumn dataColumn)
        where T : notnull
    {
        IColumnReader<T> reader = dataColumn.OpenReader<T>();
        GenericReader genericReader = dataColumn.OpenGenericReader();
        ColumnBuilder byteBuilder = new ColumnBuilder(dataColumn.LogicalType, dataColumn.PhysicalSize);
        ColumnBuilder repeatBuilder = new ColumnBuilder(LogicalType.SInt32, dataColumn.LogicalLength * Unsafe.SizeOf<int>());
        T previous = reader.Read();
        int repeats = 1;
        for (int i = 1; i < dataColumn.LogicalLength; i++)
        {
            T current = reader.Read();
            if (current.Equals(previous))
            {
                repeats++;
                continue;
            }

            byteBuilder.WriteRaw(genericReader.ReadUnits(dataColumn.LogicalType), 1);
            genericReader.AdvanceUnits(dataColumn.LogicalType, repeats - 1);
            repeatBuilder.WriteValue(repeats);
            previous = current;
            repeats = 1;
        }
        byteBuilder.WriteRaw(genericReader.ReadUnits(dataColumn.LogicalType), 1);
        repeatBuilder.WriteValue(repeats);

        return new RunLengthColumn(dataColumn.LogicalType, byteBuilder.Build(), repeatBuilder.Build(), dataColumn.LogicalLength);
    }


    public IColumnReader CreateDecoder(LogicalType type, GenericReader metadataReader, params IEnumerable<IColumnReader> childColumns)
    {
        using IEnumerator<IColumnReader> childColumnEnumerator = childColumns.GetEnumerator();
        if (!childColumnEnumerator.MoveNext() || childColumnEnumerator.Current is not { } bytes ||
            !childColumnEnumerator.MoveNext() || childColumnEnumerator.Current is not IColumnReader<int> repeats ||
            childColumnEnumerator.MoveNext())
            throw new Exception("Child columns not configured correctly for RunLength column.");
        int length = metadataReader.Read<int>();
        return CreateReader(type, bytes, repeats, length);
    }

    internal static IColumnReader CreateReader(LogicalType type, IColumnReader byteReader, IColumnReader<int> repeatReader, int length)
    {
        return type switch
        {
            LogicalType.SInt8 => new RunLengthReader<sbyte>(byteReader, repeatReader, length),
            LogicalType.SInt16 => new RunLengthReader<short>(byteReader, repeatReader, length),
            LogicalType.SInt32 => new RunLengthReader<int>(byteReader, repeatReader, length),
            LogicalType.SInt64 => new RunLengthReader<long>(byteReader, repeatReader, length),
            LogicalType.UInt8 => new RunLengthReader<byte>(byteReader, repeatReader, length),
            LogicalType.UInt16 => new RunLengthReader<ushort>(byteReader, repeatReader, length),
            LogicalType.UInt32 => new RunLengthReader<uint>(byteReader, repeatReader, length),
            LogicalType.UInt64 => new RunLengthReader<ulong>(byteReader, repeatReader, length),
            LogicalType.Float16 => new RunLengthReader<Half>(byteReader, repeatReader, length),
            LogicalType.Float32 => new RunLengthReader<float>(byteReader, repeatReader, length),
            LogicalType.Float64 => new RunLengthReader<double>(byteReader, repeatReader, length),

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