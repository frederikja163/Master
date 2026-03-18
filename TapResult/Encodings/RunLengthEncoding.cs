using System.Numerics;
using System.Runtime.CompilerServices;
using TapResult.Columns;
using TapResult.Readers;

namespace TapResult.Encodings;

/// <summary>
/// RLE https://en.wikipedia.org/wiki/Run-length_encoding
/// </summary>
public class RunLengthEncoding : IEncoding
{
    public EncodingId Id { get; } = EncodingId.RunLength;
    
    public IColumn Encode(in DataColumn dataColumn)
    {
        if (!dataColumn.LogicalType.TryGetSize(out int size))
        {
            throw new Exception("Type must be a primitive.");
        }
        
        IColumn column = size switch
        {
            1 => Encode<byte>(dataColumn),
            2 => Encode<ushort>(dataColumn),
            4 => Encode<uint>(dataColumn),
            8 => Encode<ulong>(dataColumn),
            // TODO: string
            _ => throw new Exception("Logical type size must be either 1, 2, 4 or 8."),
        };

        return column;
    }
    
    public IColumn Encode<T>(in DataColumn dataColumn)
        where T : unmanaged, INumber<T>, IBinaryInteger<T>, IMinMaxValue<T>
    {
        IColumnReader<T> reader = new PrimitiveReader<T>(dataColumn.Data);
        int byteLength = Unsafe.SizeOf<T>();
        DataColumnBuilder byteBuilder = new DataColumnBuilder(dataColumn.LogicalType, dataColumn.PhysicalSize);
        DataColumnBuilder repeatBuilder = new DataColumnBuilder(LogicalType.SInt32, dataColumn.LogicalLength * Unsafe.SizeOf<int>());
        T previous = reader.Read();
        int repeats = 1;
        for (int i = 1; i < dataColumn.LogicalLength; i++)
        {
            T current = reader.Read();
            if (current == previous)
            {
                repeats++;
                continue;
            }
            byteBuilder.Write(previous);
            repeatBuilder.Write(repeats);
            previous = current;
        }
        byteBuilder.Write(previous);
        repeatBuilder.Write(repeats);

        return new RunLengthColumn(dataColumn.LogicalType, byteBuilder.Build(), repeatBuilder.Build(), byteLength, dataColumn.LogicalLength);
    }

    public IColumnReader CreateDecoder(LogicalType type,
        ref GenericReader metadataReader, IEnumerable<IColumnReader> childColumns)
    {
        using IEnumerator<IColumnReader> childColumnEnumerator = childColumns.GetEnumerator();
        if (!childColumnEnumerator.MoveNext() || childColumnEnumerator.Current is not { } bytes ||
            !childColumnEnumerator.MoveNext() || childColumnEnumerator.Current is not IColumnReader<int> repeats ||
            childColumnEnumerator.MoveNext())
            throw new Exception("Child columns not configured correctly for RunLength column.");
        if (metadataReader.Read<int>() != RunLengthColumn.Size)
            throw new Exception("RunLength metadata was malformed.");
        int byteLength = metadataReader.Read<int>();
        int length = metadataReader.Read<int>();
        return type switch
        {
            LogicalType.SInt8 => new RunLengthReader<sbyte>(bytes, repeats, byteLength, length),
            LogicalType.SInt16 => new RunLengthReader<short>(bytes, repeats, byteLength, length),
            LogicalType.SInt32 => new RunLengthReader<int>(bytes, repeats, byteLength, length),
            LogicalType.SInt64 => new RunLengthReader<long>(bytes, repeats, byteLength, length),
            LogicalType.UInt8 => new RunLengthReader<byte>(bytes, repeats, byteLength, length),
            LogicalType.UInt16 => new RunLengthReader<ushort>(bytes, repeats, byteLength, length),
            LogicalType.UInt32 => new RunLengthReader<uint>(bytes, repeats, byteLength, length),
            LogicalType.UInt64 => new RunLengthReader<ulong>(bytes, repeats, byteLength, length),
            LogicalType.Float16 => new RunLengthReader<Half>(bytes, repeats, byteLength, length),
            LogicalType.Float32 => new RunLengthReader<float>(bytes, repeats, byteLength, length),
            LogicalType.Float64 => new RunLengthReader<double>(bytes, repeats, byteLength, length),
            
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