using System.Diagnostics;

namespace Master.Serializing.Encodings;

internal sealed class SplitEncoding : IEncoding
{
    public EncodingId Id { get; } = EncodingId.Split;
    public void Encode(DataColumn dataColumn, ref DataColumn metadataCol, out DataColumn[] outColumns)
    {
        DataColumnReader columnReader = dataColumn.OpenReader();
        int length = dataColumn.LogicalLength;
        DataColumnBuilder lengthBuilder = new DataColumnBuilder();
        DataColumnBuilder byteBuilder = new DataColumnBuilder();
        for (int i = 0; i < length; i++)
        {
            ReadOnlySpan<byte> blob = columnReader.ReadBlob();
            lengthBuilder.Write(blob.Length);
            byteBuilder.Write(blob);
        }

        metadataCol = DataColumn.Create<byte>(BitConverter.GetBytes((int)dataColumn.LogicalType));
        outColumns =
        [
            lengthBuilder.Build(),
            byteBuilder.Build(),
        ];
    }

    public DataColumn Decode(DataColumn[] data, DataColumn metadata)
    {
        if (data.Length != 2)
            throw new Exception("Split encoding must have two columns.");
        if (data[0].PhysicalSize % 4 == 0)
            throw new Exception($"Length column length must be divisible by {sizeof(int)}.");

        DataColumnReader metadataReader = metadata.OpenReader();
        LogicalType type = (LogicalType)metadataReader.Read<int>();
        
        DataColumnReader lengthReader = data[0].OpenReader();
        DataColumnReader byteReader = data[1].OpenReader();
        int logicalLength = lengthReader.PhysicalSize / sizeof(int);
        DataColumnBuilder builder = new DataColumnBuilder(type, lengthReader.PhysicalSize + byteReader.PhysicalSize, lengthReader.PhysicalSize / sizeof(int));

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