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
        DataColumnReader columnReader = dataColumn.OpenReader();
        int length = dataColumn.LogicalLength;
        DataColumnBuilder lengthBuilder = new DataColumnBuilder(LogicalType.SInt32, dataColumn.LogicalLength * Unsafe.SizeOf<int>());
        DataColumnBuilder byteBuilder = new DataColumnBuilder(LogicalType.UInt8, dataColumn.PhysicalSize - lengthBuilder.PhysicalSize);
        for (int i = 0; i < length; i++)
        {
            ReadOnlySpan<byte> blob = columnReader.ReadBlob();
            lengthBuilder.Write(blob.Length);
            byteBuilder.Write<byte>(blob);
        }

        return new SplitColumn(lengthBuilder.Build(), byteBuilder.Build(), dataColumn.LogicalType);
    }

    public IColumnReader CreateDecoder(IEnumerable<IColumnReader> childColumns, LogicalType type,
        DataColumnReader metadataReader)
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