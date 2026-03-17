using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

namespace Master.Readers;

internal struct VarLengthReader : IColumnReader<string>, IColumnReader<byte[]>
{
    private readonly ReadOnlyMemory<byte> _data;
    private int _byteIndex;
    public int Length { get; }
    public int Index { get; private set; }

    internal VarLengthReader(ReadOnlyMemory<byte> data, int length)
    {
        _data = data;
        Length = length;
    }

    private int ReadIntAt(int byteOffset)
    {
        ReadOnlySpan<byte> span = _data.Span.Slice(_byteIndex + byteOffset, Unsafe.SizeOf<int>());
        return BinaryPrimitives.ReadInt32LittleEndian(span);
    }

    private ReadOnlySpan<byte> ReadBlobOffset(int offset)
    {
        int totalOffset = 0;
        for (int i = 0; i < offset; i++)
        {
            totalOffset += ReadIntAt(totalOffset);
            totalOffset += Unsafe.SizeOf<int>();
        }

        int length = ReadIntAt(totalOffset);
        return _data.Span.Slice(_byteIndex + totalOffset + Unsafe.SizeOf<int>(), length);
    }

    public void Advance(int units)
    {
        for (int i = 0; i < units; i++)
        {
            int length = ReadIntAt(0);
            _byteIndex += length;
            _byteIndex += Unsafe.SizeOf<int>();
            Index += 1;
        }
    }

    string IColumnReader<string>.Peek(int offset)
    {
        return Encoding.UTF8.GetString(ReadBlobOffset(offset));
    }

    IEnumerable<string> IColumnReader<string>.Peek(int offset, int count)
    {
        IColumnReader<string> blobReader = this;
        for (int i = 0; i < count; i++)
        {
            yield return blobReader.Peek(offset + i);
        }
    }

    byte[] IColumnReader<byte[]>.Peek(int offset)
    {
        return ReadBlobOffset(offset).ToArray();
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