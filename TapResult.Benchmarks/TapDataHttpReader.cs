using TapResult.Columns;
using TapResult.Encodings;
using TapResult.Readers;

namespace TapResult.Benchmarks;

public class TapDataHttpReader : TapDataReader
{
    private readonly string _serverUrl;
    private readonly string _dataFilename;

    public TapDataHttpReader(Encoder encoder, string serverUrl, string dataFilename, string schemaFilename)
        : base(encoder, Stream.Null, GetSchemaStream(serverUrl ?? Server.DefaultUrl, schemaFilename), false)
    {
        _serverUrl = serverUrl ?? Server.DefaultUrl;
        _dataFilename = dataFilename;
    }

    private static Stream GetSchemaStream(string serverUrl, string schemaFilename)
    {
        byte[] schemaBytes = Server.ReadFileAsync(serverUrl, schemaFilename)
            .GetAwaiter().GetResult();
        return new MemoryStream(schemaBytes);
    }

    protected override IColumnReader CreateReader(EncodingInfo encodingInfo)
    {
        if (encodingInfo.Encoding == EncodingType.Binary)
        {
            GenericReader reader = new GenericReader(encodingInfo.Blob);
            int physicalSize = reader.Read<int>();
            long offset = reader.Read<long>();

            byte[] data = Server.ReadRangeAsync(_serverUrl, _dataFilename, offset, physicalSize)
                .GetAwaiter().GetResult();
            DataColumn col = new DataColumn(encodingInfo.Type, data, encodingInfo.Length);
            return col.OpenReader();
        }
        return base.CreateReader(encodingInfo);
    }
}
