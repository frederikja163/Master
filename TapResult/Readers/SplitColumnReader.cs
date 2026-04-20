using System.Diagnostics;
using System.Text;

namespace TapResult.Readers;

internal sealed class SplitColumnReader : IColumnReader<string>, IColumnReader<byte[]>
{
    private readonly IColumnReader<int> _lengthColumn;
    private readonly IColumnReader<byte> _byteColumn;

    public int Length => _lengthColumn.Length;
    public int Index => _lengthColumn.Index;
    public LogicalType Type { get; }

    public SplitColumnReader(IColumnReader<int> lengthColumn, IColumnReader<byte> byteColumn, LogicalType type)
    {
        _lengthColumn = lengthColumn;
        _byteColumn = byteColumn;
        Type = type;
    }

    public void Advance(int units)
    {
        for (int i = 0; i < units; i++)
        {
            int length = _lengthColumn.Read();
            _byteColumn.Advance(length);
        }
    }

    IEnumerable<object> IColumnReader.Peek(int offset, int count)
    {
        if (Type == LogicalType.Blob)
        {
            return ((IColumnReader<byte[]>)this).Peek(offset, count);
        }
        if (Type == LogicalType.String)
        {
            return ((IColumnReader<string>)this).Peek(offset, count);
        }

        throw new UnreachableException();
    }

    private SplitColumnReader Clone()
    {
        return new SplitColumnReader(_lengthColumn.Clone(), _byteColumn.Clone(), Type);
    }

    IColumnReader<byte[]> IColumnReader<byte[]>.Clone()
    {
        return Clone();
    }

    IColumnReader<string> IColumnReader<string>.Clone()
    {
        return Clone();
    }

    IColumnReader IColumnReader.Clone()
    {
        return Clone();
    }

    object IColumnReader.Peek(int offset)
    {
        if (Type == LogicalType.Blob)
        {
            return ((IColumnReader<byte[]>)this).Peek(offset);
        }
        if (Type == LogicalType.String)
        {
            return ((IColumnReader<string>)this).Peek(offset);
        }

        throw new UnreachableException();
    }

    private byte[] Peek(int offset)
    {
        int byteOffset = 0;
        for (int i = 0; i < offset; i++)
        {
            byteOffset += _lengthColumn.Peek(i);
        }

        int length = _lengthColumn.Peek(offset);
        return _byteColumn.Peek(byteOffset, length).ToArray();
    }

    string IColumnReader<string>.Peek(int offset)
    {
        return Encoding.UTF8.GetString(Peek(offset));
    }

    IEnumerable<string> IColumnReader<string>.Peek(int offset, int count)
    {
        IColumnReader<string> stringReader = this;
        for (int i = 0; i < count; i++)
        {
            yield return stringReader.Peek(offset + i);
        }
    }

    byte[] IColumnReader<byte[]>.Peek(int offset)
    {
        return Peek(offset);
    }

    IEnumerable<byte[]> IColumnReader<byte[]>.Peek(int offset, int count)
    {
        IColumnReader<byte[]> blobReader = this;
        for (int i = 0; i < count; i++)
        {
            yield return blobReader.Peek(offset + i);
        }
    }
}