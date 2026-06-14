using System.Runtime.CompilerServices;
using TapResult.Columns;
using TapResult.Encodings;
using TapResult.Extensions;

namespace TapResult.Benchmarks;

public sealed class TapDataWriter : WriterBase, IDisposable
{
    private readonly bool _leaveOpen;
    private readonly Stream _dataStream;
    private readonly Stream _schemaStream;

    public TapDataWriter(Stream dataStream, Stream schemaStream, bool leaveOpen = false)
    {
        _dataStream = dataStream;
        _schemaStream = schemaStream;
        _leaveOpen = leaveOpen;

        _dataStream.WriteUInt64(Bootstrap.GetMagicNumber(FileType.TapData, 1, 0, 0));
        _schemaStream.WriteUInt64(Bootstrap.GetMagicNumber(FileType.TapSchema, 1, 0, 0));
    }

    public override void Write(Table table)
    {
        base.Write(table);
        _dataStream.WriteUInt64(Bootstrap.GetMagicNumber(FileType.TapData, 1, 0, 0));

        long position = _schemaStream.Position;
        Span<byte> prefix = stackalloc byte[Bootstrap.PrefixSize];
        _schemaStream.Write(prefix);
        long offset = _schemaStream.Position - position;
        
        base.Write(GetMetadata(), true);
        long end = _schemaStream.Position;

        _schemaStream.Seek(position, SeekOrigin.Begin);
        long length = end - position - offset;
        Bootstrap.SerializePrefix(prefix, offset, length, Length);
        _schemaStream.Write(prefix);
        _schemaStream.Seek(end, SeekOrigin.Begin);
        
        _schemaStream.WriteUInt64(Bootstrap.GetMagicNumber(FileType.TapSchema, 1, 0, 0));
        ClearMetadata();
    }

    protected override void Write(IColumn column, bool isSchema)
    {
        if (column is DataColumn dataColumn)
        {
            dataColumn.Write(isSchema ? _schemaStream : _dataStream);
            return;
        }
        base.Write(column, isSchema);
    }

    public void Dispose()
    {
        if (_leaveOpen)
            return;
        _schemaStream.Dispose();
        _dataStream.Dispose();
    }
}