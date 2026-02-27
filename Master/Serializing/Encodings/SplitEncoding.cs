using System.Diagnostics;
using System.Runtime.CompilerServices;
using Master.Serializing.Columns;
using Master.Serializing.Readers;

namespace Master.Serializing.Encodings;

internal sealed class SplitEncoding : IEncoding
{
    public EncodingId Id { get; } = EncodingId.Split;
    public IColumn Encode(DataColumn dataColumn)
    {
        IColumnReader<byte[]> columnReader = dataColumn.OpenReader<byte[]>();
        int length = dataColumn.LogicalLength;
        DataColumnBuilder lengthBuilder = new DataColumnBuilder(LogicalType.SInt32, dataColumn.LogicalLength * Unsafe.SizeOf<int>());
        DataColumnBuilder byteBuilder = new DataColumnBuilder(LogicalType.UInt8, dataColumn.PhysicalSize - lengthBuilder.PhysicalSize);
        for (int i = 0; i < length; i++)
        {
            ReadOnlySpan<byte> blob = columnReader.Read();
            lengthBuilder.Write(blob.Length);
            byteBuilder.Write(blob);
        }

        return new SplitColumn(lengthBuilder.Build(), byteBuilder.Build(), dataColumn.LogicalType);
    }

    public IColumnReader CreateDecoder(LogicalType type,
        DataColumnReader<byte> metadataReader, IEnumerable<IColumnReader> childColumns)
    {
        using IEnumerator<IColumnReader> childColumnEnumerator = childColumns.GetEnumerator();
        IColumnReader lengthReader = childColumnEnumerator.Current;
        if (!childColumnEnumerator.MoveNext() || lengthReader is not IColumnReader<int> lengths)
            goto error;
        IColumnReader byteReader = childColumnEnumerator.Current;
        if (childColumnEnumerator.MoveNext() || byteReader is not IColumnReader<byte> bytes)
            goto error;
        return new SplitColumnReader(lengths, bytes);
        
        error:
        throw new Exception();
    }

    public IEnumerable<LogicalType> GetSupportedTypes()
    {
        yield return LogicalType.Blob;
        yield return LogicalType.String;
    }
}