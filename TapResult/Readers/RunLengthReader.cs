using System.Numerics;

namespace TapResult.Readers;

public class RunLengthReader<T> : IColumnReader<T>
    where T : unmanaged
{
    private readonly IColumnReader<T> _byteColumn;
    private readonly IColumnReader<int> _repeatColumn;

    public RunLengthReader(IColumnReader byteColumn, IColumnReader<int> repeatColumn, int byteLength, int length)
    {
        if (byteColumn is not IColumnReader<T> columnReader)
            throw new ArgumentException($"{nameof(columnReader)} not a {nameof(IColumnReader<T>)}");
        _byteColumn = columnReader;
        _repeatColumn = repeatColumn;
        ByteLength = byteLength;
        Length = length;
    }

    public int ByteLength { get; }
    public int Length { get; }
    public int Index { get; private set; }
    private int _repeatIndex = 0;

    public void Advance(int units)
    {
        _repeatIndex += units;
        while (_repeatIndex > 0 && _repeatIndex >= _repeatColumn.Peek()) // 0 check for when all values have been consumed
        {
            _repeatIndex -= _repeatColumn.Peek();
            _byteColumn.Advance(1);
            _repeatColumn.Advance(1);
        }
        Index += units;
    }

    public T Peek(int offset = 0)
    {
        int indexOffset = 0;
        int repeatIndex = _repeatIndex + offset;
        while (repeatIndex >= _repeatColumn.Peek(indexOffset))
        {
            repeatIndex -= _repeatColumn.Peek(indexOffset);
            indexOffset++;
        }
        return _byteColumn.Peek(indexOffset);
    }

    public IEnumerable<T> Peek(int offset, int count)
    {
        int indexOffset = 0;
        int repeatIndex = _repeatIndex + offset;
        while (repeatIndex >= _repeatColumn.Peek(indexOffset))
        {
            repeatIndex -= _repeatColumn.Peek(indexOffset);
            indexOffset++;
        }

        while (count > 0)
        {
            if (_repeatColumn.Peek(indexOffset) > count)
            {
                foreach (T val in _byteColumn.Peek(indexOffset, count)) 
                    yield return val;
                break;
            }

            foreach (T val in _byteColumn.Peek(indexOffset, _repeatColumn.Peek(indexOffset))) 
                yield return val;
            count -= _repeatColumn.Peek(indexOffset);
            indexOffset++;
        }
    }
}