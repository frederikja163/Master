using System.Runtime.CompilerServices;
using TapResult.Columns;
using TapResult.Encodings;
using TapResult.Extensions;
using TapResult.Readers;

namespace TapResult.Benchmarks;

public class TapDataReader : ReaderBase, IDisposable, IAsyncDisposable
{
    private readonly bool _leaveOpen;
    private readonly Stream _dataStream;
    private readonly Stream _schemaStream;
    
    public TapDataReader(Encoder encoder, Stream dataStream, Stream schemaStream, bool leaveOpen = false) : base(encoder)
    {
        _dataStream = dataStream;
        _schemaStream = schemaStream;
        _leaveOpen = leaveOpen;
        
        ulong magicNumber = schemaStream.ReadUInt64();
        byte[] bootstrap = new byte[Bootstrap.PrefixSize];
        while (Bootstrap.TryParseMagicNumber(magicNumber, out FileType type, out byte major, out _, out _) && type == FileType.TapSchema && major == 1 && _schemaStream.Position < _schemaStream.Length)
        {
            _schemaStream.ReadExactly(bootstrap);
            Bootstrap.ParsePrefix(bootstrap, out long start, out long length, out long logicalLength);
            _schemaStream.Seek(start - Bootstrap.PrefixSize, SeekOrigin.Current);
            byte[] bytes = new byte[length];
            _schemaStream.ReadExactly(bytes);
            AddEncodings(bytes, (int)logicalLength);
            magicNumber = _schemaStream.ReadUInt64();
        }
    }

    protected override IColumnReader CreateReader(EncodingInfo encodingInfo)
    {
        if (encodingInfo.Encoding == EncodingType.Binary)
        {
            GenericReader reader = new GenericReader(encodingInfo.Blob);
            int physicalSize = reader.Read<int>();
            long offset = reader.Read<long>();
            _dataStream.Seek(offset, SeekOrigin.Begin);
            byte[] data = new byte[physicalSize];
            _dataStream.ReadExactly(data);
            DataColumn col = new DataColumn(encodingInfo.Type, data, encodingInfo.Length);
            return col.OpenReader();
        }
        return base.CreateReader(encodingInfo);
    }

    public void Dispose()
    {
        if (_leaveOpen)
            return;
        _dataStream.Dispose();
        _schemaStream.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_leaveOpen)
            return;
        await _dataStream.DisposeAsync();
        await _schemaStream.DisposeAsync();
    }
}