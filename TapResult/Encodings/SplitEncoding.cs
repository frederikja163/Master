using System.Runtime.CompilerServices;
using TapResult.Columns;
using TapResult.Readers;

namespace TapResult.Encodings;

/// <summary>
/// Split encoding takes variable length columns and splits the length and the data into separate columns.
/// </summary>
public sealed class SplitEncoding : IEncoding
{
    public EncodingType Type { get; } = EncodingType.Split;
    public IColumn Encode(in DataColumn dataColumn)
    {
        GenericReader columnReader = dataColumn.OpenGenericReader();
        int length = dataColumn.LogicalLength;
        ColumnBuilder lengthBuilder = new ColumnBuilder(LogicalType.SInt32, dataColumn.LogicalLength * Unsafe.SizeOf<int>());
        ColumnBuilder byteBuilder = new ColumnBuilder(LogicalType.UInt8, dataColumn.PhysicalSize - lengthBuilder.PhysicalSize);
        for (int i = 0; i < length; i++)
        {
            ReadOnlySpan<byte> blob = columnReader.ReadUnits(dataColumn.LogicalType);
            lengthBuilder.WriteRaw(blob.Slice(0, Unsafe.SizeOf<int>()), 1);
            byteBuilder.Write(blob.Slice(Unsafe.SizeOf<int>()));
        }

        return new SplitColumn(lengthBuilder.BuildDataColumn(), byteBuilder.BuildDataColumn(), dataColumn.LogicalType);
    }

    public IColumnReader CreateDecoder(LogicalType type,
        GenericReader metadataReader, IEnumerable<IColumnReader> childColumns)
    {
        using IEnumerator<IColumnReader> childColumnEnumerator = childColumns.GetEnumerator();
        if (!childColumnEnumerator.MoveNext() || childColumnEnumerator.Current is not IColumnReader<int> lengths ||
            !childColumnEnumerator.MoveNext() || childColumnEnumerator.Current is not IColumnReader<byte> bytes ||
            childColumnEnumerator.MoveNext())
            throw new Exception("Child columns not configured correctly for split column.");
        return new SplitColumnReader(lengths, bytes, type);
    }

    public IEnumerable<LogicalType> GetSupportedTypes()
    {
        yield return LogicalType.Blob;
        yield return LogicalType.String;
    }
}