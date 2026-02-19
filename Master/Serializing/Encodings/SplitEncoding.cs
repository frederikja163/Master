using System.Diagnostics;
using System.Runtime.CompilerServices;
using Master.Serializing.Columns;

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

    public DataColumn Decode(IColumn data)
    {
        if (data is not SplitColumn splitColumn)
            throw new Exception($"Data({nameof(data)}) is not a SplitColumn");
        if (splitColumn._lengthColumn.PhysicalSize % sizeof(int) != 0)
            throw new Exception($"Length column length must be divisible by {sizeof(int)}.");

        
        DataColumnReader lengthReader = splitColumn._lengthColumn.OpenReader();
        DataColumnReader byteReader = splitColumn._byteColumn.OpenReader();
        int logicalLength = lengthReader.PhysicalSize / sizeof(int);
        DataColumnBuilder builder = new DataColumnBuilder(splitColumn._logicalType, lengthReader.PhysicalSize + byteReader.PhysicalSize);

        for (int i = 0; i < logicalLength; i++)
        {
            int length = lengthReader.Read<int>();
            ReadOnlySpan<byte> bytes = byteReader.Read<byte>(length);
            builder.WriteBlob(bytes);
        }
        return builder.Build();
    }

    public IEnumerable<LogicalType> GetSupportedTypes()
    {
        yield return LogicalType.Blob;
        yield return LogicalType.String;
    }
}