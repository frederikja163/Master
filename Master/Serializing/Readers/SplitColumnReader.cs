using System.Data;
using System.Text;

namespace Master.Serializing.Readers;

internal sealed class SplitColumnReader : IColumnReader<string>, IColumnReader<byte[]>
{
    private readonly IColumnReader<int> _lengthColumn;
    private readonly IColumnReader<byte> _byteColumn;

    public int Length => _lengthColumn.Length;
    public int Index => _lengthColumn.Index;

    public SplitColumnReader(IColumnReader<int> lengthColumn, IColumnReader<byte> byteColumn)
    {
        _lengthColumn = lengthColumn;
        _byteColumn = byteColumn;
    }

    public void Advance(int units)
    {
        for (int i = 0; i < units; i++)
        {
            int length = _lengthColumn.Read();
            _byteColumn.Advance(length);
        }
    }

    string IColumnReader<string>.Peek(int byteOffset)
    {
        int length = _lengthColumn.Peek(byteOffset);
        return Encoding.UTF8.GetString(_byteColumn.Peek(byteOffset, length).ToArray());
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
        int length = _lengthColumn.Peek(offset);
        return _byteColumn.Peek(offset, length).ToArray();
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