using System.Diagnostics;

namespace Master.Serializing.Encodings;

internal sealed class SplitEncoding : IEncoding
{
    public EncodingId Id { get; } = EncodingId.Split;
    public Column Encode(PhysicalColumn physicalColumn, ReadOnlyMemory<byte>? suggestedParameters = null)
    {
        ReadOnlySpan<byte> data = physicalColumn.Data.Span;
        int length = physicalColumn.LogicalLength;
        int[] lengths = new int[length];
        byte[] bytes = new byte[physicalColumn.PhysicalSize - length * sizeof(int)];
        int dataIndex = 0, bytesIndex = 0;
        for (int i = 0; i < length; i++)
        {
            int size = BitConverter.ToInt32(data.Slice(dataIndex, sizeof(int)));
            dataIndex += sizeof(int);
            lengths[i] = size;
            
            data.Slice(dataIndex, size).CopyTo(bytes.AsSpan().Slice(bytesIndex));
            dataIndex += size;
            bytesIndex += size;
        }

        return new Column
        {
            Parameters = BitConverter.GetBytes((int)physicalColumn.LogicalType),
            PhysicalColumns =
            [
                PhysicalColumn.Create<int>(lengths),
                PhysicalColumn.Create<byte>(bytes)
            ],
        };
    }

    public PhysicalColumn Decode(ReadOnlyMemory<byte>[] data, ReadOnlyMemory<byte> parameters)
    {
        Debug.Assert(data.Length == 2);
        ReadOnlyMemory<byte> lengths = data[0];
        Debug.Assert(lengths.Length % 4 == 0);
        ReadOnlyMemory<byte> bytesIn = data[1];
        byte[] bytesOut = new byte[lengths.Length + bytesIn.Length];

        int inIndex = 0, outIndex = 0;
        int logicalLength = lengths.Length / sizeof(int);
        for (int i = 0; i < logicalLength; i++)
        {
            ReadOnlySpan<byte> lengthSpan = lengths.Span.Slice(i * sizeof(int), sizeof(int));
            int length = BitConverter.ToInt32(lengthSpan);
            lengthSpan.CopyTo(bytesOut.AsSpan(outIndex, sizeof(int)));
            outIndex += sizeof(int);
            
            bytesIn.Span.Slice(inIndex, length).CopyTo(bytesOut.AsSpan(outIndex, length));
            outIndex += length;
            inIndex += length;
        }

        LogicalType type = (LogicalType)BitConverter.ToInt32(parameters.Span);
        return new PhysicalColumn(type, bytesOut, logicalLength);
    }

    public IEnumerable<LogicalType> GetSupportedTypes()
    {
        yield return LogicalType.Blob;
        yield return LogicalType.String;
    }
}