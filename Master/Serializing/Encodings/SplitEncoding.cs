using System.Diagnostics;
using System.Runtime.CompilerServices;
using Master.Serializing.Columns;
using Master.Serializing.Readers;

namespace Master.Serializing.Encodings;

internal sealed class SplitEncoding : IEncoding
{
    public EncodingId Id { get; } = EncodingId.Split;
    public IColumn Encode(ref DataColumn dataColumn)
    {
        GenericReader columnReader = dataColumn.OpenGenericReader();
        int length = dataColumn.LogicalLength;
        DataColumnBuilder lengthBuilder = new DataColumnBuilder(LogicalType.SInt32, dataColumn.LogicalLength * Unsafe.SizeOf<int>());
        DataColumnBuilder byteBuilder = new DataColumnBuilder(LogicalType.UInt8, dataColumn.PhysicalSize - lengthBuilder.PhysicalSize);
        for (int i = 0; i < length; i++)
        {
            ReadOnlySpan<byte> blob = columnReader.ReadUnits();
            lengthBuilder.WriteRaw(blob.Slice(0, Unsafe.SizeOf<int>()), 1);
            byteBuilder.Write(blob.Slice(Unsafe.SizeOf<int>()));
        }

        return new SplitColumn(lengthBuilder.Build(), byteBuilder.Build(), dataColumn.LogicalType);
    }

    public IColumnReader CreateDecoder(LogicalType type,
        GenericReader metadataReader, IEnumerable<IColumnReader> childColumns)
    {
        using IEnumerator<IColumnReader> childColumnEnumerator = childColumns.GetEnumerator();
        if (!childColumnEnumerator.MoveNext() || childColumnEnumerator.Current is not IColumnReader<int> lengths ||
            !childColumnEnumerator.MoveNext() || childColumnEnumerator.Current is not IColumnReader<byte> bytes ||
            childColumnEnumerator.MoveNext())
            throw new Exception("Child columns not configured correctly for split column.");
        return new SplitColumnReader(lengths, bytes);
    }

    public IEnumerable<LogicalType> GetSupportedTypes()
    {
        yield return LogicalType.Blob;
        yield return LogicalType.String;
    }
}