using System.Runtime.CompilerServices;
using TapResult.Columns;
using TapResult.Encodings;
using TapResult.Extensions;
using TapResult.Readers;

namespace TapResult.Benchmarks;

public class TapResultHttpReader : ReaderBase, IDisposable, IAsyncDisposable
{
    private readonly string _serverUrl;
    private readonly string _filename;
    private readonly bool _leaveOpen;

    private TapResultHttpReader(Encoder encoder, string serverUrl, string filename, bool leaveOpen = false)
        : base(encoder)
    {
        _serverUrl = serverUrl ?? Server.DefaultUrl;
        _filename = filename;
        _leaveOpen = leaveOpen;
    }

    public static async Task<TapResultHttpReader> CreateReaderAsync(
        string serverUrl, string filename, Encoder? encoder = null, bool leaveOpen = true)
    {
        TapResultHttpReader reader = new TapResultHttpReader(encoder ?? Encoder.Default, serverUrl, filename, leaveOpen);

        byte[] postfixBytes = await Server.ReadRangeAsync(serverUrl, filename, -Bootstrap.PostfixSize, Bootstrap.PostfixSize);
        Span<byte> postfix = postfixBytes;

        Bootstrap.ParsePostfix(postfix, out long start, out long length, out long logicalLength, out ulong magicNumber);

        if (!Bootstrap.TryParseMagicNumber(magicNumber, out FileType type, out byte major, out _, out _) ||
            type != FileType.TapResult || major != 1)
        {
            throw new Exception("Magic number is either malformed or this file is of another type.");
        }

        byte[] schema = await Server.ReadRangeAsync(serverUrl, filename, start, length);

        reader.AddEncodings(schema, (int)logicalLength);
        return reader;
    }

    protected override IColumnReader CreateReader(EncodingInfo encodingInfo)
    {
        if (encodingInfo.Encoding == EncodingType.Binary)
        {
            GenericReader reader = new GenericReader(encodingInfo.Blob);
            int physicalSize = reader.Read<int>();
            long offset = reader.Read<long>();

            byte[] data = Server.ReadRangeAsync(_serverUrl, _filename, offset, physicalSize)
                .GetAwaiter().GetResult();
            DataColumn col = new DataColumn(encodingInfo.Type, data, encodingInfo.Length);
            return col.OpenReader();
        }

        return base.CreateReader(encodingInfo);
    }

    public void Dispose()
    {
    }

    public async ValueTask DisposeAsync()
    {
    }
}
